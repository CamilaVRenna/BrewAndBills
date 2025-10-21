using UnityEngine;
using System;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    // El singleton se inicializa en Awake()
    public static TimeManager Instance { get; private set; }

    // =========================================================================
    // EVENTOS Y FLAGS
    // =========================================================================

    public event Action OnDayStart;
    public event Action OnNightStart;
    public event Action OnNewDaySequenceStarted; // Se dispara antes del fade.
    public bool durmioManualmente { get; private set; } = false;

    // =========================================================================
    // CONFIGURACIÓN DE TIEMPO
    // =========================================================================

    [Header("Configuración de Duraciones Reales")]
    [Tooltip("Duración de la fase de DÍA (6:00 a 20:00) en segundos reales.")]
    [SerializeField] private float dayDurationInSeconds = 480f; // 8 minutos
    [Tooltip("Duración de la fase de NOCHE (20:00 a 00:00) en segundos reales.")]
    [SerializeField] private float nightDurationInSeconds = 60f; // 1 minuto (4 horas de juego)

    [Header("Transición")]
    [Tooltip("Tiempo que el mensaje 'DÍA X' permanece en pantalla durante la transición")]
    [SerializeField] private float messageHoldTime = 2.0f;

    // Horas fijas del ciclo (24 horas de juego)
    private const int HORA_INICIO_DIA = 6;     // 6:00 AM
    private const int HORA_INICIO_NOCHE = 20;  // 8:00 PM (20:00)
    private const int HORAS_FIN_NOCHE = 0;     // 12:00 AM (00:00) 👈 ¡CAMBIO CLAVE!
    private const int HORAS_EN_UN_DIA = 24;

    // Horas de duración de cada fase de juego (24 horas totales)
    private const int HORAS_FASE_DIA = HORA_INICIO_NOCHE - HORA_INICIO_DIA; // 14 horas (20 - 6)
    // Nueva duración de noche: 20 a 24 = 4 horas de juego.
    private const int HORAS_FASE_NOCHE = HORAS_EN_UN_DIA - HORA_INICIO_NOCHE; // 24 - 20 = 4 horas

    // Factores de escala de tiempo (Cuánto avanza la hora de juego por segundo real)
    private float dayTimeScaleFactor;
    private float nightTimeScaleFactor;

    // =========================================================================
    // ESTADO ACTUAL
    // =========================================================================

    [Header("Estado Actual")]
    public int currentDay = 1;
    public bool isDaytime = true;

    // La hora actual del juego (ej: 6.0, 20.0, 24.0 (medianoche))
    private float currentHour;

    private Coroutine cycleCoroutine;

    // -----------------------------------------------------------------------------
    // CICLO DE VIDA Y SETUP
    // -----------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CalculateTimeScaleFactors();

        // Nota: Asume que GestorJuego ya inicializó currentDay
        StartDayCycle();
    }

    private void Update()
    {
        // 🚨 SINCRONIZACIÓN CLAVE: Envía la hora actual al GestorUI cada frame.
        if (cycleCoroutine != null && GestorUI.Instance != null)
        {
            GestorUI.Instance.ActualizarRelojUI(GetCurrentTimeString());
        }
    }

    /// <summary>
    /// Calcula cuánto tiempo de juego debe avanzar por cada segundo real para respetar las duraciones.
    /// </summary>
    private void CalculateTimeScaleFactors()
    {
        // Factor de Día: (14 Horas de Juego) / (Segundos Reales)
        dayTimeScaleFactor = HORAS_FASE_DIA / dayDurationInSeconds;

        // Factor de Noche: (4 Horas de Juego) / (Segundos Reales)
        nightTimeScaleFactor = HORAS_FASE_NOCHE / nightDurationInSeconds;

        Debug.Log($"[TimeManager] Día (14h) a {dayDurationInSeconds}s, Factor: {dayTimeScaleFactor:F3}");
        Debug.Log($"[TimeManager] Noche (4h) a {nightDurationInSeconds}s, Factor: {nightTimeScaleFactor:F3}");
    }

    private void StartDayCycle()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        cycleCoroutine = StartCoroutine(DayNightCycle());
    }

    // -----------------------------------------------------------------------------
    // FLUJO DE TIEMPO PRINCIPAL (DayNightCycle)
    // -----------------------------------------------------------------------------

    private IEnumerator DayNightCycle()
    {
        currentHour = HORA_INICIO_DIA; // Siempre empezamos el ciclo a las 6:00 AM

        while (true) // Bucle infinito
        {
            // 1. FASE DE DÍA (6:00 a 20:00)
            durmioManualmente = false;
            isDaytime = true;
            Debug.Log($"[TimeManager] Comienza el DÍA {currentDay} a las {GetCurrentTimeString()}.");
            OnDayStart?.Invoke();

            // Avanzamos usando el factor del día.
            while (currentHour < HORA_INICIO_NOCHE)
            {
                currentHour += Time.deltaTime * dayTimeScaleFactor;
                yield return null;
            }

            // Aseguramos que la hora sea exactamente 20:00.
            currentHour = HORA_INICIO_NOCHE;

            // 2. FASE DE NOCHE (20:00 a 00:00)
            isDaytime = false;
            Debug.Log($"[TimeManager] Comienza la Noche del DÍA {currentDay} a las {GetCurrentTimeString()}.");
            OnNightStart?.Invoke();

            // La hora de finalización de esta fase es 24.0 (Medianoche/00:00)
            float cycleEndTime = HORAS_EN_UN_DIA; // 24.0

            // Avanzamos usando el factor de la noche (más rápido).
            while (currentHour < cycleEndTime)
            {
                // Si el jugador durmió manualmente, salimos del bucle
                if (durmioManualmente)
                {
                    Debug.Log("Jugador durmió manualmente. Saltando al nuevo día.");
                    break;
                }
                currentHour += Time.deltaTime * nightTimeScaleFactor;
                yield return null;
            }

            // 3. FIN DE CICLO: INICIAR TRANSICIÓN
            Debug.Log($"[TimeManager] Medianoche alcanzada (o se durmió). Hora final: {GetCurrentTimeString()}");

            // Aseguramos que la hora para el nuevo día sea 0:00 (24.0), aunque solo sea para el cálculo.
            currentHour = cycleEndTime;

            StartCoroutine(HandleDayEndTransition());

            // Pausamos el ciclo actual.
            yield break;
        }
    }

    // -----------------------------------------------------------------------------
    // GESTIÓN DE LA TRANSICIÓN VISUAL
    // -----------------------------------------------------------------------------

    private IEnumerator HandleDayEndTransition()
    {
        // 1. NOTIFICAR INICIO DE SECUENCIA (GestorJuego guarda, bloquea input, etc.)
        OnNewDaySequenceStarted?.Invoke();

        // 2. Esperar si fue por desmayo (llegó a medianoche sin dormir)
        if (!durmioManualmente)
        {
            Debug.Log("[TimeManager] El jugador se desmayó a medianoche. Esperando el mensaje temporal del UI.");
            // Esto le da tiempo al GestorJuego/UI de mostrar un mensaje de desmayo antes del fade
            yield return new WaitForSeconds(3.5f);
        }

        // 3. SECUENCIA VISUAL y AVANCE DE DÍA
        yield return StartCoroutine(TransitionToNewDay());

        // 4. Reiniciar el ciclo, que comienza internamente a las 6:00 AM
        StartDayCycle();
    }

    private IEnumerator TransitionToNewDay()
    {
        GestorUI gestorUI = GestorUI.Instance;
        if (gestorUI == null) yield break;

        // 1. FADE A NEGRO
        yield return StartCoroutine(gestorUI.FundidoANegro());

        // 2. LÓGICA DE AVANCE DE DÍA
        currentDay++;

        // 3. MOSTRAR MENSAJE CENTRAL (Ej: "DÍA 2")
        string mensaje = $"DÍA {currentDay}";
        yield return StartCoroutine(gestorUI.MostrarMensajeCentral(mensaje, messageHoldTime));

        // 4. FADE DESDE NEGRO
        yield return StartCoroutine(gestorUI.FundidoDesdeNegro());
    }


    // -----------------------------------------------------------------------------
    // MÉTODOS PÚBLICOS
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Método llamado por un objeto interactuable (ej: la cama) para terminar el día.
    /// </summary>
    public void RegistrarDormir()
    {
        // Solo se permite dormir si es de noche (>= 20:00)
        if (currentHour >= HORA_INICIO_NOCHE && currentHour < HORAS_EN_UN_DIA && durmioManualmente == false)
        {
            durmioManualmente = true;
            Debug.Log("Dormir registrado a las " + GetCurrentTimeString());
        }
        else
        {
            Debug.LogWarning("No se puede registrar el sueño: No es de noche o ya se registró.");
        }
    }

    /// <summary>
    /// Obtiene la hora actual formateada como HH:MM (00:00 a 23:59).
    /// </summary>
    public string GetCurrentTimeString()
    {
        // Normalizamos la hora para que esté entre 0 y 24 (excluido)
        float normalizedHour = currentHour % HORAS_EN_UN_DIA;

        // Si el resultado es muy cercano a 24.0, lo forzamos a 0.0 para mostrar 00:00
        if (normalizedHour > 23.999f) normalizedHour = 0f;

        int hours = Mathf.FloorToInt(normalizedHour);
        int minutes = Mathf.FloorToInt((normalizedHour - hours) * 60f);
        return $"{hours:D2}:{minutes:D2}";
    }

    /// <summary>
    /// Devuelve el progreso del día (0.0 al 1.0) para el control visual (sol/cielo).
    /// </summary>
    public float GetDayProgress()
    {
        // La hora actual del ciclo, donde 0 es 6 AM y 24 es 6 AM del día siguiente.
        float totalHoursInCycle = HORAS_EN_UN_DIA; // 24 horas

        // El progreso se basa en la hora actual desde el inicio del ciclo (6 AM)
        float cycleHour = currentHour - HORA_INICIO_DIA;

        // Si el currentHour es 24 (medianoche), el cycleHour es 18 (24 - 6).
        // Si el currentHour es 6 (inicio), el cycleHour es 0 (6 - 6).

        // Normalizamos el valor para que sea un ciclo completo de 0 a 24 horas
        if (cycleHour < 0) cycleHour += totalHoursInCycle; // Debería ser innecesario con la nueva lógica, pero por seguridad.

        return (cycleHour % totalHoursInCycle) / totalHoursInCycle;
    }
}