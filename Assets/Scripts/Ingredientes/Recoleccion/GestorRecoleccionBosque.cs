using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GestorRecoleccionBosque : MonoBehaviour
{
    // =========================================================================
    // ESTRUCTURAS DE CONFIGURACIÓN (Actualizadas a usar string)
    // =========================================================================

    [System.Serializable]
    public class ConfigSpawnIngrediente
    {
        [Tooltip("La clave o nombre del ingrediente (Debe ser el mismo que en PuntoSpawnRecoleccion y ItemCatalog).")]
        public string claveIngrediente; // <-- CORREGIDO: Usamos el string
        [Tooltip("Cuántos de ESTE ingrediente aparecerán como MÁXIMO cada día.")]
        public int maxPorDia;
        [Tooltip("Cuántos días deben pasar desde la recolección para que pueda volver a aparecer.")]
        public int diasCooldown = 1;
        [Range(0f, 1f)]
        [Tooltip("Probabilidad (0=0%, 1=100%) de que aparezca en un punto disponible.")]
        public float probabilidadSpawn = 1.0f;
    }

    [Header("Configuración de Spawn")]
    [Tooltip("Define aquí las reglas para cada tipo de ingrediente que quieras que aparezca en el bosque.")]
    public List<ConfigSpawnIngrediente> configuracionSpawns;

    [Header("Puntos de Spawn")]
    [Tooltip("Puedes dejarla vacía para que busque todos los puntos automáticamente al iniciar.")]
    public List<PuntoSpawnRecoleccion> todosLosPuntos;

    // =========================================================================
    // CICLO DE VIDA Y LÓGICA DE SPAWN
    // =========================================================================

    void Awake()
    {
        if (todosLosPuntos == null || todosLosPuntos.Count == 0)
        {
            todosLosPuntos = FindObjectsOfType<PuntoSpawnRecoleccion>().ToList();
            Debug.Log($"[GestorRecoleccion] Encontrados {todosLosPuntos.Count} Puntos de Spawn de Recolección.");
        }
    }

    void Start()
    {
        // En una aplicación real, este método debe llamarse desde GestorJuego.OnNewDayStarted
        // Pero para simplificar, lo dejamos en Start para la prueba inicial.
        GenerarIngredientesDelDia();
    }

    void GenerarIngredientesDelDia()
    {
        if (GestorJuego.Instance == null) { Debug.LogError("GestorRecoleccion: No se encontró GestorJuego."); return; }
        if (todosLosPuntos == null || todosLosPuntos.Count == 0) { Debug.LogWarning("GestorRecoleccion: No hay puntos de spawn definidos en la escena."); return; }

        int diaActual = GestorJuego.Instance.diaActual;
        Debug.Log($"--- [GestorRecoleccion] Iniciando generación para el Día {diaActual} ---");

        LimpiarObjetosInstanciados();

        // Agrupar los puntos por la clave (string) que definen en PuntoSpawnRecoleccion
        var puntosAgrupados = todosLosPuntos
                              // Solo procesar puntos que tienen una clave válida
                              .Where(p => p != null && !string.IsNullOrEmpty(p.claveIngredienteParaSpawnear))
                              .GroupBy(p => p.claveIngredienteParaSpawnear);

        foreach (var grupo in puntosAgrupados)
        {
            string claveIngrediente = grupo.Key;
            List<PuntoSpawnRecoleccion> puntosParaEsteTipo = grupo.ToList();

            // Buscar la configuración de spawn por la clave (string)
            ConfigSpawnIngrediente config = configuracionSpawns.FirstOrDefault(c => c.claveIngrediente == claveIngrediente);

            if (config == null)
            {
                Debug.LogWarning($"No hay configuración de spawn para la clave '{claveIngrediente}'. No aparecerá.");
                continue;
            }

            // OBTENER EL PREFAB USANDO EL GESTOR JUEGO Y EL CATÁLOGO
            GameObject prefab = GestorJuego.Instance.ObtenerPrefabRecolectable(claveIngrediente);

            if (prefab == null)
            {
                // El error ya se registró en GestorJuego (no encontrado/no ingrediente)
                continue;
            }

            // Filtrar puntos disponibles (no ocupados y cooldown cumplido)
            List<PuntoSpawnRecoleccion> puntosDisponibles = puntosParaEsteTipo
                .Where(p => p.objetoInstanciadoActual == null &&
                            diaActual >= p.diaUltimaRecoleccion + config.diasCooldown)
                .ToList();

            int maxASpawnearEsteTipo = Mathf.Min(config.maxPorDia, puntosDisponibles.Count);
            int spawneadosEsteTipo = 0;

            System.Random rng = new System.Random();
            puntosDisponibles = puntosDisponibles.OrderBy(p => rng.Next()).ToList(); // Aleatorizar orden

            foreach (PuntoSpawnRecoleccion punto in puntosDisponibles)
            {
                if (spawneadosEsteTipo >= maxASpawnearEsteTipo) break;

                if (Random.value <= config.probabilidadSpawn)
                {
                    Quaternion rotacion = punto.rotacionAleatoriaY ?
                                          Quaternion.Euler(0, Random.Range(0f, 360f), 0) * prefab.transform.rotation :
                                          prefab.transform.rotation;

                    GameObject instanciado = Instantiate(prefab, punto.transform.position, rotacion);

                    // 🌟🌟🌟 CORRECCIÓN CRÍTICA AÑADIDA 🌟🌟🌟
                    // Hacemos el objeto instanciado hijo del punto para mantener orden.
                    instanciado.transform.SetParent(punto.transform);

                    punto.objetoInstanciadoActual = instanciado;

                    IngredienteRecolectable recolectable = instanciado.GetComponent<IngredienteRecolectable>();
                    if (recolectable != null)
                    {
                        // 🚨 CLAVE FALTANTE: Asignar la clave al script IngredienteRecolectable 🚨
                        recolectable.claveIngrediente = claveIngrediente;

                        recolectable.puntoOrigen = punto;
                        // Opcional: Si IngredienteRecolectable necesita el ItemData, debe buscarlo aquí
                        // recolectable.datosIngrediente = GestorJuego.Instance.catalogoMaestro.GetItemData(claveIngrediente);
                    }
                    else
                    {
                        Debug.LogError($"[GestorRecoleccionBosque] El prefab '{prefab.name}' no tiene el script IngredienteRecolectable. ¡Asigna el script al prefab!");
                    }

                    spawneadosEsteTipo++;
                }
            }
            Debug.Log($"-> Spawneados {spawneadosEsteTipo} de '{claveIngrediente}' (Máx Diario: {config.maxPorDia}, Puntos Disponibles Hoy: {puntosDisponibles.Count})");
        }
        Debug.Log("--- [GestorRecoleccion] Generación de ingredientes terminada ---");
    }

    void LimpiarObjetosInstanciados()
    {
        int cont = 0;
        foreach (var punto in todosLosPuntos)
        {
            if (punto != null && punto.objetoInstanciadoActual != null)
            {
                // Solo destruimos si el objeto existe
                if (punto.objetoInstanciadoActual != null)
                {
                    Destroy(punto.objetoInstanciadoActual);
                    cont++;
                }
                punto.objetoInstanciadoActual = null;
            }
        }
        if (cont > 0) Debug.Log($"[GestorRecoleccion] Limpiados {cont} objetos.");
    }
}