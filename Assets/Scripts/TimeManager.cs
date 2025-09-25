using UnityEngine;
using System;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    // Este c�digo hace que el script pueda ser accedido f�cilmente desde cualquier otro lugar.
    public static TimeManager Instance { get; private set; }

    // Eventos: Son como se�ales que se env�an a otros scripts.
    // Los otros scripts pueden "escuchar" estas se�ales.
    public event Action OnDayStart;
    public event Action OnNightStart;
    public event Action OnNightEnd;

    [Header("Configuraci�n del Tiempo")]
    [Tooltip("Duraci�n de un d�a de juego en segundos reales")]
    [SerializeField] private float dayDurationInSeconds = 600f; // 10 minutos

    [Tooltip("Duraci�n de la noche en segundos reales")]
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
        // Empieza la coroutine que har� que el tiempo corra.
        StartCoroutine(DayNightCycle());
    }

    // M�todo para que otros scripts (como el RelojUI) puedan obtener el progreso del d�a.
    public float GetDayProgress()
    {
        // Calcula el progreso del d�a en una escala de 0 a 1.
        return tiempoActual / (dayDurationInSeconds + nightDurationInSeconds);
    }

    private IEnumerator DayNightCycle()
    {
        while (true) // Bucle infinito para que el ciclo se repita d�a tras d�a.
        {
            // --- FASE DEL D�A ---
            isDaytime = true;
            Debug.Log($"Iniciando D�A {currentDay}");
            OnDayStart?.Invoke(); // Env�a la se�al de que el d�a ha empezado.

            // Bucle para actualizar el tiempo y esperar durante el d�a.
            tiempoActual = 0f; // Reinicia el contador para el d�a
            while (tiempoActual < dayDurationInSeconds)
            {
                tiempoActual += Time.deltaTime;
                yield return null;
            }

            // --- FASE DE LA NOCHE ---
            isDaytime = false;
            Debug.Log("El d�a ha terminado. La noche ha comenzado.");
            OnNightStart?.Invoke(); // Env�a la se�al de que la noche ha empezado.

            // Bucle para actualizar el tiempo y esperar durante la noche.
            while (tiempoActual < dayDurationInSeconds + nightDurationInSeconds)
            {
                tiempoActual += Time.deltaTime;
                yield return null;
            }

            // --- FIN DE LA NOCHE Y NUEVO D�A ---
            Debug.Log("La noche ha terminado. El jugador deber�a desmayarse o ir a la cama.");
            OnNightEnd?.Invoke(); // Env�a la se�al de que la noche ha terminado.

            // Avanza al siguiente d�a.
            currentDay++;
        }
    }
}