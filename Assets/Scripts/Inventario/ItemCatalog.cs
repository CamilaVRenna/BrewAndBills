using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para la búsqueda por nombre.

/// <summary>
/// Contiene toda la información y modelos de todos los ítems del juego,
/// centralizando la gestión de datos en un solo ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "ItemCatalog", menuName = "Inventario/Item Catalog Centralizado")]
public class ItemCatalog : ScriptableObject
{
    // =========================================================================
    // ENUMERADOR DE TIPOS DE ÍTEMS (ACTUALIZADO)
    // =========================================================================

    /// <summary>
    /// Define el tipo fundamental del ítem, permitiendo lógica de juego específica 
    /// para crafteo, uso y equipamiento.
    /// </summary>
    public enum TipoDeItem
    {
        INGREDIENTE,         // Materiales base, recolectables, usados en recetas.
        INGREDIENTE_PROCESADO, // Material intermedio (ej: lingotes, harina). Para implementar lógica a futuro.
        FRASCO,              // Contenedor vacío o base líquida, generalmente usado en crafteo de pociones.
        POCION,              // Consumible bebible que aplica un efecto inmediato.
        HERRAMIENTA          // Objeto que se puede usar o equipar y que puede tener durabilidad (ej: hacha, pico).
    }

    // =========================================================================
    // ESTRUCTURA INTERNA DE DATOS
    // =========================================================================

    [System.Serializable]
    public class ItemData
    {
        [Header("Información General")]
        [Tooltip("El tipo fundamental del item (INGREDIENTE, POCION, HERRAMIENTA, etc.).")]
        public TipoDeItem tipoDeItem = TipoDeItem.INGREDIENTE; // <-- ¡NUEVO CAMPO CRUCIAL!

        [Tooltip("Nombre único y legible del item.")]
        public string nombreItem = "Item Base";

        [Tooltip("Prefab del modelo 3D que se instanciará al sostener el item.")]
        public GameObject prefabModelo3D;

        [Tooltip("Rotación adicional en Euler (X,Y,Z) para que el item se vea bien en la mano.")]
        public Vector3 rotacionEnMano = Vector3.zero;

        [Header("Específico de Recolección/Ingredientes")]
        [Tooltip("Si es un INGREDIENTE, arrastra aquí el PREFAB del objeto recolectable.")]
        public GameObject prefabRecolectable;
    }

    // =========================================================================
    // PROPIEDADES DEL CATÁLOGO
    // =========================================================================

    [Header("Lista Maestra de Ítems")]
    [Tooltip("La lista de todos los items ScriptableObject en el juego. Arrastra las estructuras aquí.")]
    [SerializeField]
    private List<ItemData> allItems = new List<ItemData>();

    // Diccionario para búsqueda rápida por nombre (se llena en Initialize).
    private Dictionary<string, ItemData> itemMap;

    // =========================================================================
    // MÉTODOS DE GESTIÓN
    // =========================================================================

    /// <summary>
    /// Inicializa el catálogo, llenando el diccionario de búsqueda. 
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
                    Debug.LogError($"[ItemCatalog] Error: Ítem duplicado encontrado: {item.nombreItem}");
                    continue;
                }
                itemMap[item.nombreItem] = item;
            }
        }
        Debug.Log($"[ItemCatalog] Inicialización completa. {itemMap.Count} ítems cargados.");
    }

    /// <summary>
    /// Devuelve la estructura de datos completa (ItemData) para el ítem dado su nombre.
    /// </summary>
    /// <param name="itemName">El nombre único del ítem.</param>
    /// <returns>La estructura ItemData, o null si no se encuentra.</returns>
    public ItemData GetItemData(string itemName)
    {
        if (itemMap == null) Initialize();

        if (string.IsNullOrEmpty(itemName)) return null;

        if (itemMap.TryGetValue(itemName, out ItemData itemData))
        {
            return itemData;
        }

        Debug.LogWarning($"[ItemCatalog] No se encontró el ítem: {itemName}");
        return null;
    }

    /// <summary>
    /// Devuelve todos los ítems de un tipo específico.
    /// Útil para filtrar en la UI o la lógica de crafteo.
    /// </summary>
    /// <param name="type">El TipoDeItem a buscar.</param>
    /// <returns>Una lista de ItemData que coinciden con el tipo.</returns>
    public List<ItemData> GetItemsByType(TipoDeItem type)
    {
        // NOTA: Usar LINQ aquí está bien ya que solo se ejecuta al inicializar o en momentos clave.
        return allItems.Where(item => item.tipoDeItem == type).ToList();
    }
}
