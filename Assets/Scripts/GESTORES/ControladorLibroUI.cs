using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;

public class ControladorLibroUI : MonoBehaviour
{
    [Header("Referencias Catálogo")]
    public CatalogoRecetas catalogo;

    [Header("Referencias UI - Página Izquierda")]
    public Image imagenRecetaIzquierda;
    [Header("Referencias UI - Página Derecha")]
    public TextMeshProUGUI textoNombreDerecha;
    public TextMeshProUGUI textoDescripcionDerecha;
    public TextMeshProUGUI textoIngredientesDerecha;

    [Header("Referencias UI - Navegación")]
    public Button botonAnterior;
    public Button botonSiguiente;
    public Button botonCerrar;

    [Header("Sonidos Libro")]
    public AudioClip sonidoPasarPagina;
    public AudioClip sonidoCerrarLibro;
    public AudioClip sonidoAbrirLibro;

    [Header("Post Procesado")]
    public PostProcessProfile perfilNormal;
    public PostProcessProfile perfilLibro;

    private PostProcessVolume camaraVolume;

    private int paginaActual = 0;
    private List<PedidoPocionData> recetasMostrables;

    private ControladorJugador controladorJugador;
    private InteraccionJugador interaccionJugador;

    public static bool LibroAbierto { get; private set; } = false;

    private GameObject canvasPrincipalRef = null;

    void Start()
    {
        controladorJugador = FindObjectOfType<ControladorJugador>();
        interaccionJugador = FindObjectOfType<InteraccionJugador>();
        gameObject.SetActive(false);

        if (botonAnterior) botonAnterior.onClick.AddListener(PaginaAnterior);
        if (botonSiguiente) botonSiguiente.onClick.AddListener(PaginaSiguiente);
        if (botonCerrar) botonCerrar.onClick.AddListener(CerrarLibro);

        Camera camPrincipal = Camera.main;
        if (camPrincipal != null)
        {
            camaraVolume = camPrincipal.GetComponent<PostProcessVolume>();
        }
        if (camaraVolume == null)
        {
            Camera cualquierCamara = FindObjectOfType<Camera>();
            if (cualquierCamara != null) camaraVolume = cualquierCamara.GetComponent<PostProcessVolume>();
        }

        if (camaraVolume == null)
        {
            Debug.LogError("ControladorLibroUI no encontró PostProcessVolume en la cámara!", gameObject);
        }
        LibroAbierto = false;
    }

    public void AbrirLibro()
    {
        if (GestorAudio.Instancia != null && sonidoAbrirLibro != null) { GestorAudio.Instancia.ReproducirSonido(sonidoAbrirLibro); }
        if (catalogo == null || catalogo.todasLasRecetas == null) { return; }

        recetasMostrables = catalogo.todasLasRecetas;
        if (recetasMostrables.Count == 0) { /* ... warning ... */ }

        Debug.Log("Abriendo Libro...");
        gameObject.SetActive(true);
        LibroAbierto = true;
        paginaActual = 0;
        MostrarPaginaActual();

        // Ocultar Canvas Principal (HUD, Inventario)
        if (canvasPrincipalRef == null)
        {
            canvasPrincipalRef = GameObject.Find("CanvasPrincipal");
        }
        if (canvasPrincipalRef != null)
        {
            canvasPrincipalRef.SetActive(false);
            Debug.Log("CanvasPrincipal ocultado por Libro.");
        }
        else { Debug.LogWarning("ControladorLibroUI: No se encontró CanvasPrincipal para ocultar."); }

        if (camaraVolume != null) camaraVolume.profile = perfilLibro;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null) jugador.HabilitarMovimiento(false);

        if (interaccionJugador != null && interaccionJugador.uiNombreItemSostenido != null)
        {
            interaccionJugador.uiNombreItemSostenido.gameObject.SetActive(false);
        }
    }

    public void CerrarLibro()
    {
        Debug.Log("Cerrando Libro...");
        gameObject.SetActive(false);
        LibroAbierto = false;

        if (canvasPrincipalRef != null)
        {
            canvasPrincipalRef.SetActive(true);
            Debug.Log("CanvasPrincipal mostrado al Cerrar Libro.");
        }
        else
        {
            canvasPrincipalRef = GameObject.Find("CanvasPrincipal");
            if (canvasPrincipalRef != null) canvasPrincipalRef.SetActive(true);
            else Debug.LogWarning("ControladorLibroUI: No se encontró CanvasPrincipal para mostrar al cerrar.");
        }

        if (camaraVolume != null) camaraVolume.profile = perfilNormal;
        if (GestorAudio.Instancia != null && sonidoCerrarLibro != null) { GestorAudio.Instancia.ReproducirSonido(sonidoCerrarLibro); }

        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null) jugador.HabilitarMovimiento(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void PaginaSiguiente()
    {
        if (paginaActual + 1 < recetasMostrables.Count)
        {
            paginaActual++;
            MostrarPaginaActual();
            if (GestorAudio.Instancia != null && sonidoPasarPagina != null)
            {
                // GestorAudio.Instancia.ReproducirSonido(sonidoPasarPagina);
            }
        }
    }

    void PaginaAnterior()
    {
        if (paginaActual > 0)
        {
            paginaActual--;
            MostrarPaginaActual();
            if (GestorAudio.Instancia != null && sonidoPasarPagina != null)
            {
                // GestorAudio.Instancia.ReproducirSonido(sonidoPasarPagina);
            }
        }
    }

    void MostrarPaginaActual()
    {
        if (recetasMostrables == null || recetasMostrables.Count == 0)
        {
            Debug.LogWarning("No hay recetas para mostrar.");
            if (imagenRecetaIzquierda != null) imagenRecetaIzquierda.enabled = false;
            if (textoNombreDerecha != null) textoNombreDerecha.text = "Libro Vacío";
            if (textoDescripcionDerecha != null) textoDescripcionDerecha.text = "";
            if (textoIngredientesDerecha != null) textoIngredientesDerecha.text = "";
            if (botonAnterior) botonAnterior.interactable = false;
            if (botonSiguiente) botonSiguiente.interactable = false;
            return;
        }

        paginaActual = Mathf.Clamp(paginaActual, 0, recetasMostrables.Count - 1);

        PedidoPocionData recetaActual = recetasMostrables[paginaActual];

        if (imagenRecetaIzquierda != null)
        {
            if (recetaActual.imagenPocion != null)
            {
                imagenRecetaIzquierda.sprite = recetaActual.imagenPocion;
                imagenRecetaIzquierda.enabled = true;
                imagenRecetaIzquierda.preserveAspect = true;
            }
            else { imagenRecetaIzquierda.enabled = false; }
        }

        if (textoNombreDerecha != null) { textoNombreDerecha.text = recetaActual.nombreResultadoPocion; textoNombreDerecha.gameObject.SetActive(true); }
        if (textoDescripcionDerecha != null) { textoDescripcionDerecha.text = recetaActual.descripcionPocion; textoDescripcionDerecha.gameObject.SetActive(true); }
        if (textoIngredientesDerecha != null)
        {
            string textoIng = "Ingredientes:\n";
            if (recetaActual.ingredientesRequeridos != null)
            {
                foreach (var ing in recetaActual.ingredientesRequeridos)
                {
                    // ¡CORRECCIÓN CLAVE AQUÍ! 'ing' ya es un string, no necesita .nombreIngrediente.
                    if (ing != null) textoIng += $"- {ing}\n";
                    else textoIng += "- ???\n";
                }
            }
            else { textoIng += "- Desconocidos"; }

            textoIngredientesDerecha.text = textoIng;
            textoIngredientesDerecha.gameObject.SetActive(true);
        }

        if (botonAnterior) botonAnterior.interactable = (paginaActual > 0);
        if (botonSiguiente) botonSiguiente.interactable = (paginaActual + 1 < recetasMostrables.Count);
    }

    void Update()
    {
        if (!LibroAbierto) return;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            PaginaSiguiente();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            PaginaAnterior();
        }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarLibro();
        }
    }
}