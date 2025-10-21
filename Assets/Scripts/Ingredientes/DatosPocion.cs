using UnityEngine;

// Esto te permitirá crear instancias desde el menú de Unity: Create -> Inventario -> Pocion
[CreateAssetMenu(fileName = "NuevaPocion", menuName = "Inventario/Pocion")]
public class DatosPocion : ScriptableObject
{
    [Header("Configuración de la Poción")]
    public string nombreInterno; // Nombre que usará el Inventario (ej: "PocionFallida")
    public Sprite icono;
    // Puedes agregar aquí otras propiedades comunes a todas tus pociones
}