using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

// Asegúrate de que estos tipos existan en tu proyecto (NPCComprador, NPCMovimiento, PedidoPocionData, GestorUI, GestorJuego)

public class GestorCompradores : MonoBehaviour
{
    // --- Configuración e Inicialización ---
    [Header("Configuración General")]
    public Transform puntoAparicion;
    public Transform posicionVentana;
    public Transform puntoMiradaVentana;
    public float intervaloAparicion = 10.0f;
    [Tooltip("CRÍTICO: Posición final donde el NPC se destruye al irse.")]
    public Transform puntoSalidaNPC;

    [Tooltip("Arrastra aquí TODOS los prefabs de NPC diferentes que pueden aparecer.")]
    public List<GameObject> prefabsNPCsPosibles;
    [Tooltip("Máximo de NPCs en escena (en cola + en ventana) al mismo tiempo.")]
    public int maximoNPCsActivos = 5; // Límite de CONCURRENCIA

    [Header("Catálogo de Recetas")]
    public List<PedidoPocionData> listaMaestraPedidos; // Lista principal de pedidos

    [Header("Referencias de UI y Audio")]
    public GestorUI uiGestor;
    public AudioClip sonidoNuevoPedido;

    // --- Estado Interno ---
    private Queue<NPCComprador> colaNPCs = new Queue<NPCComprador>();
    private NPCComprador npcActualEnVentana = null;
    private float temporizadorGeneracion = 0f;

    [HideInInspector] public bool tiendaAbierta = false;
    [HideInInspector] public bool compradoresHabilitados = false; // Controla la generación por tiempo


    void Start()
    {
        // Validación inicial de referencias cruciales
        if (GestorJuego.Instance == null)
        {
            Debug.LogError("GestorJuego.Instance no está inicializado. El sistema de NPCs no funcionará correctamente.");
        }

        if (puntoSalidaNPC == null)
        {
            Debug.LogError("CRÍTICO: El puntoSalidaNPC no está asignado. Los NPCs no podrán salir correctamente.");
        }
    }

    void Update()
    {
        // La generación solo ocurre si es de día Y está habilitada
        if (!tiendaAbierta || !compradoresHabilitados) return;

        // 1. Controlar la Generación de NPCs (por tiempo y límite de concurrencia)
        ManejarGeneracionNPCs();

        // 2. Controlar la Asignación a la Ventana (la cola avanza)
        if (npcActualEnVentana == null && colaNPCs.Count > 0)
        {
            AsignarSiguienteNPC();
        }
    }

    // ------------------------------------------------------------------
    // Lógica de Generación
    // ------------------------------------------------------------------

    void ManejarGeneracionNPCs()
    {
        temporizadorGeneracion += Time.deltaTime;
        if (temporizadorGeneracion >= intervaloAparicion && PuedeGenerarNPC())
        {
            temporizadorGeneracion = 0f;
            GenerarNPC();
        }
    }

    private bool PuedeGenerarNPC()
    {
        int totalNPCsActivos = colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0);
        // Única limitación: el máximo de NPCs en la escena al mismo tiempo (concurrencia)
        bool limiteConcurrenteOk = totalNPCsActivos < maximoNPCsActivos;

        return limiteConcurrenteOk;
    }

    void GenerarNPC()
    {
        if (!ValidarConfiguracion()) return;

        GameObject prefabAUsar = prefabsNPCsPosibles[Random.Range(0, prefabsNPCsPosibles.Count)];

        GameObject objetoNPC = Instantiate(prefabAUsar, puntoAparicion.position, puntoAparicion.rotation);
        NPCComprador controladorNPC = objetoNPC.GetComponent<NPCComprador>();

        if (controladorNPC != null)
        {
            controladorNPC.gestor = this;

            var npcMovimiento = objetoNPC.GetComponent<NPCMovimiento>();
            if (npcMovimiento != null && puntoMiradaVentana != null)
            {
                npcMovimiento.puntoMiradaVentana = puntoMiradaVentana;
            }

            colaNPCs.Enqueue(controladorNPC);

            // ELIMINADA la llamada a GestorJuego.Instance.RegistrarNPCGeneradoHoy()
        }
        else
        {
            Debug.LogError($"¡El Prefab '{prefabAUsar.name}' no tiene el script 'NPCComprador'!");
            Destroy(objetoNPC);
        }
    }

    private bool ValidarConfiguracion()
    {
        if (prefabsNPCsPosibles == null || prefabsNPCsPosibles.Count == 0)
        {
            Debug.LogError("La lista 'prefabsNPCsPosibles' está vacía o no asignada.");
            return false;
        }
        if (puntoAparicion == null || posicionVentana == null || puntoSalidaNPC == null)
        {
            // Añadido posicionVentana a la verificación
            Debug.LogError("¡Falta asignar Punto Aparicion, Posicion Ventana o Punto Salida NPC!");
            return false;
        }
        return true;
    }

    void AsignarSiguienteNPC()
    {
        if (npcActualEnVentana != null || colaNPCs.Count == 0) return;

        npcActualEnVentana = colaNPCs.Dequeue();

        // El NPC en cola debe estar activo para iniciar el movimiento (si se desactiva al entrar a la cola)
        npcActualEnVentana.gameObject.SetActive(true);

        // Inicia el movimiento a la ventana (usa los puntos de control del Gestor)
        npcActualEnVentana.IrAVentana(posicionVentana.position, puntoSalidaNPC.position);
    }

    // ------------------------------------------------------------------
    // Métodos de Control Público
    // ------------------------------------------------------------------

    public void NPCTermino(NPCComprador npcQueTermino)
    {
        if (npcQueTermino == npcActualEnVentana)
        {
            npcActualEnVentana = null;
        }
        else
        {
            Debug.LogWarning($"Un NPC ({npcQueTermino?.gameObject.name}) que NO estaba en la ventana intentó notificar término. Esto es inesperado si la cola funciona bien.");
        }

        // No necesitamos forzar la generación aquí, el Update se encargará
        // de asignar el siguiente NPC de la cola (si hay) y el temporizador 
        // se encargará de generar uno nuevo si el límite lo permite.
    }

    public void DespawnComprador(GameObject npcObjeto)
    {
        Destroy(npcObjeto);
    }

    public void AbrirTienda()
    {
        if (GestorJuego.Instance != null && GestorJuego.Instance.EstaDeDia())
        {
            tiendaAbierta = true;
            compradoresHabilitados = true;
            Debug.Log("Tienda abierta. Compradores habilitados.");
        }
        else
        {
            Debug.Log("No se puede abrir la tienda: no es de día.");
        }
    }

    /// <summary>
    /// Cierra la tienda, deshabilita la generación de compradores y fuerza la salida de todos los NPCs activos.
    /// Usado al cambiar a la noche.
    /// </summary>
    public void CerrarTienda()
    {
        tiendaAbierta = false;
        compradoresHabilitados = false;
        ForzarDespawnTodosNPCs();
        Debug.Log("Tienda cerrada. Compradores deshabilitados.");
    }

    // ------------------------------------------------------------------
    // Métodos de Limpieza y Reinicio
    // ------------------------------------------------------------------

    public void ReiniciarParaNuevoDia()
    {
        ForzarDespawnTodosNPCs(); // Limpiar la escena de cualquier NPC que haya quedado.
        temporizadorGeneracion = 0f;
        // La tienda se abre después de esto mediante la llamada a AbrirTienda() o al evento del GestorJuego
    }

    /// <summary>
    /// Un único método para gestionar la salida forzada de todos los NPCs, usado al cerrar la tienda o al reiniciar el día.
    /// Llama a Irse() en cada NPC, el cual se encargará de su propia destrucción al llegar a puntoSalidaNPC.
    /// </summary>
    private void ForzarDespawnTodosNPCs()
    {
        // 1. NPC en ventanilla
        if (npcActualEnVentana != null)
        {
            // Llama al método del NPC para que inicie su secuencia de salida y autodestrucción
            npcActualEnVentana.Irse();
            npcActualEnVentana = null;
        }

        // 2. NPCs en cola
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue();
            if (npcEnCola != null)
            {
                npcEnCola.Irse();
            }
        }
        colaNPCs.Clear();
    }

    // ------------------------------------------------------------------
    // Getters
    // ------------------------------------------------------------------

    public NPCComprador ObtenerNPCActual()
    {
        return npcActualEnVentana;
    }
}