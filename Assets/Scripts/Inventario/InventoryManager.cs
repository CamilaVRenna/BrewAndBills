using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// IMPORTANTE: Este script asume que las clases 'DatosIngrediente', 'DatosFrasco', 
// 'CatalogoRecetas', y 'ItemCatalog' existen en tu proyecto.
// Nota: Se ha removido la dependencia directa de las listas 'todosLosIngredientes' y 'todosLosFrascos'
// en favor de usar ItemCatalog, como lo requiere Item3DHolder.

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // 1. Evento para notificar cambios en la LISTA de ítems
    public event Action<List<ItemStack>> OnInventoryUpdated;
    // 2. Evento para notificar cambios en el ITEM SELECCIONADO (Envía el nombre del ítem, o null/vacío)
    public event Action<string> OnItemSelected;

    [System.Serializable]
    public class ItemStack
    {
        public string nombre;
        public int cantidad;
        public ItemStack(string nombre, int cantidad)
        {
            this.nombre = nombre;
            this.cantidad = cantidad;
        }
    }

    // Datos del inventario
    public List<ItemStack> items = new List<ItemStack>();
    private int selectedIndex = -1; // -1 = NADA seleccionado
    private bool inventarioAbierto = false;

    [Header("Catálogo de Datos y Configuración 3D")]
    public ItemCatalog itemCatalog;
    [Tooltip("Máximo de ítems a mostrar en la hotbar/mano 3D")]
    [SerializeField] private int maxHoldableItems = 3; // Límite de 3 ítems (slots 1, 2, 3)

    // Las referencias obsoletas se mantienen comentadas para evitar errores de compilación si las usas, 
    // pero ya no son necesarias para la lógica principal.
    /* public CatalogoRecetas catalogoRecetas;
    public List<DatosIngrediente> todosLosIngredientes;
    public List<DatosFrasco> todosLosFrascos;
    */

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        itemCatalog?.Initialize();

        selectedIndex = -1;
    }

    public ItemCatalog GetCatalog()
    {
        return itemCatalog;
    }

    // Método para notificar el cambio de item seleccionado (Dispara OnItemSelected)
    private void UpdateSelectedSlot()
    {
        string selectedItemName = GetSelectedItem();
        // Dispara el evento, notificando el nombre del ítem (o null/vacío si no hay selección)
        OnItemSelected?.Invoke(selectedItemName);
    }

    // Método de utilidad para disparar el evento de actualización de la lista
    private void NotifyInventoryChange()
    {
        // Esto notifica a cualquier UI que muestre la lista (aunque el usuario no quiera hotbar,
        // esto es útil para el debug o la gestión interna).
        OnInventoryUpdated?.Invoke(items);
        // Cuando la lista cambia, revalidamos la selección
        UpdateSelectedSlot();
    }

    // ====================================================================
    // MÉTODOS PÚBLICOS DE SELECCIÓN
    // ====================================================================

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    // Método principal para establecer o deseleccionar un índice de slot
    public void SetSelectedIndex(int index)
    {
        // Solo consideramos los slots hasta maxHoldableItems (3)
        int availableSlots = Mathf.Min(items.Count, maxHoldableItems);

        // Si el índice está fuera del rango de slots visibles, o si es un intento de deseleccionar.
        if (index < 0 || index >= availableSlots)
        {
            index = -1; // Deseleccionar
        }

        if (selectedIndex != index)
        {
            selectedIndex = index;
            // IMPORTANTE: Esto dispara OnItemSelected para que Item3DHolder actualice la mano
            UpdateSelectedSlot();
        }
    }


    public string GetSelectedItem()
    {
        // Solo consideramos los primeros 'maxHoldableItems' (3)
        if (items.Count == 0 || selectedIndex < 0 || selectedIndex >= Mathf.Min(items.Count, maxHoldableItems))
            return null;

        return items[selectedIndex].nombre;
    }

    // ====================================================================
    // MÉTODOS DE MANIPULACIÓN DE INVENTARIO
    // ====================================================================

    public void AddItem(string item)
    {
        bool wasEmpty = items.Count == 0;

        var stack = items.Find(i => i.nombre == item);
        if (stack != null)
        {
            stack.cantidad++;
        }
        else
        {
            // Solo se pueden añadir items si hay slots disponibles en la hotbar (máximo 3 tipos de ítems)
            if (items.Count < maxHoldableItems)
            {
                items.Add(new ItemStack(item, 1));
            }
            else
            {
                Debug.LogWarning($"[Inventario] El inventario está lleno. No se pudo añadir: {item}.");
                return;
            }
        }

        Debug.Log("[Inventario] Agregado: " + item);

        NotifyInventoryChange();

        // Auto-seleccionar solo si es el PRIMER ítem recogido
        if (wasEmpty && items.Count == 1)
        {
            SetSelectedIndex(0);
        }
    }

    public void AddItemByName(string item)
    {
        AddItem(item);
    }

    // Remueve X cantidad de item
    public void RemoveItem(string item, int cantidad = 1)
    {
        var stack = items.Find(i => i.nombre == item);

        if (stack != null)
        {
            int quitar = Mathf.Min(stack.cantidad, cantidad);
            stack.cantidad -= quitar;

            bool itemRemoved = false;
            if (stack.cantidad <= 0)
            {
                items.Remove(stack);
                itemRemoved = true;
            }

            Debug.Log($"[Inventario] Eliminado: {item} x{quitar}");

            // Reajusta el índice de selección si el item seleccionado fue removido o el inventario se encogió
            if (selectedIndex >= items.Count || (itemRemoved && items.Count == 0))
                SetSelectedIndex(-1);
            else if (itemRemoved && selectedIndex > 0)
            {
                // Si el item seleccionado se eliminó y había más items, intenta seleccionar el slot anterior.
                SetSelectedIndex(Mathf.Min(selectedIndex, items.Count - 1));
            }
            else if (itemRemoved)
            {
                // Si el item 0 fue eliminado y hay otro item en el slot 0 (porque los demás se movieron)
                SetSelectedIndex(0);
            }

            NotifyInventoryChange();
        }
    }

    public bool HasItem(string item)
    {
        return items.Exists(i => i.nombre == item && i.cantidad > 0);
    }

    public int ContarItem(string item)
    {
        var stack = items.Find(i => i.nombre == item);
        return stack != null ? stack.cantidad : 0;
    }

    // ====================================================================
    // UPDATE: INPUT Y CONTROL DE SELECCIÓN
    // ====================================================================

    private void Update()
    {
        // Omitir lógica de input si el inventario de debug está abierto
        if (inventarioAbierto) return;

        // 1. Lógica de selección con Rueda del Ratón
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        int availableSlots = Mathf.Min(items.Count, maxHoldableItems);

        if (scroll != 0f && availableSlots > 0)
        {
            int newIndex = selectedIndex;

            if (newIndex == -1)
            {
                // Si no hay nada seleccionado, empezamos en el primer slot (0)
                newIndex = 0;
            }

            if (scroll > 0f) // Mover hacia adelante (scroll up)
            {
                newIndex = (newIndex + 1) % availableSlots;
            }
            else if (scroll < 0f) // Mover hacia atrás (scroll down)
            {
                newIndex = (newIndex - 1 + availableSlots) % availableSlots;
            }

            SetSelectedIndex(newIndex);
        }

        // 2. Lógica de selección por número (Limitada a maxHoldableItems)
        for (int i = 0; i < maxHoldableItems; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                int slotIndex = i;

                if (slotIndex < items.Count)
                {
                    // Alternar: Si presionamos el número del slot actual, deseleccionamos (-1)
                    if (selectedIndex == slotIndex)
                    {
                        SetSelectedIndex(-1);
                    }
                    else
                    {
                        SetSelectedIndex(slotIndex);
                    }
                }
                else
                {
                    // Si presionamos un número de slot vacío o fuera del rango (ej. presionas '3' pero solo tienes 1 ítem), deseleccionamos.
                    SetSelectedIndex(-1);
                }
                break;
            }
        }

        // -----------------------------------------------------------
        // KeyCode.T para deseleccionar (Opción alternativa)
        // -----------------------------------------------------------
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (selectedIndex != -1)
            {
                Debug.Log($"[Inventario] Deseleccionaste el item actual.");
                SetSelectedIndex(-1);
            }
            else
            {
                Debug.Log("[Inventario] No hay item seleccionado para deseleccionar.");
            }
        }

        // La lógica de KeyCode.I para debug se deja igual
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventarioAbierto = !inventarioAbierto;
            if (inventarioAbierto)
            {
                string lista = "T para deseleccionar item\n";
                for (int i = 0; i < items.Count; i++)
                {
                    string itemStr = $"{i + 1}. {items[i].nombre} x{items[i].cantidad}";
                    // Solo muestra el indicador de selección si está dentro de los slots 3D
                    if (i == selectedIndex && i < maxHoldableItems) itemStr += " *";
                    lista += itemStr + "\n";
                }
                Debug.Log($"[Inventario Abierto]\n{lista}");
            }
            else
            {
                Debug.Log("[Inventario Cerrado]");
            }
        }
    }

    // Se elimina GetDatosIngrediente para usar ItemCatalog
}
