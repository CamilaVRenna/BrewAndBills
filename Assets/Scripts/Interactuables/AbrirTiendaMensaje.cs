using UnityEngine;
using System.Collections;
using TMPro;

// Este componente debe ser añadido al objeto del cartel.
public class ControladorCartelTienda : MonoBehaviour
{
    // Asigna tu GestorCompradores aquí desde el Inspector
    public GestorCompradores gestorCompradores;

    [Header("Configuraci\u00f3n")]
    [Tooltip("Tiempo que tarda la rotaci\u00f3n en segundos.")]
    public float tiempoRotacion = 0.5f;

    [Header("UI (Informaci\u00f3n de Raycast)")]
    public GameObject uiInfoObjeto; // Panel o TextMeshPro a mostrar al mirar.
    public TextMeshProUGUI textoInteraccion; // Texto dentro del UI (Ej: [E] Abrir)

    private bool estaRotando = false;
    private bool tiendaAbiertaLocal = false; // Estado local para manejar la rotación

    void Awake()
    {
        if (gestorCompradores == null)
        {
            gestorCompradores = FindObjectOfType<GestorCompradores>();
        }

        // Inicializa el estado local y la rotación inicial
        if (gestorCompradores != null)
        {
            tiendaAbiertaLocal = gestorCompradores.tiendaAbierta;
            // Asegura que la rotación inicial corresponda al estado actual.
            float anguloInicial = tiendaAbiertaLocal ? 180f : 0f;
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, anguloInicial, transform.eulerAngles.z);
        }

        // Oculta la UI al inicio si está asignada
        if (uiInfoObjeto != null)
        {
            uiInfoObjeto.SetActive(false);
        }
    }

    // --- Métodos de Interacción por Raycast ---

    public void MostrarInformacion()
    {
        if (uiInfoObjeto != null)
        {
            if (gestorCompradores == null) return;

            // Sincroniza el estado local con el gestor ANTES de mostrar el mensaje.
            tiendaAbiertaLocal = gestorCompradores.tiendaAbierta;

            if (tiendaAbiertaLocal)
            {
                textoInteraccion.text = "[E] Cerrar Tienda";
            }
            else
            {
                textoInteraccion.text = "[E] Abrir Tienda";
            }
            uiInfoObjeto.SetActive(true);
        }
    }

    public void OcultarInformacion()
    {
        if (uiInfoObjeto != null)
        {
            uiInfoObjeto.SetActive(false);
        }
    }

    // --- Método llamado por InteraccionJugador al presionar E ---

    public void Interactuar()
    {
        if (gestorCompradores == null)
        {
            Debug.LogError("El GestorCompradores no est\u00e1 asignado ni se pudo encontrar.");
            return;
        }

        if (estaRotando) return; // Ignora si ya está en rotación

        // Invertimos el estado local
        tiendaAbiertaLocal = !tiendaAbiertaLocal;

        // Llamamos al gestor y comenzamos la animación de rotación
        if (tiendaAbiertaLocal)
        {
            gestorCompradores.AbrirTienda();
            StartCoroutine(RotarObjeto(180f));
        }
        else
        {
            gestorCompradores.CerrarTienda();
            StartCoroutine(RotarObjeto(0f));
        }

        // Actualizamos el mensaje inmediatamente
        MostrarInformacion();
    }

    private IEnumerator RotarObjeto(float targetAngle)
    {
        estaRotando = true; // Bloqueamos la interacci\u00f3n
        float elapsedTime = 0f;

        Quaternion inicioRotacion = transform.rotation;
        Quaternion finalRotacion = Quaternion.Euler(
            transform.eulerAngles.x,
            targetAngle,
            transform.eulerAngles.z
        );

        while (elapsedTime < tiempoRotacion)
        {
            transform.rotation = Quaternion.Slerp(
                inicioRotacion,
                finalRotacion,
                elapsedTime / tiempoRotacion
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = finalRotacion;
        estaRotando = false; // Desbloqueamos la interacci\u00f3n

        // Reasegurar la actualización de la UI si el jugador sigue mirando
        MostrarInformacion();
    }
}