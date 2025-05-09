using UnityEngine;
public class NPCQuestGiver : MonoBehaviour
{
    // public GameObject panelTiendaVendedor; // Referencia a la UI de su tienda

    public void OfrecerOActualizarQuest()
    {
        Debug.Log($"Obteniendo mision de {gameObject.name}");
        // AQU� ir�a tu l�gica para activar el panel de UI de la tienda de este NPC
        // if(panelTiendaVendedor != null) panelTiendaVendedor.SetActive(true);
        // Bloquear movimiento jugador, etc.
        FindObjectOfType<InteraccionJugador>()?.MostrarNotificacion($"Mision de {gameObject.name} aceptada.", 2f); // Ejemplo
    }
}