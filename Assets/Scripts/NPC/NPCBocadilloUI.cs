using UnityEngine;
using TMPro;
using System.Collections;

public class NPCBocadilloUI : MonoBehaviour
{
    [Header("UI Bocadillo Config")]
    public GameObject prefabBocadilloUI; // Referencia asignada en el Inspector de este script.
    public Transform puntoAnclajeBocadillo; // Referencia asignada en el Inspector de este script.
    [HideInInspector] public float duracionFeedback = 3.0f;

    private GameObject instanciaBocadilloActual = null;
    private TextMeshProUGUI textoBocadilloActual = null;
    private TextMeshProUGUI textoTemporizadorActual = null;
    private Coroutine coroutineOcultarBocadillo = null;

    // Propiedad para acceder al texto del timer
    public TextMeshProUGUI TextoTemporizador => textoTemporizadorActual;

    // ====================================================================
    // MÉTODOS CORREGIDOS (Eliminación de argumentos redundantes)
    // ====================================================================

    /// <summary>
    /// Inicializa la instancia del bocadillo si no existe. 
    /// **CORRECCIÓN FINAL:** Ya no acepta argumentos. Usa sus propias referencias.
    /// </summary>
    public void Inicializar()
    {
        // Las referencias (prefabBocadilloUI y puntoAnclajeBocadillo) 
        // ya están asignadas en el Inspector de este componente.
        InicializarBocadillo();
    }

    /// <summary>
    /// Retorna si el bocadillo está actualmente visible. Usado por NPCComprador.
    /// </summary>
    public bool EsBocadilloVisible()
    {
        return instanciaBocadilloActual != null && instanciaBocadilloActual.activeInHierarchy;
    }

    /// <summary>
    /// Retorna el texto actual dentro del bocadillo. Usado por NPCComprador.
    /// </summary>
    public string ObtenerTextoActual()
    {
        return textoBocadilloActual != null ? textoBocadilloActual.text : string.Empty;
    }

    // ====================================================================
    // MÉTODOS EXISTENTES
    // ====================================================================

    private void OnDestroy()
    {
        if (instanciaBocadilloActual != null) Destroy(instanciaBocadilloActual);
        if (coroutineOcultarBocadillo != null) StopCoroutine(coroutineOcultarBocadillo);
    }

    /// <summary>
    /// Muestra el bocadillo con un texto dado.
    /// </summary>
    public void MostrarBocadillo(string texto, bool autoOcultar = false)
    {
        if (coroutineOcultarBocadillo != null)
        {
            StopCoroutine(coroutineOcultarBocadillo);
            coroutineOcultarBocadillo = null;
        }

        InicializarBocadillo(); // Llama a la inicialización interna

        if (textoBocadilloActual != null)
        {
            textoBocadilloActual.text = texto;
            instanciaBocadilloActual.SetActive(true);

            // Ocultar el temporizador si estamos mostrando feedback o el mensaje de atención
            bool esPedido = !autoOcultar && texto != "[E]";
            if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(esPedido);

            if (autoOcultar && duracionFeedback > 0)
                coroutineOcultarBocadillo = StartCoroutine(OcultarBocadilloDespuesDe(duracionFeedback));
        }
    }

    /// <summary>
    /// Oculta el bocadillo y detiene su temporizador de auto-ocultamiento.
    /// </summary>
    public void OcultarBocadillo()
    {
        if (coroutineOcultarBocadillo != null) { StopCoroutine(coroutineOcultarBocadillo); coroutineOcultarBocadillo = null; }
        if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(false);
        if (instanciaBocadilloActual != null) instanciaBocadilloActual.SetActive(false);
    }

    private void InicializarBocadillo()
    {
        // Se llama InicializarBocadillo() desde la Inicializar() pública y MostrarBocadillo()
        if (instanciaBocadilloActual != null) return;

        if (prefabBocadilloUI != null && puntoAnclajeBocadillo != null)
        {
            // La instancia se crea usando las referencias públicas (o serializadas) de ESTE componente.
            instanciaBocadilloActual = Instantiate(prefabBocadilloUI, puntoAnclajeBocadillo.position, puntoAnclajeBocadillo.rotation, puntoAnclajeBocadillo);

            // Buscar TextMeshProUGUI principal
            textoBocadilloActual = instanciaBocadilloActual.GetComponentInChildren<TextMeshProUGUI>(true);

            // Buscar TextMeshProUGUI del temporizador
            var timerTransform = instanciaBocadilloActual.transform.Find("CanvasBocadillo/FondoBocadillo/TextoTemporizador");
            if (timerTransform != null) textoTemporizadorActual = timerTransform.GetComponent<TextMeshProUGUI>();

            if (textoBocadilloActual == null)
            {
                Debug.LogError("¡Prefab Bocadillo UI sin TextMeshProUGUI principal!", instanciaBocadilloActual);
                Destroy(instanciaBocadilloActual);
                instanciaBocadilloActual = null;
            }

            // Ocultar al iniciar (por si acaso el prefab está activo)
            if (instanciaBocadilloActual != null) instanciaBocadilloActual.SetActive(false);
        }
        else
        {
            Debug.LogError("¡Falta asignar PrefabBocadilloUI o PuntoAnclajeBocadillo!");
        }
    }

    private IEnumerator OcultarBocadilloDespuesDe(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        OcultarBocadillo();
        coroutineOcultarBocadillo = null;
    }
}