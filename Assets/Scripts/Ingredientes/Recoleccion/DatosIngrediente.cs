using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NuevoIngrediente", menuName = "Alquimia/Datos Ingrediente")]
public class DatosIngrediente : ScriptableObject
{
    public string nombreIngrediente = "Nuevo Ingrediente";
    public Sprite icono;
    public GameObject prefabModelo3D;

    // --- A�ADIDO ---
    [Header("Ajustes Recolecci�n Bosque")]
    [Tooltip("Arrastra aqu� el PREFAB del objeto que se instanciar� en el bosque para ser recolectado (Debe tener el script IngredienteRecolectable).")]
    public GameObject prefabRecolectable; // <<--- NUEVO CAMPO
    // ---------------

    // --- A�ADIDO ---
    [Header("Ajustes Visuales en Mano")]
    [Tooltip("Rotaci�n adicional en Euler(X,Y,Z) para que se vea bien en la mano.")]
    public Vector3 rotacionEnMano = Vector3.zero; // <<--- NUEVO CAMPO
    // ---------------

    public Image imagenInventario;
}