using UnityEngine;

// Este script marca un lugar donde puede aparecer un ingrediente para recolectar.
public class PuntoSpawnRecoleccion : MonoBehaviour
{
    [Tooltip("La clave o nombre del ingrediente que PUEDE aparecer en este punto (debe coincidir con la clave del Gestor de Juego).")]
    public string claveIngredienteParaSpawnear; // <-- CORREGIDO: Ahora es un string, no un ScriptableObject

    [Tooltip("Marcar si quieres que el objeto instanciado rote aleatoriamente en el eje Y.")]
    public bool rotacionAleatoriaY = true; // Para variar la apariencia

    // --- Datos internos que usará el Gestor de Recolección ---
    // No necesitas modificarlos desde el Inspector
    [HideInInspector] public int diaUltimaRecoleccion = -1; // Día en que se recogió (-1 = listo para spawnear)
    [HideInInspector] public GameObject objetoInstanciadoActual = null; // Referencia al objeto que está aquí ahora
}