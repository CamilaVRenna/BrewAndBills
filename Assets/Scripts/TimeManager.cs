using UnityEngine;
using System;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    // Este código hace que el script pueda ser accedido fácilmente desde cualquier otro lugar.
    public static TimeManager Instance { get; private set; }

    // Eventos: Son como señales que se envían a otros scripts.
    // Los otros scripts pueden "escuchar" estas señales.
    public event Action OnDayStart;
    public event Action OnNightStart;
    public event Action OnNightEnd;

    [Header("Configuración del Tiempo")]
    [Tooltip("Duración de un día de juego en segundos reales")]
    [SerializeField] private float dayDurationInSeconds = 600f; // 10 minutos

    [Tooltip("Duración de la noche en segundos reales")]
    [SerializeField] private float nightDurationInSeconds = 120f; // 2 minutos

    [Header("Estado Actual")]
    public int currentDay = 1;
    public bool isDaytime = true;

    // Variable para guardar el tiempo que ha pasado en el ciclo actual.
    private float tiempoActual;

    private void Awake()
    {
        // Esto asegura que solo haya una instancia de TimeManager en todo el juego.
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
        // Empieza la coroutine que hará que el tiempo corra.
        StartCoroutine(DayNightCycle());
    }

    // Método para que otros scripts (como el RelojUI) puedan obtener el progreso del día.
    public float GetDayProgress()
    {
        // Calcula el progreso del día en una escala de 0 a 1.
        return tiempoActual / (dayDurationInSeconds + nightDurationInSeconds);
    }

    private IEnumerator DayNightCycle()
    {
        while (true) // Bucle infinito para que el ciclo se repita día tras día.
        {
            // --- FASE DEL DÍA ---
            isDaytime = true;
            Debug.Log($"Iniciando DÍA {currentDay}");
            OnDayStart?.Invoke(); // Envía la señal de que el día ha empezado.

            // Bucle para actualizar el tiempo y esperar durante el día.
            tiempoActual = 0f; // Reinicia el contador para el día
            while (tiempoActual < dayDurationInSeconds)
            {
                tiempoActual += Time.deltaTime;
                yield return null;
            }

            // --- FASE DE LA NOCHE ---
            isDaytime = false;
            Debug.Log("El día ha terminado. La noche ha comenzado.");
            OnNightStart?.Invoke(); // Envía la señal de que la noche ha empezado.

            // Bucle para actualizar el tiempo y esperar durante la noche.
            while (tiempoActual < dayDurationInSeconds + nightDurationInSeconds)
            {
                tiempoActual += Time.deltaTime;
                yield return null;
            }

            // --- FIN DE LA NOCHE Y NUEVO DÍA ---
            Debug.Log("La noche ha terminado. El jugador debería desmayarse o ir a la cama.");
            OnNightEnd?.Invoke(); // Envía la señal de que la noche ha terminado.

            // Avanza al siguiente día.
            currentDay++;
        }
    }
}