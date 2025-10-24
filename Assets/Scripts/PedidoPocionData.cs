using UnityEngine;
using System.Collections.Generic;

// Cambiamos el nombre del men� para que quede m�s claro que define una receta/resultado
[CreateAssetMenu(fileName = "NuevaRecetaResultado", menuName = "Pociones/Receta y Resultado")]
public class PedidoPocionData : ScriptableObject // Mantenemos el nombre de la clase por compatibilidad
{
    [Header("Identificaci�n y Pedido NPC")]
    [Tooltip("Nombre interno para identificar esta receta/pedido (ej: 'CurativaSimple').")]
    public string nombreIdentificador = "RecetaGenerica";

    [Tooltip("Nombres EXACTOS de los ingredientes que se requieren para esta receta (ej: 'FlorRoja', 'RaizGris').")]
    // �CORRECCI�N CLAVE! Usa List<string> para ItemCatalog
    public List<string> ingredientesRequeridos;

    [Tooltip("Frases gen�ricas que un NPC puede usar para pedir esta poci�n. Elige una al azar.")]
    public List<string> dialogosPedidoGenericos;
    [Tooltip("Nombre corto o palabra clave para referencia interna (ej: 'Curaci�n', 'Fuerza'). Opcional.")]
    public string clavePocion;

    [Header("Resultado y Detalles de la Receta")]
    [Tooltip("Nombre que se mostrar� en la UI cuando se cree esta poci�n.")]
    public string nombreResultadoPocion = "Poci�n Desconocida";
    [Tooltip("Material que se aplicar� al frasco y al caldero al crear esta poci�n.")]
    public Material materialResultado;
    [Tooltip("Imagen que se mostrar� en el libro de recetas (p�gina izquierda).")]
    public Sprite imagenPocion;
    [TextArea(5, 10)]
    [Tooltip("Descripci�n, historia o instrucciones de la poci�n (p�gina derecha).")]
    public string descripcionPocion = "Nadie sabe exactamente qu� hace...";
}
