using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para .ToList(), .GroupBy(), y .ToDictionary()

// Hacemos que sea un ScriptableObject, como lo tienes definido en el archivo
[CreateAssetMenu(fileName = "CatalogoDeRecetas", menuName = "Pociones/Catalogo de Recetas")]
public class CatalogoRecetas : ScriptableObject
{
    [Tooltip("Arrastra aquí TODOS los assets de RecetaResultado (PedidoPocionData modificados) que definen pociones crafteables.")]
    public List<PedidoPocionData> todasLasRecetas; // Usamos PedidoPocionData aquí

    // ********************************************************************************
    // Se eliminó la conversión de DatosIngrediente, ya que PedidoPocionData ahora usa List<string>.
    // ********************************************************************************
    /// <summary>
    /// Busca una receta que coincida exactamente con la lista de nombres de ingredientes proporcionada.
    /// La coincidencia ignora el orden y considera la cantidad de cada tipo de ingrediente (comparación de multiconjunto).
    /// </summary>
    /// <param name="nombresIngredientesCaldero">La lista de nombres de ingredientes usados en el caldero.</param>
    /// <returns>El PedidoPocionData de la receta coincidente, o null si no se encuentra ninguna.</returns>
    public PedidoPocionData BuscarRecetaPorNombres(List<string> nombresIngredientesCaldero)
    {
        if (todasLasRecetas == null || nombresIngredientesCaldero == null)
        {
            Debug.LogWarning("CatalogoRecetas o la lista de ingredientes del caldero es nula.");
            return null;
        }

        foreach (PedidoPocionData receta in todasLasRecetas)
        {
            // ASUMIMOS AHORA que receta.ingredientesRequeridos es List<string> (los nombres)
            List<string> nombresRequeridos = receta.ingredientesRequeridos;

            if (nombresRequeridos == null) continue;

            // 1. Comparar el multiconjunto (tipos y frecuencias) de ambas listas de nombres.
            if (CompararListasDeNombres(nombresRequeridos, nombresIngredientesCaldero))
            {
                Debug.Log($"¡Receta encontrada! Coincidencia con: {receta.nombreIdentificador}");
                return receta; // ¡Receta Encontrada!
            }
        }

        Debug.Log("No se encontró ninguna receta coincidente en el catálogo.");
        return null; // No encontrada
    }

    /// <summary>
    /// Compara dos listas de nombres (strings) para verificar si contienen los mismos nombres 
    /// en la misma cantidad, ignorando el orden (comparación de multiconjunto usando LINQ).
    /// </summary>
    private bool CompararListasDeNombres(List<string> listaRequerida, List<string> listaEncontrada)
    {
        // 1. Validación básica: si el número de ingredientes es diferente, no pueden coincidir.
        if (listaRequerida == null || listaEncontrada == null || listaRequerida.Count != listaEncontrada.Count)
        {
            Debug.Log($"Fallo en el conteo. Requerido: {listaRequerida?.Count ?? 0}, Encontrado: {listaEncontrada?.Count ?? 0}.");
            return false;
        }

        // 2. Agrupar y contar la frecuencia de cada nombre en ambas listas.
        var conteoRequerido = listaRequerida
            .GroupBy(name => name.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        var conteoEncontrado = listaEncontrada
            .GroupBy(name => name.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        // La comparación por defecto usa ToLowerInvariant para evitar problemas de mayúsculas/minúsculas.

        // 3. Comprobar que ambas listas tienen el mismo número de tipos únicos.
        if (conteoRequerido.Count != conteoEncontrado.Count)
        {
            Debug.Log($"Fallo en tipos únicos. Requeridos: {conteoRequerido.Count}, Encontrados: {conteoEncontrado.Count}.");
            return false;
        }

        // 4. Comparar la cantidad de cada nombre único.
        foreach (var par in conteoRequerido)
        {
            string nombreNormalizado = par.Key;
            int cantidadRequerida = par.Value;

            if (!conteoEncontrado.TryGetValue(nombreNormalizado, out int cantidadEncontrada) || cantidadRequerida != cantidadEncontrada)
            {
                Debug.Log($"Fallo en cantidad/existencia para el ingrediente: '{nombreNormalizado}'. Requerido: {cantidadRequerida}, Encontrado: {cantidadEncontrada}.");
                return false;
            }
        }

        // Si todos los tipos y cantidades coinciden, las listas son iguales.
        return true;
    }
}
