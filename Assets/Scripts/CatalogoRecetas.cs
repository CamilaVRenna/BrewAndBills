using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CatalogoDeRecetas", menuName = "Pociones/Catalogo de Recetas")]
public class CatalogoRecetas : ScriptableObject
{
    [Tooltip("Arrastra aquí TODOS los assets de RecetaResultado (PedidoPocionData modificados) que definen pociones crafteables.")]
    public List<PedidoPocionData> todasLasRecetas; // Usamos PedidoPocionData aquí

    // ********************************************************************************
    // CRÍTICO: Este método ahora acepta una lista de NOMBRES (strings) desde Caldero.cs,
    // forzando la comparación por el nombre único del ingrediente.
    // ********************************************************************************
    /// <summary>
    /// Busca una receta que coincida exactamente con la lista de nombres de ingredientes proporcionada.
    /// La coincidencia ignora el orden y considera la cantidad de cada tipo de ingrediente.
    /// </summary>
    /// <param name="nombresIngredientes">La lista de nombres de ingredientes a buscar (lo que el jugador ha usado).</param>
    /// <returns>El PedidoPocionData de la receta coincidente, o null si no se encuentra ninguna.</returns>
    public PedidoPocionData BuscarRecetaPorNombres(List<string> nombresIngredientes)
    {
        if (todasLasRecetas == null || nombresIngredientes == null) return null;

        foreach (PedidoPocionData receta in todasLasRecetas)
        {
            // Convertir los ingredientes requeridos (DatosIngrediente SOs) de la receta a una lista de nombres.
            List<string> nombresRequeridos = receta.ingredientesRequeridos
                .Select(di => di.nombreIngrediente) // Asumimos que DatosIngrediente tiene la propiedad 'nombreIngrediente'
                .ToList();

            if (CompararListasDeNombres(nombresRequeridos, nombresIngredientes))
            {
                return receta; // ¡Receta Encontrada!
            }
        }
        return null; // No encontrada
    }

    /// <summary>
    /// Compara dos listas de nombres (strings) para verificar si contienen los mismos nombres 
    /// en la misma cantidad, ignorando el orden (comparación de multiconjunto usando LINQ).
    /// </summary>
    private bool CompararListasDeNombres(List<string> lista1, List<string> lista2)
    {
        // 1. Validación básica: si el número de ingredientes es diferente, no pueden coincidir.
        if (lista1 == null || lista2 == null || lista1.Count != lista2.Count) return false;

        // 2. Agrupar y contar la frecuencia de cada nombre en ambas listas.
        var conteo1 = lista1
            .GroupBy(name => name)
            .ToDictionary(g => g.Key, g => g.Count());

        var conteo2 = lista2
            .GroupBy(name => name)
            .ToDictionary(g => g.Key, g => g.Count());

        // 3. Comprobar que ambas listas tienen el mismo número de tipos únicos.
        if (conteo1.Count != conteo2.Count) return false;

        // 4. Comparar la cantidad de cada nombre único.
        foreach (var par in conteo1)
        {
            string nombre = par.Key;
            int cantidadRequerida = par.Value;

            if (!conteo2.TryGetValue(nombre, out int cantidadEncontrada) || cantidadRequerida != cantidadEncontrada)
            {
                // Falla si el ingrediente no está o si las cantidades no coinciden.
                return false;
            }
        }

        // Si todos los tipos y cantidades coinciden, las listas son iguales.
        return true;
    }
}
