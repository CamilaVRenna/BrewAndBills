using UnityEngine;
using System.Collections.Generic;

// Cambiamos el nombre del men� para que quede m�s claro que define una receta/resultado
[CreateAssetMenu(fileName = "NuevaRecetaResultado", menuName = "Pociones/Receta y Resultado")]
public class PedidoPocionData : ScriptableObject // Mantenemos el nombre de la clase por compatibilidad
{
    [Header("Identificaci�n y Pedido NPC")]
    [Tooltip("Nombre interno para identificar esta receta/pedido (ej: 'CurativaSimple').")]
    public string nombreIdentificador = "RecetaGenerica"; // Antes era nombrePedido
    [Tooltip("Ingredientes EXACTOS que se requieren para esta receta (y que el NPC podr�a pedir).")]
    public List<DatosIngrediente> ingredientesRequeridos;
    /*[Tooltip("Texto opcional que podr�a decir el NPC al pedir esto.")]
    public string dialogoPedido = "�Podr�as prepararme una poci�n con...?";*/
    [Tooltip("Frases gen�ricas que un NPC puede usar para pedir esta poci�n. Elige una al azar.")]
    public List<string> dialogosPedidoGenericos; // <<--- NUEVA LISTA
    [Tooltip("Nombre corto o palabra clave para referencia interna (ej: 'Curaci�n', 'Fuerza'). Opcional.")]
    public string clavePocion; // <<--- NUEVO OPCIONAL

    [Header("Resultado y Detalles de la Receta")] // <<--- NUEVA SECCI�N ---
    [Tooltip("Nombre que se mostrar� en la UI cuando se cree esta poci�n.")]
    public string nombreResultadoPocion = "Poci�n Desconocida"; // <<--- NUEVO
    [Tooltip("Material que se aplicar� al frasco y al caldero al crear esta poci�n.")]
    public Material materialResultado; // <<--- NUEVO
    [Tooltip("Imagen que se mostrar� en el libro de recetas (p�gina izquierda).")]
    public Sprite imagenPocion; // <<--- NUEVO
    [TextArea(5, 10)] // Para que el campo sea m�s grande en el Inspector
    [Tooltip("Descripci�n, historia o instrucciones de la poci�n (p�gina derecha).")]
    public string descripcionPocion = "Nadie sabe exactamente qu� hace..."; // <<--- NUEVO
}