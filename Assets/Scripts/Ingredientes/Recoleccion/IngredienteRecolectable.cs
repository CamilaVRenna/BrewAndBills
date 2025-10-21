using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq; // Necesario para Linq si lo usaras, aunque no es estrictamente necesario aquí.

[RequireComponent(typeof(Collider))]
public class IngredienteRecolectable : MonoBehaviour
{
    // =========================================================================
    // CORRECCIÓN CRÍTICA: Cambiado DatosIngrediente por la clave (string)
    // =========================================================================
    [Tooltip("La clave del ingrediente (string) que se usa en ItemCatalog.")]
    public string claveIngrediente; // <-- ¡NUEVA VARIABLE CLAVE!

    // Datos del ítem (nombre, etc.) se buscarán en el Catálogo usando la clave
    private ItemCatalog.ItemData datosItem = null;

    public string textoIndicador = "Recolectar [E]";
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;
    [HideInInspector] public PuntoSpawnRecoleccion puntoOrigen = null;

    [Header("Referencias UI (Asignar en GestorUI)")]
    [Tooltip("Normalmente se asigna desde el GestorUI o el ControladorJugador.")]
    // NOTA: Si esto se usa para mensajes globales, debe ser asignado por un GestorUI
    public TextMeshProUGUI mensajeTemporalUI;

    private Coroutine mensajeCoroutine;

    void Start()
    {
        // Obtener el ItemData al iniciar usando la clave.
        if (GestorJuego.Instance != null && !string.IsNullOrEmpty(claveIngrediente))
        {
            datosItem = GestorJuego.Instance.catalogoMaestro.GetItemData(claveIngrediente);
        }
        else
        {
            Debug.LogError($"[Recolectable] No se pudo obtener ItemData para la clave '{claveIngrediente}' en {gameObject.name}. ¿Clave vacía o GestorJuego ausente?");
        }
    }

    // =========================================================================
    // MÉTODOS DE UI/INFORMACIÓN
    // =========================================================================

    public void MostrarInformacion()
    {
        if (prefabCanvasInfo == null || datosItem == null) return;

        // El resto de la lógica de UI usa ahora datosItem.nombreItem
        string nombreMostrar = datosItem.nombreItem;

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

    // =========================================================================
    // LÓGICA DE RECOLECCIÓN (El antiguo método Interactuar)
    // =========================================================================

    public void Recolectar()
    {
        if (datosItem == null)
        {
            Debug.LogError($"Intento de recolectar objeto sin ItemData. Clave: {claveIngrediente}");
            return;
        }

        Debug.Log($"Recolectado: {datosItem.nombreItem}");
        bool anadido = false;

        if (GestorJuego.Instance != null)
        {
            // ¡USAR LA CLAVE (STRING) PARA AÑADIR AL STOCK!
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
            MostrarMensajeTemporal($"Has añadido **+1 {datosItem.nombreItem}** al Stock.");
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