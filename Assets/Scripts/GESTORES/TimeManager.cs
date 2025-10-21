using UnityEngine;
using System;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    // El singleton se inicializa en Awake()
    public static TimeManager Instance { get; private set; }

    // Eventos: Las se�ales que enviaremos a otros scripts.
    public event Action OnDayStart;
    public event Action OnNightStart;
    public event Action OnNightEnd;

    // Flag para saber si el jugador durmi� manualmente
    public bool durmioManualmente = false;

    [Header("Configuraci�n del Tiempo")]
    [Tooltip("Duraci�n de un d�a de juego en segundos reales")]
    [SerializeField] private float dayDurationInSeconds = 600f; // 10 minutos
    [Tooltip("Duraci�n de la noche en segundos reales")]
    [SerializeField] private float nightDurationInSeconds = 120f; // 2 minutos

    [Header("Estado Actual")]
    public int currentDay = 1;
    public bool isDaytime = true;

    private float tiempoActual;

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
        StartCoroutine(DayNightCycle());
    }

    // M�todo para que otros scripts digan que el jugador durmi�
    public void RegistrarDormir()
    {
        durmioManualmente = true;
    }

    public float GetDayProgress()
    {
        return tiempoActual / (dayDurationInSeconds + nightDurationInSeconds);
    }

    private IEnumerator DayNightCycle()
    {
        while (true) // Bucle infinito
        {
            // Resetear el flag al inicio de cada ciclo
            durmioManualmente = false;

            // --- FASE DEL D�A ---
            isDaytime = true;
            Debug.Log($"Iniciando D�A {currentDay}");
            OnDayStart?.Invoke();

            tiempoActual = 0f;
            while (tiempoActual < dayDurationInSeconds)
            {
                tiempoActual += Time.deltaTime;
                yield return null;
            }

            // --- FASE DE LA NOCHE ---
            isDaytime = false;
            Debug.Log("El d�a ha terminado. La noche ha comenzado.");
            OnNightStart?.Invoke();

            while (tiempoActual < dayDurationInSeconds + nightDurationInSeconds)
            {
                // Si el jugador durmi� manualmente, salimos del bucle
                if (durmioManualmente)
                {
                    Debug.Log("Jugador durmi� manualmente. Saltando al nuevo d�a.");
                    break;
                }
                tiempoActual += Time.deltaTime;
                yield return null;
            }

            // --- FIN DE LA NOCHE Y NUEVO D�A ---
            Debug.Log("La noche ha terminado. Enviando se�al de fin de noche.");
            OnNightEnd?.Invoke();
            currentDay++;
        }
    }
}
