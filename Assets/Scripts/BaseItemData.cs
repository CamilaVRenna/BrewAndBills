using UnityEngine;

/// <summary>
/// CLASE BASE ABSTRACTA (ScriptableObject) para todos los elementos que el jugador
/// puede recoger, sostener y/o guardar en el inventario (ingredientes, frascos).
/// </summary>
public abstract class DatosItemBase : ScriptableObject
{
    [Header("Información General")]
    [Tooltip("Nombre único y legible del item.")]
    public string nombreItem = "Item Base";


    [Tooltip("Prefab del modelo 3D que se instanciará al sostener el item.")]
    public GameObject prefabModelo3D;

    [Header("Visualización 3D (En Mano)")]
    [Tooltip("Rotación adicional en Euler (X,Y,Z) para que el item se vea bien en la mano.")]
    public Vector3 rotacionEnMano = Vector3.zero;

    // Aquí pueden ir más propiedades comunes a TODOS los ítems (ej: peso, valor, etc.)
}
