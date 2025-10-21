using UnityEngine;

/// <summary>
/// CLASE BASE ABSTRACTA (ScriptableObject) para todos los elementos que el jugador
/// puede recoger, sostener y/o guardar en el inventario (ingredientes, frascos).
/// </summary>
public abstract class DatosItemBase : ScriptableObject
{
    [Header("Informaci�n General")]
    [Tooltip("Nombre �nico y legible del item.")]
    public string nombreItem = "Item Base";


    [Tooltip("Prefab del modelo 3D que se instanciar� al sostener el item.")]
    public GameObject prefabModelo3D;

    [Header("Visualizaci�n 3D (En Mano)")]
    [Tooltip("Rotaci�n adicional en Euler (X,Y,Z) para que el item se vea bien en la mano.")]
    public Vector3 rotacionEnMano = Vector3.zero;

    // Aqu� pueden ir m�s propiedades comunes a TODOS los �tems (ej: peso, valor, etc.)
}
