using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para la b�squeda por nombre.

/// <summary>
/// Contiene toda la informaci�n y modelos de todos los �tems del juego,
/// centralizando la gesti�n de datos en un solo ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "ItemCatalog", menuName = "Inventario/Item Catalog Centralizado")]
public class ItemCatalog : ScriptableObject
{
    // =========================================================================
    // ENUMERADOR DE TIPOS DE �TEMS (ACTUALIZADO)
    // =========================================================================

    /// <summary>
    /// Define el tipo fundamental del �tem, permitiendo l�gica de juego espec�fica 
    /// para crafteo, uso y equipamiento.
    /// </summary>
    public enum TipoDeItem
    {
        INGREDIENTE,         // Materiales base, recolectables, usados en recetas.
        INGREDIENTE_PROCESADO, // Material intermedio (ej: lingotes, harina). Para implementar l�gica a futuro.
        FRASCO,              // Contenedor vac�o o base l�quida, generalmente usado en crafteo de pociones.
        POCION,              // Consumible bebible que aplica un efecto inmediato.
        HERRAMIENTA          // Objeto que se puede usar o equipar y que puede tener durabilidad (ej: hacha, pico).
    }

    // =========================================================================
    // ESTRUCTURA INTERNA DE DATOS
    // =========================================================================

    [System.Serializable]
    public class ItemData
    {
        [Header("Informaci�n General")]
        [Tooltip("El tipo fundamental del item (INGREDIENTE, POCION, HERRAMIENTA, etc.).")]
        public TipoDeItem tipoDeItem = TipoDeItem.INGREDIENTE; // <-- �NUEVO CAMPO CRUCIAL!

        [Tooltip("Nombre �nico y legible del item.")]
        public string nombreItem = "Item Base";

        [Tooltip("Prefab del modelo 3D que se instanciar� al sostener el item.")]
        public GameObject prefabModelo3D;

        [Tooltip("Rotaci�n adicional en Euler (X,Y,Z) para que el item se vea bien en la mano.")]
        public Vector3 rotacionEnMano = Vector3.zero;

        [Header("Espec�fico de Recolecci�n/Ingredientes")]
        [Tooltip("Si es un INGREDIENTE, arrastra aqu� el PREFAB del objeto recolectable.")]
        public GameObject prefabRecolectable;
    }

    // =========================================================================
    // PROPIEDADES DEL CAT�LOGO
    // =========================================================================

    [Header("Lista Maestra de �tems")]
    [Tooltip("La lista de todos los items ScriptableObject en el juego. Arrastra las estructuras aqu�.")]
    [SerializeField]
    private List<ItemData> allItems = new List<ItemData>();

    // Diccionario para b�squeda r�pida por nombre (se llena en Initialize).
    private Dictionary<string, ItemData> itemMap;

    // =========================================================================
    // M�TODOS DE GESTI�N
    // =========================================================================

    /// <summary>
    /// Inicializa el cat�logo, llenando el diccionario de b�squeda. 
    /// Debe ser llamado una vez al inicio del juego.
    /// </summary>
    public void Initialize()
    {
        if (itemMap != null) return; // Ya inicializado.

        itemMap = new Dictionary<string, ItemData>();

        foreach (ItemData item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.nombreItem))
            {
                if (itemMap.ContainsKey(item.nombreItem))
                {
                    Debug.LogError($"[ItemCatalog] Error: �tem duplicado encontrado: {item.nombreItem}");
                    continue;
                }
                itemMap[item.nombreItem] = item;
            }
        }
        Debug.Log($"[ItemCatalog] Inicializaci�n completa. {itemMap.Count} �tems cargados.");
    }

    /// <summary>
    /// Devuelve la estructura de datos completa (ItemData) para el �tem dado su nombre.
    /// </summary>
    /// <param name="itemName">El nombre �nico del �tem.</param>
    /// <returns>La estructura ItemData, o null si no se encuentra.</returns>
    public ItemData GetItemData(string itemName)
    {
        if (itemMap == null) Initialize();

        if (string.IsNullOrEmpty(itemName)) return null;

        if (itemMap.TryGetValue(itemName, out ItemData itemData))
        {
            return itemData;
        }

        Debug.LogWarning($"[ItemCatalog] No se encontr� el �tem: {itemName}");
        return null;
    }

    /// <summary>
    /// Devuelve todos los �tems de un tipo espec�fico.
    /// �til para filtrar en la UI o la l�gica de crafteo.
    /// </summary>
    /// <param name="type">El TipoDeItem a buscar.</param>
    /// <returns>Una lista de ItemData que coinciden con el tipo.</returns>
    public List<ItemData> GetItemsByType(TipoDeItem type)
    {
        // NOTA: Usar LINQ aqu� est� bien ya que solo se ejecuta al inicializar o en momentos clave.
        return allItems.Where(item => item.tipoDeItem == type).ToList();
    }
}
