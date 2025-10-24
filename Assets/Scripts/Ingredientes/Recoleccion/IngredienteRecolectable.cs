using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq; // Incluido ya que estaba en el código que mostraste.

[RequireComponent(typeof(Collider))]
public class IngredienteRecolectable : MonoBehaviour
{
    [Tooltip("La clave del ingrediente (string) que se usa en ItemCatalog.")]
    // Esta variable DEBE ser asignada por el Gestor (GestorRecoleccionBosque, etc.)
    public string claveIngrediente;

    // Datos del ítem (nombre, etc.) se buscarán en el Catálogo usando la clave
    private ItemCatalog.ItemData datosItem = null;

    public string textoIndicador = "Recolectar [E]";
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;
    [HideInInspector] public PuntoSpawnRecoleccion puntoOrigen = null;

    [Header("Referencias UI (Asignar en GestorUI)")]
    [Tooltip("Normalmente se asigna desde el GestorUI o el ControladorJugador.")]
    public TextMeshProUGUI mensajeTemporalUI; // 🛑 CORREGIDO: Cambiado TextMeshProUGPU a TextMeshProUGUI

    private Coroutine mensajeCoroutine;

    void Start()
    {
        // 🛑 LÓGICA ELIMINADA DE START() 🛑
        // Start() ya no contendrá la lógica de obtención de ItemData.
        // Ahora, solo verificaremos si falló la Inicialización (por si acaso).
        if (datosItem == null && !string.IsNullOrEmpty(claveIngrediente))
        {
            // Si la clave ya estaba asignada en el inspector pero falló la búsqueda en Start(), 
            // intentamos inicializar de nuevo (esto solo ayuda a objetos NO spawneados).
            Inicializar(claveIngrediente);
        }
        else if (datosItem == null && string.IsNullOrEmpty(claveIngrediente))
        {
            // Si el objeto fue spawneado, la responsabilidad recae en el Gestor.
            // El Gestor DEBE llamar a Inicializar() inmediatamente después de Instantiate.
            Debug.LogWarning($"[Recolectable] Objeto {gameObject.name} spawneado sin clave ni Inicializar(). El Gestor debe corregir esto.");
        }
    }

    /// <summary>
    /// Método CRÍTICO: Debe ser llamado por el Gestor de Recolección inmediatamente 
    /// después de instanciar el prefab para asignar la clave y obtener los datos.
    /// Esta es la única forma robusta de asegurar el orden de ejecución para objetos spawneados.
    /// </summary>
    /// <param name="itemKey">La clave del ingrediente a buscar en el catálogo.</param>
    public void Inicializar(string itemKey)
    {
        if (GestorJuego.Instance == null)
        {
            Debug.LogError("[Recolectable] No se puede inicializar. GestorJuego.Instance es NULL.");
            return;
        }

        claveIngrediente = itemKey;

        // 1. Obtener el ItemData usando la clave.
        if (!string.IsNullOrEmpty(claveIngrediente))
        {
            datosItem = GestorJuego.Instance.catalogoMaestro.GetItemData(claveIngrediente);
        }

        // 2. Reportar error (sólo si no se encontró el Gestor o la clave es nula/vacía)
        if (datosItem == null)
        {
            // Este es el mensaje que indica que la clave no existe en el catálogo.
            Debug.LogError($"[Recolectable] ❌ Inicialización fallida. No se encontró ItemData para la clave '{claveIngrediente}'. Verifique que la clave exista en el ItemCatalog.");
            // No destruimos, permitimos que la recolección funcione usando solo la clave string.
        }
        else
        {
            // Éxito
            Debug.Log($"[Recolectable] ✅ Inicializado correctamente con ItemData: {datosItem.nombreItem}.");
        }
    }

    // =========================================================================
    // MÉTODOS DE UI/INFORMACIÓN
    // =========================================================================

    public void MostrarInformacion()
    {
        // Si los datosItem son null, al menos usamos la clave para el nombre
        string nombreMostrar = (datosItem != null) ? datosItem.nombreItem : claveIngrediente;

        // Se mantiene la verificación de datosItem != null por seguridad.
        if (prefabCanvasInfo == null || string.IsNullOrEmpty(nombreMostrar)) return;

        if (canvasInfoActual == null)
        {
            // ... (Lógica de instanciación del Canvas)
            Vector3 offset = Vector3.up * 0.5f;
            Collider col = GetComponent<Collider>();
            Vector3 basePos = (col != null) ? col.bounds.center : transform.position;
            canvasInfoActual = Instantiate(prefabCanvasInfo, basePos + offset, Quaternion.identity);
        }

        if (canvasInfoActual != null)
        {
            // ... (Lógica para actualizar el texto en el Canvas)
            // NOTA: Es necesario que el script InfoCanvasUI exista en el prefab.
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();

            if (uiScript != null)
            {
                if (uiScript.textoNombre != null) uiScript.textoNombre.text = $"{nombreMostrar}\n[{textoIndicador}]";
                if (uiScript.textoCantidad != null) uiScript.textoCantidad.gameObject.SetActive(false);
            }
            else
            {
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = $"{nombreMostrar}\n[{textoIndicador}]";
            }
            canvasInfoActual.SetActive(true);
        }
    }

    public void OcultarInformacion()
    {
        if (canvasInfoActual != null)
        {
            Destroy(canvasInfoActual); // Destruir el canvas de info
            canvasInfoActual = null;
        }
    }

    void OnDestroy()
    {
        if (canvasInfoActual != null)
        {
            Destroy(canvasInfoActual);
        }
    }

    // =========================================================================
    // LÓGICA DE RECOLECCIÓN
    // =========================================================================

    public void Recolectar()
    {
        // 🔴 Manejo del error: Si datosItem es null, significa que falló la búsqueda en el catálogo.
        // Pero la clave string DEBERÍA estar disponible para la recolección.

        if (string.IsNullOrEmpty(claveIngrediente))
        {
            Debug.LogError("🔴 ERROR: Recolección fallida. La claveIngrediente está vacía. El objeto no se puede añadir.");
            return;
        }

        // Si datosItem es null, logueamos la advertencia, pero continuamos porque tenemos la clave.
        if (datosItem == null)
        {
            // Este es el log de error que estabas viendo, pero ahora NO bloquea la recolección.
            Debug.LogError($"🔴 ADVERTENCIA: ItemData es NULL para la clave '{claveIngrediente}'. Verifique el catálogo. La recolección procederá usando solo la clave string.");
        }


        Debug.Log($"Recolectado: {claveIngrediente}");
        bool anadido = false;

        if (GestorJuego.Instance != null)
        {
            // ¡Paso CLAVE!: USAR LA CLAVE (STRING) PARA AÑADIR AL STOCK, NO datosItem.
            GestorJuego.Instance.AnadirStockTienda(claveIngrediente, 1);
            anadido = true;

            // Lógica de Cooldown
            if (puntoOrigen != null)
            {
                puntoOrigen.diaUltimaRecoleccion = GestorJuego.Instance.diaActual;
                puntoOrigen.objetoInstanciadoActual = null;
                Debug.Log($"Cooldown aplicado a {puntoOrigen.name} hasta el Día {puntoOrigen.diaUltimaRecoleccion + 1}");
            }
        }
        else
        {
            Debug.LogError("No se encontró GestorJuego para añadir al stock.");
        }

        if (anadido)
        {
            OcultarInformacion();

            // 🛑 PREVENCIÓN DE NRE: Si datosItem es null, usamos la clave string para el mensaje.
            string nombreAMostrar = (datosItem != null) ? datosItem.nombreItem : claveIngrediente;
            MostrarMensajeTemporal($"Has añadido **+1 {nombreAMostrar}** al Stock.");

            Destroy(gameObject);
        }
    }

    // =========================================================================
    // MÉTODOS DE UI TEMPORAL
    // =========================================================================

    private void MostrarMensajeTemporal(string mensaje, float duracion = 2f)
    {
        if (mensajeTemporalUI == null)
        {
            Debug.Log($"Mensaje temporal (UI no asignada): {mensaje}");
            return;
        }

        if (mensajeCoroutine != null)
            StopCoroutine(mensajeCoroutine);

        mensajeCoroutine = StartCoroutine(MostrarMensajeCoroutine(mensaje, duracion));
    }

    private IEnumerator MostrarMensajeCoroutine(string mensaje, float duracion)
    {
        if (mensajeTemporalUI != null)
        {
            mensajeTemporalUI.text = mensaje;
            mensajeTemporalUI.gameObject.SetActive(true);
            yield return new WaitForSeconds(duracion);
            mensajeTemporalUI.gameObject.SetActive(false);
        }
    }
}
