using UnityEngine;

public class ObjetoRecolectable : MonoBehaviour
{
    public InventoryManager inventarioJugador;

    void Start()
    {
        if (inventarioJugador == null)
        {
            inventarioJugador = FindObjectOfType<InventoryManager>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (inventarioJugador != null)
            {
                // Aquí deberías agregar el objeto al inventario usando AddItem o AddItemVisual
                // Por ejemplo, si tienes un nombre de ítem:
                // inventarioJugador.AddItem("nombreDelItem");
                // inventarioJugador.AddItemVisual(iconoDelItem);

                // Por ahora, simplemente desactiva el objeto al recogerlo
                gameObject.SetActive(false); // O Destroy(gameObject);
            }
        }
    }
}
