using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ControladorMenuPrincipal : MonoBehaviour
{
    [Header("Configuración Escenas")]
    [Tooltip("Nombre EXACTO de la escena principal del juego.")]
    public string nombreEscenaJuego = "EscenarioPrueba";

    [Header("Referencias UI")]
    [Tooltip("Arrastra aquí el GameObject del Panel de Ayuda.")]
    public GameObject panelAyuda;

    public Button botonContinuar;
    public Button botonNuevaPartida;
    [Tooltip("Asegúrate de asignar el GameObject del panel completo, NO solo un botón.")]
    public GameObject panelConfirmacionNuevaPartida;

    void Start()
    {
        // 🔑 VERIFICACIÓN CRÍTICA: Confirmar que el Singleton existe después de la carga
        if (GestorCarga.Instancia == null)
        {
            Debug.LogError("🚨 GESTORCARGA NO ENCONTRADO al iniciar el menú. Debe estar en DontDestroyOnLoad.");
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Lógica de PlayerPrefs para habilitar Continuar
        bool hayGuardado = PlayerPrefs.HasKey("ExisteGuardado") && PlayerPrefs.GetInt("ExisteGuardado") == 1;

        if (botonContinuar != null) botonContinuar.interactable = hayGuardado;
        if (panelConfirmacionNuevaPartida != null) panelConfirmacionNuevaPartida.SetActive(false);
    }

    // --- Lógica de Carga de Escena CENTRAL (Delegada al Singleton) ---

    private void IniciarCargaDeJuego()
    {
        if (GestorCarga.Instancia == null)
        {
            // Este log aparece si el GestorCarga se perdió en la carga previa (PantallaCarga -> MenuPrincipal)
            Debug.LogError("🚨 ERROR CRÍTICO: El GestorCarga (Singleton) no está inicializado. ¡Carga fallida!");
            return;
        }

        if (string.IsNullOrEmpty(nombreEscenaJuego))
        {
            Debug.LogError("¡Nombre de la escena de juego no especificado!");
            return;
        }

        // LLAMADA CLAVE: El Singleton almacena el destino y carga la PantallaCarga.
        GestorCarga.Instancia.IniciarCarga(nombreEscenaJuego);
    }


    // --- Métodos para los Botones ---

    public void BotonJugarPresionado()
    {
        IniciarCargaDeJuego();
    }

    public void BotonContinuarPresionado()
    {
        Debug.Log("[Menu] Intentando Continuar Partida. Iniciando carga...");
        IniciarCargaDeJuego();
    }

    public void BotonNuevaPartidaPresionado()
    {
        bool hayG = PlayerPrefs.HasKey("ExisteGuardado") && PlayerPrefs.GetInt("ExisteGuardado") == 1;

        Debug.Log($"[Menu] Botón Nueva Partida presionado. ¿Existe guardado previo? = {hayG}");

        // Si hay guardado Y la referencia UI es correcta (debe ser el PanelConfirmacion, no un botón)
        if (hayG && panelConfirmacionNuevaPartida != null)
        {
            panelConfirmacionNuevaPartida.SetActive(true);
        }
        else
        {
            // Si NO hay guardado, o si la referencia UI es nula (el caso más probable)
            ConfirmarNuevaPartida();
        }
    }

    public void ConfirmarNuevaPartida()
    {
        Debug.Log("[Menu] Iniciando HARD RESET de PlayerPrefs y cargando escena para Nueva Partida...");

        // 🚀 HARD RESET: Limpia los datos de la partida anterior.
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("ExisteGuardado", 1);
        PlayerPrefs.Save();

        // Oculta el panel de confirmación (por si estaba visible).
        if (panelConfirmacionNuevaPartida != null) panelConfirmacionNuevaPartida.SetActive(false);

        // Carga la escena de juego a través del Singleton
        IniciarCargaDeJuego();
    }

    public void CancelarNuevaPartida()
    {
        if (panelConfirmacionNuevaPartida != null) panelConfirmacionNuevaPartida.SetActive(false);
    }

    // --- Métodos UI Estándar ---

    public void BotonAyudaPresionado()
    {
        Debug.Log("Mostrando Panel de Ayuda...");
        if (panelAyuda != null) panelAyuda.SetActive(true);
        else Debug.LogError("¡Panel de Ayuda no asignado en MainMenuController!");
    }

    public void BotonCerrarAyudaPresionado()
    {
        Debug.Log("Cerrando Panel de Ayuda...");
        if (panelAyuda != null) panelAyuda.SetActive(false);
    }

    public void BotonSalirPresionado()
    {
        Debug.Log("Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}