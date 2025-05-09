using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para LINQ (GroupBy, OrderBy, etc.)

public class GestorRecoleccionBosque : MonoBehaviour
{
    // --- Clase interna para definir reglas por ingrediente ---
    [System.Serializable] // Para que se vea en el Inspector
    public class ConfigSpawnIngrediente
    {
        [Tooltip("El tipo de ingrediente para esta regla.")]
        public DatosIngrediente ingrediente;
        [Tooltip("Cu�ntos de ESTE ingrediente aparecer�n como M�XIMO cada d�a.")]
        public int maxPorDia;
        [Tooltip("Cu�ntos d�as deben pasar desde la recolecci�n para que pueda volver a aparecer (1=al d�a sig., 2=esperar 1 d�a).")]
        public int diasCooldown = 1;
        [Range(0f, 1f)] // Slider de 0 a 1
        [Tooltip("Probabilidad (0=0%, 1=100%) de que aparezca en un punto disponible.")]
        public float probabilidadSpawn = 1.0f;
    }
    // --- Fin clase interna ---

    [Header("Configuraci�n de Spawn")]
    [Tooltip("Define aqu� las reglas para cada tipo de ingrediente que quieras que aparezca en el bosque.")]
    public List<ConfigSpawnIngrediente> configuracionSpawns; // Configura esto en el Inspector

    [Header("Puntos de Spawn (Opcional)")]
    [Tooltip("Puedes dejarla vac�a para que busque todos los puntos autom�ticamente al iniciar, o arrastrarlos manualmente.")]
    public List<PuntoSpawnRecoleccion> todosLosPuntos;

    // Awake se llama una vez cuando el objeto se crea/activa
    void Awake()
    {
        // Buscar todos los puntos de spawn en la escena si no se asignaron manualmente
        if (todosLosPuntos == null || todosLosPuntos.Count == 0)
        {
            todosLosPuntos = FindObjectsOfType<PuntoSpawnRecoleccion>().ToList();
            Debug.Log($"[GestorRecoleccion] Encontrados {todosLosPuntos.Count} Puntos de Spawn de Recolecci�n.");
        }
        else { Debug.Log($"[GestorRecoleccion] Usando {todosLosPuntos.Count} Puntos de Spawn asignados manualmente."); }
    }

    // Start se llama despu�s de Awake
    void Start()
    {
        // Generar los ingredientes correspondientes al estado actual del juego
        GenerarIngredientesDelDia();
    }

    // M�todo principal que decide qu� y d�nde spawnear
    void GenerarIngredientesDelDia()
    {
        if (GestorJuego.Instance == null) { Debug.LogError("GestorRecoleccion: No se encontr� GestorJuego."); return; }
        if (todosLosPuntos == null || todosLosPuntos.Count == 0) { Debug.LogWarning("GestorRecoleccion: No hay puntos de spawn definidos en la escena."); return; }

        if (GestorJuego.Instance.horaActual != HoraDelDia.Tarde) 
        {
             Debug.Log($"[GestorRecoleccion] No es de Tarde ({GestorJuego.Instance.horaActual}), no se generan ingredientes.");
             LimpiarObjetosInstanciados(); // Limpiar por si acaso
             return;
        }

        int diaActual = GestorJuego.Instance.diaActual;
        Debug.Log($"--- [GestorRecoleccion] Iniciando generaci�n para el D�a {diaActual} ---");

        // 1. Limpiar cualquier objeto que pudiera haber quedado del d�a anterior
        LimpiarObjetosInstanciados();

        // 2. Agrupar puntos por tipo de ingrediente
        var puntosAgrupados = todosLosPuntos
                              .Where(p => p != null && p.ingredienteParaSpawnear != null)
                              .GroupBy(p => p.ingredienteParaSpawnear);

        // 3. Iterar por cada tipo de ingrediente encontrado en los puntos
        foreach (var grupo in puntosAgrupados)
        {
            DatosIngrediente tipoIngrediente = grupo.Key;
            List<PuntoSpawnRecoleccion> puntosParaEsteTipo = grupo.ToList();

            // Buscar la configuraci�n espec�fica para este ingrediente
            ConfigSpawnIngrediente config = configuracionSpawns.FirstOrDefault(c => c.ingrediente == tipoIngrediente);

            if (config == null)
            {
                Debug.LogWarning($"No hay configuraci�n de spawn para '{tipoIngrediente.nombreIngrediente}'. No aparecer�.");
                continue; // Pasar al siguiente tipo
            }

            // Filtrar los puntos que est�n listos para reaparecer (cooldown cumplido Y sin objeto actual)
            List<PuntoSpawnRecoleccion> puntosDisponibles = puntosParaEsteTipo
                .Where(p => p.objetoInstanciadoActual == null &&
                            diaActual >= p.diaUltimaRecoleccion + config.diasCooldown)
                .ToList();

            // Debug.Log($"Ingrediente: {tipoIngrediente.nombreIngrediente} - Puntos Totales: {puntosParaEsteTipo.Count}, Puntos Disponibles Hoy: {puntosDisponibles.Count}");

            // Calcular cu�ntos vamos a intentar spawnear hoy
            int maxASpawnearEsteTipo = Mathf.Min(config.maxPorDia, puntosDisponibles.Count);
            int spawneadosEsteTipo = 0;

            // Mezclar los puntos disponibles para que la aparici�n sea aleatoria entre ellos
            System.Random rng = new System.Random();
            puntosDisponibles = puntosDisponibles.OrderBy(p => rng.Next()).ToList();

            // Intentar spawnear en los puntos disponibles
            foreach (PuntoSpawnRecoleccion punto in puntosDisponibles)
            {
                if (spawneadosEsteTipo >= maxASpawnearEsteTipo) break; // Ya llegamos al m�ximo por d�a para este tipo

                // Comprobar probabilidad
                if (Random.value <= config.probabilidadSpawn)
                {
                    GameObject prefab = tipoIngrediente.prefabRecolectable;
                    if (prefab != null)
                    {
                        Quaternion rotacion = punto.rotacionAleatoriaY ?
                                            Quaternion.Euler(0, Random.Range(0f, 360f), 0) * prefab.transform.rotation : // Rota Y + base prefab
                                            prefab.transform.rotation; // Usa rotaci�n base prefab

                        GameObject instanciado = Instantiate(prefab, punto.transform.position, rotacion);
                        // Hacerlo hijo del punto ayuda a organizar la jerarqu�a (opcional)
                        // instanciado.transform.SetParent(punto.transform);

                        // Guardar referencias importantes
                        punto.objetoInstanciadoActual = instanciado;
                        IngredienteRecolectable recolectable = instanciado.GetComponent<IngredienteRecolectable>();
                        if (recolectable != null) { recolectable.puntoOrigen = punto; }

                        spawneadosEsteTipo++;
                    }
                    else { Debug.LogWarning($"Ingrediente '{tipoIngrediente.name}' no tiene 'Prefab Recolectable' asignado."); }
                }
                // else -> Fall� chequeo de probabilidad
            }
            Debug.Log($"-> Spawneados {spawneadosEsteTipo} de '{tipoIngrediente.nombreIngrediente}' (M�x Diario: {config.maxPorDia}, Puntos Disponibles Hoy: {puntosDisponibles.Count})");
        }
        Debug.Log("--- [GestorRecoleccion] Generaci�n de ingredientes terminada ---");
    }

    // Limpia (destruye) cualquier objeto de ingrediente que est� actualmente instanciado en los puntos
    void LimpiarObjetosInstanciados()
    {
        // Debug.Log("[GestorRecoleccion] Limpiando objetos instanciados del d�a anterior..."); // Log opcional
        int cont = 0;
        foreach (var punto in todosLosPuntos)
        {
            if (punto != null && punto.objetoInstanciadoActual != null)
            {
                Destroy(punto.objetoInstanciadoActual);
                punto.objetoInstanciadoActual = null;
                cont++;
            }
        }
        if (cont > 0) Debug.Log($"[GestorRecoleccion] Limpiados {cont} objetos.");
    }
}