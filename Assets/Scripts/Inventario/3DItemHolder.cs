using UnityEngine;
using System.Collections.Generic;

public class Item3DHolder : MonoBehaviour
{
    [Header("Configuraci�n del Holder")]
    [Tooltip("Punto de anclaje para el item sostenido (ej: frente a la c�mara)")]
    public Transform holdPoint;
    public InventoryManager inventoryManager; // Referencia al manager

    private GameObject currentHeldItem = null;

    private void Start()
    {
        if (inventoryManager == null)
        {
            // Intenta encontrar la instancia si no est� asignada en el Inspector
            inventoryManager = InventoryManager.Instance;
        }

        if (inventoryManager != null)
        {
            // Suscribirse al evento para manejar la visualizaci�n del item seleccionado
            inventoryManager.OnItemSelected += HandleItemSelected;

            // Llamada inicial para mostrar el �tem si ya hab�a uno seleccionado
            HandleItemSelected(inventoryManager.GetSelectedItem());
        }
        else
        {
            Debug.LogError("Item3DHolder no encontr� el InventoryManager.");
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
            // OBTENER LA DATA DEL �TEM:
            // *******************************************************************
            // ** CORRECCI�N CS0029: Usamos ItemCatalog.ItemData (el tipo correcto) **
            // *******************************************************************
            ItemCatalog.ItemData data = inventoryManager.GetCatalog().GetItemData(itemName);

            // 3. Instanciar y configurar el modelo 3D
            if (data != null && data.prefabModelo3D != null)
            {
                currentHeldItem = Instantiate(data.prefabModelo3D, holdPoint.position, holdPoint.rotation);
                currentHeldItem.transform.SetParent(holdPoint);

                // Aplicar la rotaci�n definida en la estructura de datos
                currentHeldItem.transform.localRotation = Quaternion.Euler(data.rotacionEnMano);

                // Opcional: Deshabilitar colisiones y f�sica para la visualizaci�n en mano
                Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);

                Collider[] colliders = currentHeldItem.GetComponents<Collider>();
                foreach (var col in colliders) col.enabled = false;
            }
            else if (data != null)
            {
                // Usamos 'nombreItem' de la estructura ItemCatalog.ItemData
                Debug.LogWarning($"El �tem '{itemName}' no tiene un prefab 3D asignado ({data.nombreItem}). Aseg�rate de configurarlo en el Item Catalog.");
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