using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Mueve (rota) un objeto hacia un ángulo de destino al interactuar, y lo devuelve a su posición original al volver a interactuar.
/// Reemplaza el script PuertaCambioEscena, ya que no hay transiciones de escena.
/// </summary>
public class ObjetoRotatorioInteractivo : MonoBehaviour
{
    [Header("Configuración de Rotación")]
    [Tooltip("El ángulo final al que rotará el objeto. Define el eje (X, Y o Z)")]
    public float anguloDestino = 90f;
    [Tooltip("Tiempo que tarda la animación en segundos.")]
    public float duracionAnimacion = 0.5f;

    [Header("Ejes de Rotación")]
    public bool rotarEjeX = false;
    public bool rotarEjeY = true; // Por defecto para la mayoría de las puertas
    public bool rotarEjeZ = false;

    private Quaternion rotacionInicial;
    private Quaternion rotacionFinal;
    private bool estaAbierto = false;
    private Coroutine coroutineRotacion = null;

    // AÑADIDO: Nuevas propiedades para los clips de audio (para corregir CS1503)
    [Header("Audio")]
    [Tooltip("Clip de sonido que se reproducirá al abrir la puerta.")]
    public AudioClip sonidoAbrirPuerta;
    [Tooltip("Clip de sonido que se reproducirá al cerrar la puerta.")]
    public AudioClip sonidoCerrarPuerta;
    // FIN AÑADIDO

    [Header("Indicador Visual (Al Mirar)")]
    [Tooltip("Texto que se mostrará al mirar el objeto. Ej: 'Abrir puerta'")]
    public string textoIndicador = "Abrir";
    [Tooltip("Arrastra aquí el MISMO prefab de Canvas flotante que usas para los ingredientes.")]
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;


    void Start()
    {
        // Guardar la rotación original al iniciar. Esta es la posición cerrada/base.
        rotacionInicial = transform.localRotation;

        // Calcular la rotación final (destino)
        Vector3 eulerInicial = rotacionInicial.eulerAngles;
        // Solo aplica el anguloDestino al eje marcado
        float x = rotarEjeX ? anguloDestino : 0;
        float y = rotarEjeY ? anguloDestino : 0;
        float z = rotarEjeZ ? anguloDestino : 0;

        // Sumar la rotación deseada a la rotación inicial. Se usa localRotation para rotar respecto al pivote del objeto.
        rotacionFinal = Quaternion.Euler(eulerInicial.x + x, eulerInicial.y + y, eulerInicial.z + z);
    }

    /// <summary>
    /// Esta es la función que se llama desde el script de Interacción del jugador.
    /// Inicia la rotación de apertura o cierre.
    /// </summary>
    public void Interactuar()
    {
        // Si hay una animación en curso, la detenemos para iniciar la nueva.
        if (coroutineRotacion != null)
        {
            StopCoroutine(coroutineRotacion);
        }

        if (estaAbierto)
        {
            // Si está abierto, lo cerramos
            coroutineRotacion = StartCoroutine(RotarObjeto(rotacionInicial));
            estaAbierto = false;
            // CORRECCIÓN: Ahora pasamos el AudioClip en lugar del string
            if (GestorAudio.Instancia != null) GestorAudio.Instancia.ReproducirSonidoPuerta(sonidoCerrarPuerta);
            textoIndicador = "Abrir"; // Actualiza el texto para el siguiente uso
        }
        else
        {
            // Si está cerrado, lo abrimos
            coroutineRotacion = StartCoroutine(RotarObjeto(rotacionFinal));
            estaAbierto = true;
            // CORRECCIÓN: Ahora pasamos el AudioClip en lugar del string
            if (GestorAudio.Instancia != null) GestorAudio.Instancia.ReproducirSonidoPuerta(sonidoAbrirPuerta);
            textoIndicador = "Cerrar"; // Actualiza el texto para el siguiente uso
        }

        // Si el indicador visual está activo mientras interactuamos, actualiza el texto inmediatamente
        if (canvasInfoActual != null && canvasInfoActual.activeSelf)
        {
            MostrarInformacion();
        }
    }

    private IEnumerator RotarObjeto(Quaternion destino)
    {
        float tiempoTranscurrido = 0f;
        Quaternion rotacionInicio = transform.localRotation;

        while (tiempoTranscurrido < duracionAnimacion)
        {
            // Lerp (interpolación lineal) para una rotación suave.
            transform.localRotation = Quaternion.Slerp(rotacionInicio, destino, tiempoTranscurrido / duracionAnimacion);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Asegurar que termine exactamente en el destino
        transform.localRotation = destino;
        coroutineRotacion = null;
    }

    // --- LÓGICA DE INDICADOR VISUAL ---

    public void MostrarInformacion()
    {
        if (prefabCanvasInfo == null)
        {
            Debug.LogWarning($"Objeto Rotatorio {gameObject.name}: PrefabCanvasInfo no asignado.");
            return;
        }

        // El textoAMostrar ahora se basa en el estado actual para mostrar "Abrir" o "Cerrar"
        string textoAMostrar = estaAbierto ? "Cerrar" : "Abrir";

        // Usamos el valor del inspector si no estamos en movimiento, pero priorizamos el estado
        if (coroutineRotacion == null)
        {
            textoAMostrar = estaAbierto ? "Cerrar" : this.textoIndicador;
        }


        if (canvasInfoActual == null)
        {
            // Instanciar el canvas
            Vector3 offset = Vector3.up * 1.0f;
            Collider col = GetComponent<Collider>();
            Vector3 basePos = (col != null) ? col.bounds.center : transform.position;
            canvasInfoActual = Instantiate(prefabCanvasInfo, basePos + offset, Quaternion.identity);

            // Intentar configurar el texto a través de InfoCanvasUI
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoNombre != null)
            {
                uiScript.textoNombre.text = textoAMostrar;
                uiScript.textoNombre.gameObject.SetActive(true);
                if (uiScript.textoCantidad != null) uiScript.textoCantidad.gameObject.SetActive(false);
            }
            else // Fallback si no hay InfoCanvasUI
            {
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) { tmp.text = textoAMostrar; }
                else { Debug.LogWarning($"No se encontró TextMeshProUGUI en prefab para {gameObject.name}."); }
            }
        }
        else // Si el canvas ya existe, solo actualizar y activar
        {
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoNombre != null)
            {
                uiScript.textoNombre.text = textoAMostrar;
                uiScript.textoNombre.gameObject.SetActive(true);
            }
            canvasInfoActual.SetActive(true);
        }
    }

    public void OcultarInformacion()
    {
        if (canvasInfoActual != null)
        {
            canvasInfoActual.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (canvasInfoActual != null)
        {
            Destroy(canvasInfoActual);
        }
    }
}