using UnityEngine;
using TMPro;

public class FuenteIngredientes : MonoBehaviour
{
    // =========================================================================
    // CAMBIO CRÍTICO: Usar la clave (string) en lugar del ScriptableObject.
    // =========================================================================
    [Tooltip("La clave del ingrediente (string) que se usa en ItemCatalog.")]
    public string claveIngrediente;

    // Cacheamos los datos del catálogo para UI/mensajería.
    private ItemCatalog.ItemData datosItem = null;

    public GameObject objetoModelo;

    [Header("UI Información (Opcional)")]
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;

    // Llevar registro de cuántos ingredientes entregó esta fuente al jugador
    private int cantidadEntregadaAlJugador = 0;

    void Start()
    {
        // 1. Obtener la referencia de ItemData al inicio usando la CLAVE.
        if (GestorJuego.Instance != null && !string.IsNullOrEmpty(claveIngrediente))
        {
            datosItem = GestorJuego.Instance.catalogoMaestro.GetItemData(claveIngrediente);
        }
        else
        {
            Debug.LogError($"[FuenteIngredientes] FATAL: Clave '{claveIngrediente}' vacía o GestorJuego/ItemCatalog no encontrado en {gameObject.name}.");
        }

        // Inicializar el canvas de información y mantenerlo oculto
        if (canvasInfoActual == null && prefabCanvasInfo != null)
        {
            canvasInfoActual = Instantiate(prefabCanvasInfo, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            canvasInfoActual.SetActive(false);
        }
    }

    public void MostrarInformacion()
    {
        // Usamos el caché de datos. Si es nulo, la clave no es válida.
        if (datosItem == null)
        {
            Debug.LogError($"FATAL: No se encontró ItemData para la clave: {claveIngrediente} en {gameObject.name}");
            return;
        }

        if (canvasInfoActual != null)
        {
            // Posicionar y orientar el canvas
            canvasInfoActual.transform.position = transform.position + Vector3.up * 1.5f;
            if (Camera.main != null)
            {
                canvasInfoActual.transform.LookAt(Camera.main.transform);
                canvasInfoActual.transform.forward *= -1;
            }
            canvasInfoActual.SetActive(true);

            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null)
                {
                    // Usamos el nombre legible del catálogo
                    uiScript.textoNombre.text = datosItem.nombreItem;
                }
                int stockActual = 0;
                if (GestorJuego.Instance != null)
                {
                    // ✅ CORRECCIÓN CLAVE: Usar la claveIngrediente (string)
                    stockActual = GestorJuego.Instance.ObtenerStockTienda(claveIngrediente);
                }
                uiScript.textoCantidad.text = $"Disp.: {stockActual}";
                uiScript.textoCantidad.gameObject.SetActive(true);
            }
        }
        else if (prefabCanvasInfo == null)
        {
            Debug.LogWarning($"PrefabCanvasInfo no asignado en {gameObject.name}, no se puede mostrar info.");
        }
    }

    public void OcultarInformacion()
    {
        if (canvasInfoActual != null)
        {
            canvasInfoActual.SetActive(false);
        }
    }

    /// <summary>
    /// Intenta reducir el stock global. Devuelve el ItemData si la recogida es exitosa.
    /// </summary>
    public ItemCatalog.ItemData IntentarRecoger()
    {
        // Usamos el caché de datos. Si es nulo, la clave no es válida.
        if (datosItem == null)
        {
            Debug.LogError($"Fuente {gameObject.name} no tiene ItemData/Clave asignada!");
            return null;
        }

        if (GestorJuego.Instance == null)
        {
            Debug.LogError("FATAL: No se encontró GestorJuego.");
            return null;
        }

        // 1. Verificar stock
        // ✅ CORRECCIÓN CLAVE: Usar la claveIngrediente (string)
        int cantidadEnStock = GestorJuego.Instance.ObtenerStockTienda(claveIngrediente);

        if (cantidadEnStock > 0)
        {
            // 2. Intentar consumir 1 unidad del stock global
            // ✅ CORRECCIÓN CLAVE: Usar la claveIngrediente (string)
            if (GestorJuego.Instance.ConsumirStockTienda(claveIngrediente))
            {
                cantidadEntregadaAlJugador += 1;
                ActualizarVisuales();

                Debug.Log($"[Recolección Éxito] 1 unidad de {datosItem.nombreItem} reservada. InteraccionJugador la añadirá.");

                // Devuelve los datos para que InteraccionJugador se encargue de la adición al inventario.
                return datosItem;
            }
            else
            {
                Debug.LogWarning($"Fallo al consumir stock de {datosItem.nombreItem}. Revisar lógica de GestorJuego.");
                return null;
            }
        }
        else
        {
            Debug.Log($"¡No queda {datosItem.nombreItem} en el stock!");
            return null;
        }
    }


    public void DevolverIngrediente()
    {
        if (datosItem == null) return;

        // Asumo que InventoryManager.Instance existe
        if (GestorJuego.Instance != null)
        {
            // ✅ CORRECCIÓN CLAVE: Usar la claveIngrediente (string)
            GestorJuego.Instance.AnadirStockTienda(claveIngrediente, 1);
            ActualizarVisuales();

            Debug.Log($"Devuelto 1 de {datosItem.nombreItem} a la tienda.");
        }
        else
        {
            Debug.LogWarning("GestorJuego no encontrado para devolver el ingrediente.");
        }
    }


    public void RegistrarIngredienteDevueltoDesdeCaldero()
    {
        cantidadEntregadaAlJugador += 1;
    }

    void ActualizarVisuales()
    {
        if (datosItem == null) return;

        // La UI solo se actualiza si está activa (visible)
        if (canvasInfoActual != null && canvasInfoActual.activeSelf)
        {
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoCantidad != null)
                {
                    int stockActual = 0;
                    if (GestorJuego.Instance != null)
                    {
                        // ✅ CORRECCIÓN CLAVE: Usar la claveIngrediente (string)
                        stockActual = GestorJuego.Instance.ObtenerStockTienda(claveIngrediente);
                    }
                    uiScript.textoCantidad.text = $"Disp.: {stockActual}";
                }
            }
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