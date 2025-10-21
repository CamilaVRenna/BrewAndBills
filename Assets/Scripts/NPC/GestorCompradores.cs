using UnityEngine;
using System.Collections.Generic;
using System.Collections;
// Asegúrate de que este archivo tenga todos los 'using' necesarios para tus otros tipos de datos
// (ej: HoraDelDia, GestorJuego, CatalogoRecetas, PedidoPocionData, GestorUI)

public class GestorCompradores : MonoBehaviour
{
    [Header("Configuración General")]
    public Transform puntoAparicion;
    public Transform posicionVentana;
    public Transform puntoMiradaVentana;
    public float intervaloAparicion = 10.0f;
    public Transform puntoSalidaNPC; // CRÍTICO: Asignar en el Inspector

    [Tooltip("Arrastra aquí TODOS los prefabs de NPC diferentes que pueden aparecer.")]
    public List<GameObject> prefabsNPCsPosibles;

    [Tooltip("Número máximo de NPCs que pueden estar en la cola + en la ventanilla al mismo tiempo.")]
    public int maximoNPCsActivos = 5;

    [Header("Catálogo de Recetas")]
    public CatalogoRecetas catalogoRecetas;

    [Header("Pedidos y Sonidos")]
    public List<PedidoPocionData> listaMaestraPedidos;
    public AudioClip sonidoNuevoPedido;

    // --- REFERENCIAS DE UI y Audio ---
    [Header("Referencias de UI y Audio")]
    public GestorUI uiGestor;
    public AudioSource audioSource;
    // ---------------------------------

    private Queue<NPCComprador> colaNPCs = new Queue<NPCComprador>();
    private NPCComprador npcActualEnVentana = null;
    private float temporizador = 0f;

    [HideInInspector]
    public bool tiendaAbierta = false;

    [HideInInspector]
    public bool compradoresHabilitados = false;


    void Update()
    {
        if (!tiendaAbierta || !compradoresHabilitados) return;

        temporizador += Time.deltaTime;
        if (temporizador >= intervaloAparicion && PuedeGenerarMasNPCs())
        {
            temporizador = 0f;
            GenerarNPC();
        }

        if (npcActualEnVentana == null && colaNPCs.Count > 0)
        {
            AsignarSiguienteNPC();
        }
    }

    private bool PuedeGenerarMasNPCs()
    {
        int totalNPCsActivos = colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0);
        bool limiteConcurrenteOk = totalNPCsActivos < maximoNPCsActivos;

        bool limiteDiarioOk = false;
        // bool esDeNoche = false; // Se omite, se asume que 'compradoresHabilitados' ya maneja esto.

        if (GestorJuego.Instance != null)
        {
            // NOTA: Asumiendo que GestorJuego y HoraDelDia existen.
            limiteDiarioOk = GestorJuego.Instance.ObtenerNPCsGeneradosHoy() < GestorJuego.Instance.limiteNPCsPorDia;
        }
        else
        {
            Debug.LogError("GestorJuego no encontrado para verificar el límite diario!");
            return false;
        }

        bool puedeGenerar = limiteConcurrenteOk && limiteDiarioOk;
        return puedeGenerar;
    }

    void GenerarNPC()
    {
        if (prefabsNPCsPosibles == null || prefabsNPCsPosibles.Count == 0)
        {
            Debug.LogError("¡La lista 'prefabsNPCsPosibles' está vacía o no asignada en GestorCompradores! No se pueden generar NPCs.");
            return;
        }
        if (puntoAparicion == null)
        {
            Debug.LogError("¡Falta asignar Punto Aparicion en GestorCompradores!");
            return;
        }

        int indicePrefab = Random.Range(0, prefabsNPCsPosibles.Count);
        GameObject prefabAUsar = prefabsNPCsPosibles[indicePrefab];

        if (prefabAUsar == null)
        {
            Debug.LogError($"El elemento {indicePrefab} en la lista 'prefabsNPCsPosibles' está vacío (None).");
            return;
        }

        GameObject objetoNPC = Instantiate(prefabAUsar, puntoAparicion.position, puntoAparicion.rotation);

        NPCComprador controladorNPC = objetoNPC.GetComponent<NPCComprador>();

        if (controladorNPC != null)
        {
            controladorNPC.gestor = this;
            colaNPCs.Enqueue(controladorNPC);

            if (GestorJuego.Instance != null)
            {
                GestorJuego.Instance.RegistrarNPCGeneradoHoy();
            }
            else { Debug.LogWarning("GenerarNPC: No se encontró GestorJuego para registrar NPC diario."); }

            Debug.Log($"NPC {objetoNPC.name} (Tipo: {prefabAUsar.name}) generado y añadido a la cola. (Total en cola: {colaNPCs.Count}, Total activos: {colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0)})");
        }
        else
        {
            Debug.LogError($"¡El Prefab '{prefabAUsar.name}' no tiene el script 'NPCComprador'!");
            Destroy(objetoNPC);
        }
    }

    void AsignarSiguienteNPC()
    {
        if (npcActualEnVentana != null) return;
        npcActualEnVentana = colaNPCs.Dequeue();
        Debug.Log($"Asignando a {npcActualEnVentana.gameObject.name} a la ventana. ({colaNPCs.Count} restantes en cola)");
        npcActualEnVentana.gameObject.SetActive(true);

        // --- CORRECCIÓN DEL ERROR CS1501 ---
        // Se llama a IrAVentana con 2 argumentos, lo cual ahora es compatible con la definición en NPCComprador.cs
        npcActualEnVentana.IrAVentana(posicionVentana.position, puntoSalidaNPC.position);
    }

    public void NPCTermino(NPCComprador npcQueTermino)
    {
        if (npcQueTermino == npcActualEnVentana)
        {
            Debug.Log($"{npcQueTermino.gameObject.name} ha terminado en la ventana. Liberando puesto.");
            npcActualEnVentana = null;
        }
        else
        {
            Debug.LogWarning($"Un NPC ({npcQueTermino?.gameObject.name}) que NO estaba en la ventana intentó notificar término.");
            if (npcActualEnVentana == npcQueTermino) { npcActualEnVentana = null; }
        }
    }

    public NPCComprador ObtenerNPCActual()
    {
        return npcActualEnVentana;
    }

    public void ReiniciarParaNuevoDia()
    {
        Debug.Log("GestorCompradores: Reiniciando para nuevo día...");

        if (npcActualEnVentana != null)
        {
            // El NPC se va y se autodestruye al llegar a su punto de salida.
            npcActualEnVentana.Irse();
            npcActualEnVentana = null;
        }

        Debug.Log($"Limpiando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue();
            if (npcEnCola != null)
            {
                Debug.Log($"- Despachando NPC en cola: {npcEnCola.gameObject.name}");
                // El NPC se va y se autodestruye al llegar a su punto de salida.
                npcEnCola.Irse();
            }
        }
        colaNPCs.Clear();

        temporizador = 0f;
        Debug.Log("GestorCompradores: Reinicio completado. Temporizador a 0.");
    }

    public void DespawnTodosNPCsPorNoche()
    {
        Debug.LogWarning("GestorCompradores: Se hizo de noche. Despachando a todos los NPCs...");

        if (npcActualEnVentana != null)
        {
            npcActualEnVentana.Irse(); // Se va y se autodestruye
            npcActualEnVentana = null;
        }

        Debug.Log($"- Vaciando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue();
            if (npcEnCola != null)
            {
                npcEnCola.Irse(); // Se va y se autodestruye
            }
        }
        colaNPCs.Clear();

        temporizador = 0f;
        Debug.Log("GestorCompradores: NPCs despachados por noche.");
    }

    public void AbrirTienda()
    {
        // NOTA: Asumiendo que 'HoraDelDia' es un enum que has definido y está accesible.
        // Para evitar dependencia directa del Enum, asumimos que Noche es el índice 2 como en el log de inicio.
        if (GestorJuego.Instance != null && (int)(object)GestorJuego.Instance.horaActual != 2) // Asumiendo que 2 es Noche
        {
            tiendaAbierta = true;
            compradoresHabilitados = true;
            Debug.Log("La tienda ha sido abierta por el jugador.");
        }
        else
        {
            Debug.Log("No se puede abrir la tienda de noche. Debes esperar a que amanezca.");
        }
    }

    public void CerrarTienda()
    {
        tiendaAbierta = false;
        compradoresHabilitados = false;
        DespawnTodosNPCsPorNoche();
        Debug.Log("La tienda ha sido cerrada por el jugador.");
    }
}
