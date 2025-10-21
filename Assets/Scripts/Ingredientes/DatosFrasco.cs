using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Nuevo Frasco", menuName = "Alquimia/Datos Frasco")]
public class DatosFrasco : ScriptableObject
{
    [Header("Informaci�n B�sica")]
    public string nombreItem = "Frasco Vac�o";
    public Sprite icono;

    [Header("Visualizaci�n 3D (En Mano)")]
    public GameObject prefabModelo3D;
    public Material materialVacio;
    public Material materialLleno;

    // --- A�ADIDO ---
    [Header("Ajustes Visuales en Mano")]
    [Tooltip("Rotaci�n adicional en Euler(X,Y,Z) para que se vea bien en la mano.")]
    public Vector3 rotacionEnMano = Vector3.zero; // <<--- NUEVO CAMPO
    // ---------------

    public Image imagenInventario;
}