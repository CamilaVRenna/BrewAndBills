using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    // ...existing code...

    public void ResumeGame()
    {
        // Llama al método de ControladorPausa para reanudar correctamente
        ControladorPausa controlador = FindObjectOfType<ControladorPausa>();
        if (controlador != null)
        {
            controlador.ReanudarJuego();
        }
        else
        {
            Debug.LogError("No se encontró ControladorPausa en la escena.");
        }
    }

    // ...existing code...
}