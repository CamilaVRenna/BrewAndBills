using UnityEngine;
using System.Collections.Generic;

// Cambiamos el nombre del menú para que quede más claro que define una receta/resultado
[CreateAssetMenu(fileName = "NuevaRecetaResultado", menuName = "Pociones/Receta y Resultado")]
public class PedidoPocionData : ScriptableObject // Mantenemos el nombre de la clase por compatibilidad
{
    [Header("Identificación y Pedido NPC")]
    [Tooltip("Nombre interno para identificar esta receta/pedido (ej: 'CurativaSimple').")]
    public string nombreIdentificador = "RecetaGenerica";

    [Tooltip("Nombres EXACTOS de los ingredientes que se requieren para esta receta (ej: 'FlorRoja', 'RaizGris').")]
    // ¡CORRECCIÓN CLAVE! Usa List<string> para ItemCatalog
    public List<string> ingredientesRequeridos;

    [Tooltip("Frases genéricas que un NPC puede usar para pedir esta poción. Elige una al azar.")]
    public List<string> dialogosPedidoGenericos;
    [Tooltip("Nombre corto o palabra clave para referencia interna (ej: 'Curación', 'Fuerza'). Opcional.")]
    public string clavePocion;

    [Header("Resultado y Detalles de la Receta")]
    [Tooltip("Nombre que se mostrará en la UI cuando se cree esta poción.")]
    public string nombreResultadoPocion = "Poción Desconocida";
    [Tooltip("Material que se aplicará al frasco y al caldero al crear esta poción.")]
    public Material materialResultado;
    [Tooltip("Imagen que se mostrará en el libro de recetas (página izquierda).")]
    public Sprite imagenPocion;
    [TextArea(5, 10)]
    [Tooltip("Descripción, historia o instrucciones de la poción (página derecha).")]
    public string descripcionPocion = "Nadie sabe exactamente qué hace...";
}
