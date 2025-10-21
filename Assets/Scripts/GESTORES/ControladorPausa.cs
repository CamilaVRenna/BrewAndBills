using UnityEngine;
using UnityEngine.SceneManagement; // Para cambiar de escena
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class ControladorPausa : MonoBehaviour
{
    [Tooltip("Arrastra aqu� el GameObject PanelPausa desde la jerarqu�a del prefab del jugador.")]
    public GameObject panelPausa; // Referencia al panel UI

    [Header("Post Procesado")]
    [Tooltip("Arrastra aqu� el asset del perfil SIN desenfoque.")]
    public PostProcessProfile perfilNormal; // <<--- Asignar PP_Normal aqu�
    [Tooltip("Arrastra aqu� el asset del perfil CON desenfoque.")]
    public PostProcessProfile perfilPausa; // <<--- Asignar PP_Desenfocado aqu�

    private PostProcessVolume camaraVolume; // Referencia al Volume en la c�mara

    // Variable est�tica para saber si el juego est� pausado (accesible desde otros scripts si es necesario)
    public static bool JuegoPausado { get; private set; } = false;

    private GameObject canvasPrincipalRef = null; // <<--- A�ADE ESTA L�NEA

    [Header("BotonesEX")]
    [Tooltip("Arrastra aqu� el boton para cargar la partida.")]
    public Button botonCargarPartida;

    void Start()
    {
        // Asegurarse de que el panel est� oculto al empezar la escena
        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }
        else
        {
            Debug.LogError("�Panel de Pausa no asignado en ControladorPausa!", this.gameObject);
        }
        // Asegurar que el tiempo corra normal al inicio de la escena
        Time.timeScale = 1f;
        JuegoPausado = false; // Resetear estado al cargar escena
                              // Asegurar que el cursor est� bloqueado al inicio de escenas de juego
        if (SceneManager.GetActiveScene().name != "MenuPrincipal") // No bloquear en men� principal
        {
            Cursor.lockState = CursorLockMode.Locked;
           Cursor.visible = false;
        }

        // Buscar el Volume en la c�mara del jugador
        // Asumiendo que este script est� en el objeto Jugador ra�z
        Camera cam = GetComponentInChildren<Camera>(); // Busca la c�mara hija
        if (cam != null)
        {
            camaraVolume = cam.GetComponent<PostProcessVolume>();
        }
        if (camaraVolume == null)
        {
            Debug.LogError("ControladorPausa no encontr� PostProcessVolume en la c�mara hija!", gameObject);
        }
        else
        {
            // Asegurar que empieza con el perfil normal
            if (perfilNormal != null) camaraVolume.profile = perfilNormal;
        }

        bool hayGuardado = PlayerPrefs.HasKey("ExisteGuardado") && PlayerPrefs.GetInt("ExisteGuardado") == 1;
        if (botonCargarPartida != null) botonCargarPartida.interactable = hayGuardado;

    }

    void Update()
    {
        // Detectar la tecla Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!JuegoPausado)
            {
                PausarJuego();
            }
            else
            {
                ReanudarJuego();
                // Asegura que el cursor se oculte y se bloquee al cerrar con Escape
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

void PausarJuego()
{
    JuegoPausado = true;
    Time.timeScale = 0f;

    if (panelPausa != null)
        panelPausa.SetActive(true);

    // Cambiar el perfil de postprocesado si está asignado
    if (camaraVolume != null && perfilPausa != null)
        camaraVolume.profile = perfilPausa;

    // Desactivar movimiento del jugador
    ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
    if (jugador != null)
        jugador.HabilitarMovimiento(false);

    // --- HABILITAR CURSOR ---
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}

    // Método público para ser llamado por el botón
public void ReanudarJuego()
{
    JuegoPausado = false;
    Time.timeScale = 1f;
    if (panelPausa != null)
        panelPausa.SetActive(false);

    if (camaraVolume != null && perfilNormal != null)
        camaraVolume.profile = perfilNormal;

    ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
    if (jugador != null)
        jugador.HabilitarMovimiento(true);

    // --- OCULTAR CURSOR ---
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}


    // M�todo p�blico para el bot�n "Men� Principal"
    public void IrMenuPrincipal()
    {
        Debug.Log("Volviendo al Men� Principal...");
        // �MUY IMPORTANTE! Restaurar la escala de tiempo ANTES de cargar la escena
        Time.timeScale = 1f;
        JuegoPausado = false; // Resetear estado

        // --- MOSTRAR HUD ANTES DE SALIR (POR SI ACASO) --- <<<--- A�ADIDO
        if (canvasPrincipalRef != null) canvasPrincipalRef.SetActive(true);
        // -------------------------------------------------

        // Asegurar perfil normal antes de salir
        if (camaraVolume != null) camaraVolume.profile = perfilNormal;
        GestorJuego.CargarEscenaConPantallaDeCarga("MainMenu");

        // Usar el cargador de escenas con pantalla de carga
        // Aseg�rate de que el nombre "MainMenu" es correcto
        GestorJuego.CargarEscenaConPantallaDeCarga("MenuPrincipal");
    }

    // M�todo p�blico para el bot�n "Salir del Juego"
    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        // C�digo para salir (igual que en MainMenuController)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Llamado por el bot�n "Cargar Partida" del men� de pausa
    public void BotonCargarPartidaPresionado()
    {
        Debug.Log("Cargando �ltimo guardado (Reiniciando d�a)...");
        Time.timeScale = 1f;
        JuegoPausado = false;
        // Recargar la escena actual har� que GestorJuego.CargarDatos se ejecute
        GestorJuego.CargarEscenaConPantallaDeCarga(SceneManager.GetActiveScene().name);
    }

}