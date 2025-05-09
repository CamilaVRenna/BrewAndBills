using UnityEngine;
public class NPCDialogo : MonoBehaviour
{
    public void IniciarDialogo()
    {
        Debug.Log($"Iniciando di�logo con {gameObject.name}");
        // AQU� ir�a tu l�gica para mostrar la ventana de di�logo
        // Puedes usar un sistema de UI, mostrar notificaciones, etc.
        // Por ahora, solo un mensaje en consola.
        FindObjectOfType<InteraccionJugador>()?.MostrarNotificacion($"{gameObject.name}: Hola, viajero.", 3f); // Ejemplo
    }
}