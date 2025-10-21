using UnityEngine;
using System.Collections.Generic;

public class Item3DHolder : MonoBehaviour
{
    [Header("Configuración del Holder")]
    [Tooltip("Punto de anclaje para el item sostenido (ej: frente a la cámara)")]
    public Transform holdPoint;
    public InventoryManager inventoryManager; // Referencia al manager

    private GameObject currentHeldItem = null;

    private void Start()
    {
        if (inventoryManager == null)
        {
            // Intenta encontrar la instancia si no está asignada en el Inspector
            inventoryManager = InventoryManager.Instance;
        }

        if (inventoryManager != null)
        {
            // Suscribirse al evento para manejar la visualización del item seleccionado
            inventoryManager.OnItemSelected += HandleItemSelected;

            // Llamada inicial para mostrar el ítem si ya había uno seleccionado
            HandleItemSelected(inventoryManager.GetSelectedItem());
        }
        else
        {
            Debug.LogError("Item3DHolder no encontró el InventoryManager.");
        }
    }

    // Se llama cuando el InventoryManager notifica un cambio de item seleccionado
    private void HandleItemSelected(string itemName)
    {
        // 1. Destruir item actual si existe
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
        }

        // 2. Si hay un item seleccionado, instanciar su prefab 3D
        if (!string.IsNullOrEmpty(itemName))
        {
            // OBTENER LA DATA DEL ÍTEM:
            // *******************************************************************
            // ** CORRECCIÓN CS0029: Usamos ItemCatalog.ItemData (el tipo correcto) **
            // *******************************************************************
            ItemCatalog.ItemData data = inventoryManager.GetCatalog().GetItemData(itemName);

            // 3. Instanciar y configurar el modelo 3D
            if (data != null && data.prefabModelo3D != null)
            {
                currentHeldItem = Instantiate(data.prefabModelo3D, holdPoint.position, holdPoint.rotation);
                currentHeldItem.transform.SetParent(holdPoint);

                // Aplicar la rotación definida en la estructura de datos
                currentHeldItem.transform.localRotation = Quaternion.Euler(data.rotacionEnMano);

                // Opcional: Deshabilitar colisiones y física para la visualización en mano
                Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);

                Collider[] colliders = currentHeldItem.GetComponents<Collider>();
                foreach (var col in colliders) col.enabled = false;
            }
            else if (data != null)
            {
                // Usamos 'nombreItem' de la estructura ItemCatalog.ItemData
                Debug.LogWarning($"El ítem '{itemName}' no tiene un prefab 3D asignado ({data.nombreItem}). Asegúrate de configurarlo en el Item Catalog.");
            }
        }
    }

    private void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnItemSelected -= HandleItemSelected;
        }
    }
}