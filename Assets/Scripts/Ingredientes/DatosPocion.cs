using UnityEngine;

// Esto te permitir� crear instancias desde el men� de Unity: Create -> Inventario -> Pocion
[CreateAssetMenu(fileName = "NuevaPocion", menuName = "Inventario/Pocion")]
public class DatosPocion : ScriptableObject
{
    [Header("Configuraci�n de la Poci�n")]
    public string nombreInterno; // Nombre que usar� el Inventario (ej: "PocionFallida")
    public Sprite icono;
    // Puedes agregar aqu� otras propiedades comunes a todas tus pociones
}