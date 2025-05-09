using UnityEngine;

// Este script marca un lugar donde puede aparecer un ingrediente para recolectar.
public class PuntoSpawnRecoleccion : MonoBehaviour
{
    [Tooltip("Arrastra aqu� el ScriptableObject del ingrediente que PUEDE aparecer en este punto.")]
    public DatosIngrediente ingredienteParaSpawnear; // Define qu� tipo de ingrediente es este punto

    [Tooltip("Marcar si quieres que el objeto instanciado rote aleatoriamente en el eje Y.")]
    public bool rotacionAleatoriaY = true; // Para variar la apariencia

    // --- Datos internos que usar� el Gestor de Recolecci�n ---
    // No necesitas modificarlos desde el Inspector
    [HideInInspector] public int diaUltimaRecoleccion = -1; // D�a en que se recogi� (-1 = listo para spawnear)
    [HideInInspector] public GameObject objetoInstanciadoActual = null; // Referencia al objeto que est� aqu� ahora
}