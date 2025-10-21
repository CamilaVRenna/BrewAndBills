using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GestorUI : MonoBehaviour
{
    // =========================================================================
    // SINGLETON
    // =========================================================================
    public static GestorUI Instance { get; private set; }

    // =========================================================================
    // PROPIEDADES (Tus referencias originales + Reloj)
    // =========================================================================

    [Header("UI Persistente")]
    [Tooltip("El texto de TMPro que muestra la hora actual (HH:MM)")]
    public TextMeshProUGUI textoReloj; // 👈 ¡REFERENCIA DEL RELOJ AÑADIDA!
    public TextMeshProUGUI textoDinero;
    public Image iconoMonedaDinero;
    public Sprite spriteMoneda;
    private int dineroActual = 50;
    public int DineroActual => dineroActual;
    public TextMeshProUGUI textoMielesRecolectadas;

    [Header("UI Feedback Día y Dinero")]
    [Tooltip("El texto del día que aparece al inicio de cada ciclo.")]
    public TextMeshProUGUI textoAnuncioDia;

    [Tooltip("CanvasGroup del texto del día para controlar su alfa (fade).")]
    public CanvasGroup grupoCanvasTextoDia;
    public float duracionFadeDia = 1.0f;
    public float tiempoVisibleDia = 2.0f;

    public TextMeshProUGUI textoCambioDinero;
    public Image iconoMonedaCambio;
    [Tooltip("CanvasGroup del texto de cambio de dinero para controlar su alfa.")]
    public CanvasGroup grupoCanvasCambioDinero;
    public float duracionFadeCambioDinero = 0.5f;
    public float tiempoVisibleCambioDinero = 1.5f;

    [Header("UI Fundido Transición")]
    [Tooltip("Una Imagen UI de color negro que cubra toda la pantalla.")]
    public Image panelFundidoNegro;
    public float duracionFundidoNegro = 0.75f;

    [Header("UI de Pedidos")]
    public GameObject uiPedidoGameObject;
    public TextMeshProUGUI textoNombrePedido;
    public Image iconoPedido;

    private Transform npcConPedidoTransform;

    [Header("Mensaje Flotante")]
    [Tooltip("El panel o texto para mostrar mensajes temporales.")]
    public GameObject panelMensajeFlotante;
    public TextMeshProUGUI textoMensajeFlotante;
    private Coroutine coMensajeFlotante;

    // =========================================================================
    // CICLO DE VIDA
    // =========================================================================

    void Awake()
    {
        // 1. Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            // No usamos DontDestroyOnLoad porque este objeto está en la escena de juego.
        }
        else
        {
            Debug.LogError("Ya existe una instancia de GestorUI. Destruyendo duplicado.");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (grupoCanvasTextoDia != null) grupoCanvasTextoDia.alpha = 0;
        if (grupoCanvasCambioDinero != null) grupoCanvasCambioDinero.alpha = 0;

        if (uiPedidoGameObject != null) uiPedidoGameObject.SetActive(false);
        if (panelMensajeFlotante != null) panelMensajeFlotante.SetActive(false);

        if (panelFundidoNegro != null)
        {
            panelFundidoNegro.color = new Color(0, 0, 0, 0);
            panelFundidoNegro.gameObject.SetActive(false);
        }

        if (iconoMonedaDinero != null && spriteMoneda != null) iconoMonedaDinero.sprite = spriteMoneda;
        if (iconoMonedaCambio != null && spriteMoneda != null) iconoMonedaCambio.sprite = spriteMoneda;

        // Inicializar el dinero si el GestorJuego ya está presente
        if (GestorJuego.Instance != null)
        {
            ActualizarUIDinero(GestorJuego.Instance.dineroActual);
        }

        // Inicializar el reloj a un valor por defecto
        if (textoReloj != null)
        {
            textoReloj.text = "06:00";
        }
    }

    void Update()
    {
        if (npcConPedidoTransform != null && uiPedidoGameObject != null && Camera.main != null)
        {
            Vector3 posicionMundo = npcConPedidoTransform.position + new Vector3(0, 2f, 0);
            Vector3 posicionPantalla = Camera.main.WorldToScreenPoint(posicionMundo);
            uiPedidoGameObject.transform.position = posicionPantalla;
        }
    }

    // =========================================================================
    // UI RELOJ 👈 ¡NUEVO MÉTODO!
    // =========================================================================

    /// <summary>
    /// Actualiza el texto del reloj con la hora formateada (HH:MM).
    /// Llamado por el TimeManager en cada frame.
    /// </summary>
    public void ActualizarRelojUI(string horaFormateada)
    {
        if (textoReloj != null)
        {
            textoReloj.text = horaFormateada;
        }
    }

    // =========================================================================
    // UI DINERO
    // =========================================================================

    public void ActualizarUIDinero(int cantidad)
    {
        dineroActual = cantidad;
        if (textoDinero != null)
        {
            textoDinero.text = cantidad.ToString();
        }
        if (iconoMonedaDinero != null && textoDinero != null)
        {
            iconoMonedaDinero.enabled = true;
        }
    }

    public bool IntentarGastarDinero(int cantidad)
    {
        if (dineroActual >= cantidad)
        {
            dineroActual -= cantidad;
            if (GestorJuego.Instance != null)
            {
                GestorJuego.Instance.dineroActual = dineroActual;
            }

            ActualizarUIDinero(dineroActual);
            MostrarCambioDinero(-cantidad);
            return true;
        }
        else
        {
            Debug.Log("No hay suficiente dinero.");
            MostrarMensajeTemporal("¡No tienes suficiente dinero!", 2.0f); // Feedback
            return false;
        }
    }

    public void MostrarCambioDinero(int cantidad)
    {
        if (textoCambioDinero != null && grupoCanvasCambioDinero != null && iconoMonedaCambio != null)
        {
            string signo = (cantidad > 0) ? "+" : "";
            textoCambioDinero.text = $"{signo}{cantidad}";
            textoCambioDinero.color = (cantidad > 0) ? Color.green : Color.red;
            iconoMonedaCambio.color = textoCambioDinero.color;
            StartCoroutine(FundidoEntradaSalidaElemento(grupoCanvasCambioDinero, duracionFadeCambioDinero, tiempoVisibleCambioDinero));
        }
    }

    // =========================================================================
    // UI PEDIDOS (Burbuja)
    // =========================================================================

    public void MostrarPedido(string nombre, Sprite icono, Transform npcTransform)
    {
        if (uiPedidoGameObject == null || textoNombrePedido == null || iconoPedido == null)
        {
            Debug.LogError("Faltan referencias de UI para el pedido en GestorUI.");
            return;
        }
        npcConPedidoTransform = npcTransform;
        uiPedidoGameObject.SetActive(true);
        textoNombrePedido.text = nombre;
        iconoPedido.sprite = icono;
    }

    public void OcultarPedido()
    {
        if (uiPedidoGameObject != null)
        {
            uiPedidoGameObject.SetActive(false);
        }
        npcConPedidoTransform = null;
    }

    // =========================================================================
    // UI MENSAJE TEMPORAL/FLOTANTE
    // =========================================================================

    public void MostrarMensajeTemporal(string mensaje, float duracion)
    {
        if (panelMensajeFlotante == null || textoMensajeFlotante == null)
        {
            Debug.LogError("Faltan referencias de UI para el mensaje flotante en GestorUI.");
            return;
        }
        if (coMensajeFlotante != null)
        {
            StopCoroutine(coMensajeFlotante);
        }
        coMensajeFlotante = StartCoroutine(MostrarMensaje(mensaje, duracion));
    }

    private IEnumerator MostrarMensaje(string mensaje, float duracion)
    {
        panelMensajeFlotante.SetActive(true);
        textoMensajeFlotante.text = mensaje;

        CanvasGroup canvasGroup = panelMensajeFlotante.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            // Intentar añadir CanvasGroup si no existe para el fade
            canvasGroup = panelMensajeFlotante.AddComponent<CanvasGroup>();
        }

        // Fade In
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < 0.5f)
        {
            tiempoTranscurrido += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, tiempoTranscurrido / 0.5f);
            yield return null;
        }

        yield return new WaitForSeconds(duracion);

        // Fade Out
        tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < 0.5f)
        {
            tiempoTranscurrido += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, tiempoTranscurrido / 0.5f);
            yield return null;
        }

        panelMensajeFlotante.SetActive(false);
    }

    // =========================================================================
    // UI TRANSICIÓN DE DÍA (Fundido y Mensaje Central)
    // =========================================================================

    public IEnumerator MostrarMensajeCentral(string mensaje, float tiempoVisible)
    {
        if (textoAnuncioDia == null || grupoCanvasTextoDia == null)
        {
            Debug.LogError("Falta TextoAnuncioDia o GrupoCanvasTextoDia para el mensaje central.");
            yield break;
        }

        textoAnuncioDia.text = mensaje;
        grupoCanvasTextoDia.gameObject.SetActive(true);

        // FADE IN
        yield return FundidoEntradaElemento(grupoCanvasTextoDia, duracionFadeDia);

        // ESPERA VISIBLE
        yield return new WaitForSeconds(tiempoVisible);

        // FADE OUT
        yield return FundidoSalidaElemento(grupoCanvasTextoDia, duracionFadeDia);
    }

    public IEnumerator FundidoANegro()
    {
        Debug.Log("Iniciando Fundido a Negro...");
        if (panelFundidoNegro == null) yield break;
        panelFundidoNegro.gameObject.SetActive(true);
        // Asegura que el color de inicio sea transparente (alpha 0)
        Color colorInicial = panelFundidoNegro.color;
        panelFundidoNegro.color = new Color(colorInicial.r, colorInicial.g, colorInicial.b, 0f);
        yield return FundidoAlfaElemento(panelFundidoNegro, 0f, 1f, duracionFundidoNegro);
        Debug.Log("Fundido a Negro Completo.");
    }

    public IEnumerator FundidoDesdeNegro()
    {
        Debug.Log("Iniciando Fundido desde Negro...");
        if (panelFundidoNegro == null) yield break;
        // Asegura que el color de inicio sea opaco (alpha 1)
        Color colorInicial = panelFundidoNegro.color;
        panelFundidoNegro.color = new Color(colorInicial.r, colorInicial.g, colorInicial.b, 1f);
        yield return FundidoAlfaElemento(panelFundidoNegro, 1f, 0f, duracionFundidoNegro);
        panelFundidoNegro.gameObject.SetActive(false);
        Debug.Log("Fundido desde Negro Completo.");
    }

    // =========================================================================
    // CORRUTINAS GENÉRICAS
    // =========================================================================

    private IEnumerator FundidoEntradaSalidaElemento(CanvasGroup gc, float durFade, float durVisible)
    {
        float temporizador = 0f;
        gc.gameObject.SetActive(true); // Asegurar que está activo
        while (temporizador < durFade)
        {
            temporizador += Time.deltaTime;
            gc.alpha = Mathf.Lerp(0f, 1f, temporizador / durFade);
            yield return null;
        }
        gc.alpha = 1f;
        yield return new WaitForSeconds(durVisible);
        temporizador = 0f;
        while (temporizador < durFade)
        {
            temporizador += Time.deltaTime;
            gc.alpha = Mathf.Lerp(1f, 0f, temporizador / durFade);
            yield return null;
        }
        gc.alpha = 0f;
        gc.gameObject.SetActive(false);
    }

    private IEnumerator FundidoEntradaElemento(CanvasGroup gc, float duracion)
    {
        gc.gameObject.SetActive(true);
        float temporizador = 0f;
        while (temporizador < duracion)
        {
            temporizador += Time.deltaTime;
            gc.alpha = Mathf.Lerp(0f, 1f, temporizador / duracion);
            yield return null;
        }
        gc.alpha = 1f;
    }

    private IEnumerator FundidoSalidaElemento(CanvasGroup gc, float duracion)
    {
        float temporizador = 0f;
        while (temporizador < duracion)
        {
            temporizador += Time.deltaTime;
            gc.alpha = Mathf.Lerp(1f, 0f, temporizador / duracion);
            yield return null;
        }
        gc.alpha = 0f;
        gc.gameObject.SetActive(false);
    }

    private IEnumerator FundidoAlfaElemento(Image imagen, float alfaInicio, float alfaFinal, float duracion)
    {
        float temporizador = 0f;
        Color colorActual = imagen.color;
        while (temporizador < duracion)
        {
            temporizador += Time.deltaTime;
            float alfa = Mathf.Lerp(alfaInicio, alfaFinal, temporizador / duracion);
            imagen.color = new Color(colorActual.r, colorActual.g, colorActual.b, alfa);
            yield return null;
        }
        imagen.color = new Color(colorActual.r, colorActual.g, colorActual.b, alfaFinal);
    }

    // =========================================================================
    // MÉTODOS MENORES
    // =========================================================================

    public void ActualizarTextoMieles(string texto)
    {
        if (textoMielesRecolectadas != null)
            textoMielesRecolectadas.text = texto;
        else
            Debug.LogWarning("No se asignó textoMielesRecolectadas en GestorUI.");
    }
}
