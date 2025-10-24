// GestorCarga.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class GestorCarga : MonoBehaviour
{
    // Singleton pattern: Acceso global y única instancia.
    public static GestorCarga Instancia { get; private set; }

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre EXACTO de la escena de la pantalla de carga.")]
    [SerializeField]
    private string nombreEscenaCarga = "PantallaCarga"; // Puedes configurarlo en el Inspector

    // Variable para almacenar la escena final a la que debe ir el juego (Ej: "EscenarioPrueba")
    private string _escenaDestino = "";

    void Awake()
    {
        // Implementación del Singleton
        if (Instancia == null)
        {
            Instancia = this;
            // 🔑 CRÍTICO: Este objeto se mantendrá vivo al cargar nuevas escenas.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si ya existe una instancia, destruye este nuevo objeto.
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Método público llamado desde el menú o cualquier parte del juego para iniciar la carga.
    /// </summary>
    /// <param name="escenaDestino">El nombre de la escena final a cargar asíncronamente.</param>
    public void IniciarCarga(string escenaDestino)
    {
        if (string.IsNullOrEmpty(escenaDestino))
        {
            Debug.LogError("[GestorCarga] Nombre de escena destino no válido.");
            return;
        }

        // 1. Almacenar el destino en esta instancia persistente.
        _escenaDestino = escenaDestino;
        Debug.Log($"[GestorCarga] Destino de carga fijado a: {_escenaDestino}. Cargando PantallaCarga...");

        // 2. Cargar la Pantalla de Carga de forma sincrónica.
        SceneManager.LoadScene(nombreEscenaCarga);
    }

    /// <summary>
    /// Llamado por el ControladorPantallaCarga para obtener el destino real.
    /// </summary>
    /// <returns>El nombre de la escena a cargar asíncronamente.</returns>
    public string ObtenerDestino()
    {
        // Si el juego inicia en la PantallaCarga sin destino (ej. desde el Editor), va al menú.
        if (string.IsNullOrEmpty(_escenaDestino))
        {
            return "MenuPrincipal";
        }
        return _escenaDestino;
    }
}