using UnityEngine;

public class ResolutionEnforcer : MonoBehaviour
{
    // 1920x1080 (Full HD, Proporción 16:9)
    public int desiredWidth = 1920;
    public int desiredHeight = 1080;

    void Awake()
    {
        // El 'false' al final fuerza el modo ventana. Si quieres pantalla completa, usa 'true'.
        Screen.SetResolution(desiredWidth, desiredHeight, false);
        Debug.Log("Resolución de juego forzada a 1920x1080 (Full HD).");
    }
}