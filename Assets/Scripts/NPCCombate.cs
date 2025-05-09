using UnityEngine;

public class NPCCombate : MonoBehaviour
{
    // public GameObject panelTiendaVendedor; // Referencia a la UI de su tienda

    public void EmpezarCombate()
    {
        Debug.Log($"Empezando pelea con {gameObject.name}");
        // AQU� ir�a tu l�gica para activar el panel de UI de la tienda de este NPC
        // if(panelTiendaVendedor != null) panelTiendaVendedor.SetActive(true);
        // Bloquear movimiento jugador, etc.
        FindObjectOfType<InteraccionJugador>()?.MostrarNotificacion($"Golpe dado a {gameObject.name} .", 2f); // Ejemplo
    }
}