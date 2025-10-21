using UnityEngine;

public class Recolectable : MonoBehaviour
{
    public string nombreObjeto;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            InventoryManager.Instance?.AddItemByName(nombreObjeto); // <-- Usa el nombre correcto
            Destroy(gameObject);
        }
    }
}
