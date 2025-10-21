using UnityEngine;
using UnityEngine.SceneManagement; // Para cargar escenas
using UnityEngine.UI;            // Para Button (aunque no lo usemos directamente aqu� si asignamos por Inspector)

public class ControladorMenuPrincipal : MonoBehaviour
{
    [Header("Configuraci�n Escenas")]
    [Tooltip("Nombre EXACTO de la escena principal del juego.")]
    public string nombreEscenaJuego = "EscenarioPrueba"; // <-- CAMBIA ESTO si tu escena se llama diferente

    [Header("Referencias UI")]
    [Tooltip("Arrastra aqu� el GameObject del Panel de Ayuda.")]
    public GameObject panelAyuda; // Para activar/desactivar

    public Button botonContinuar;
    public Button botonNuevaPartida; // Reemplaza o complementa a BotonJugar
    public GameObject panelConfirmacionNuevaPartida; // Panel con botones S�/No

    // Aseg�rate de que el panel empieza desactivado desde el editor
    void Start()
    {
                PlayerPrefs.DeleteAll();
                        PlayerPrefs.Save();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        bool hayGuardado = PlayerPrefs.HasKey("ExisteGuardado") && PlayerPrefs.GetInt("ExisteGuardado") == 1;
        if (botonContinuar != null) botonContinuar.interactable = hayGuardado;
        if (panelConfirmacionNuevaPartida != null) panelConfirmacionNuevaPartida.SetActive(false);
    }
    // --- M�todos para los Botones ---

    public void BotonJugarPresionado()
    {
        Debug.Log($"Cargando escena: {nombreEscenaJuego}...");
        // Opcional: A�adir un efecto de fade ANTES de cargar
        if (!string.IsNullOrEmpty(nombreEscenaJuego))
        {
            GestorJuego.CargarEscenaConPantallaDeCarga(nombreEscenaJuego);
        }
        else
        {
            Debug.LogError("�Nombre de la escena de juego no especificado en MainMenuController!");
        }
    }

    public void BotonAyudaPresionado()
    {
        Debug.Log("Mostrando Panel de Ayuda...");
        if (panelAyuda != null)
        {
            panelAyuda.SetActive(true); // Muestra el panel
        }
        else
        {
            Debug.LogError("�Panel de Ayuda no asignado en MainMenuController!");
        }
    }

    public void BotonSalirPresionado()
    {
        Debug.Log("Saliendo del juego...");
        // Si est�s en el editor de Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        // Si est�s en una build compilada
#else
        Application.Quit();
#endif
    }

    public void BotonCerrarAyudaPresionado()
    {
        Debug.Log("Cerrando Panel de Ayuda...");
        if (panelAyuda != null)
        {
            panelAyuda.SetActive(false); // Oculta el panel
        }
    }

    public void BotonContinuarPresionado()
    {
        GestorJuego.CargarEscenaConPantallaDeCarga(nombreEscenaJuego);
    }

    public void BotonNuevaPartidaPresionado()
    {
        bool hayG = PlayerPrefs.HasKey("ExisteGuardado") && PlayerPrefs.GetInt("ExisteGuardado") == 1;
        if (hayG && panelConfirmacionNuevaPartida != null)
        {
            panelConfirmacionNuevaPartida.SetActive(true);
        }
        else
        {
            ConfirmarNuevaPartida();
        }
    }

    public void ConfirmarNuevaPartida()
    {
        Debug.Log("Borrando datos para Nueva Partida...");
        PlayerPrefs.DeleteKey("ExisteGuardado");
        PlayerPrefs.DeleteKey("DiaActual");
        PlayerPrefs.DeleteKey("DineroActual");
        PlayerPrefs.DeleteKey("HoraActual");
        PlayerPrefs.DeleteKey("StockIngredientes");
        /* Borrar otras claves */
        PlayerPrefs.Save();
        if (panelConfirmacionNuevaPartida != null) panelConfirmacionNuevaPartida.SetActive(false);
        GestorJuego.CargarEscenaConPantallaDeCarga(nombreEscenaJuego);
    }

    public void CancelarNuevaPartida()
    {
        if (panelConfirmacionNuevaPartida != null) panelConfirmacionNuevaPartida.SetActive(false);
    }


}