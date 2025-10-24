using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

// Aseg�rate de que estos tipos existan en tu proyecto (NPCComprador, NPCMovimiento, PedidoPocionData, GestorUI, GestorJuego)

public class GestorCompradores : MonoBehaviour
{
    // --- Configuraci�n e Inicializaci�n ---
    [Header("Configuraci�n General")]
    public Transform puntoAparicion;
    public Transform posicionVentana;
    public Transform puntoMiradaVentana;
    public float intervaloAparicion = 10.0f;
    [Tooltip("CR�TICO: Posici�n final donde el NPC se destruye al irse.")]
    public Transform puntoSalidaNPC;

    [Tooltip("Arrastra aqu� TODOS los prefabs de NPC diferentes que pueden aparecer.")]
    public List<GameObject> prefabsNPCsPosibles;
    [Tooltip("M�ximo de NPCs en escena (en cola + en ventana) al mismo tiempo.")]
    public int maximoNPCsActivos = 5; // L�mite de CONCURRENCIA

    [Header("Cat�logo de Recetas")]
    public List<PedidoPocionData> listaMaestraPedidos; // Lista principal de pedidos

    [Header("Referencias de UI y Audio")]
    public GestorUI uiGestor;
    public AudioClip sonidoNuevoPedido;

    // --- Estado Interno ---
    private Queue<NPCComprador> colaNPCs = new Queue<NPCComprador>();
    private NPCComprador npcActualEnVentana = null;
    private float temporizadorGeneracion = 0f;

    [HideInInspector] public bool tiendaAbierta = false;
    [HideInInspector] public bool compradoresHabilitados = false; // Controla la generaci�n por tiempo


    void Start()
    {
        // Validaci�n inicial de referencias cruciales
        if (GestorJuego.Instance == null)
        {
            Debug.LogError("GestorJuego.Instance no est� inicializado. El sistema de NPCs no funcionar� correctamente.");
        }

        if (puntoSalidaNPC == null)
        {
            Debug.LogError("CR�TICO: El puntoSalidaNPC no est� asignado. Los NPCs no podr�n salir correctamente.");
        }
    }

    void Update()
    {
        // La generaci�n solo ocurre si es de d�a Y est� habilitada
        if (!tiendaAbierta || !compradoresHabilitados) return;

        // 1. Controlar la Generaci�n de NPCs (por tiempo y l�mite de concurrencia)
        ManejarGeneracionNPCs();

        // 2. Controlar la Asignaci�n a la Ventana (la cola avanza)
        if (npcActualEnVentana == null && colaNPCs.Count > 0)
        {
            AsignarSiguienteNPC();
        }
    }

    // ------------------------------------------------------------------
    // L�gica de Generaci�n
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
        // �nica limitaci�n: el m�ximo de NPCs en la escena al mismo tiempo (concurrencia)
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
            Debug.LogError($"�El Prefab '{prefabAUsar.name}' no tiene el script 'NPCComprador'!");
            Destroy(objetoNPC);
        }
    }

    private bool ValidarConfiguracion()
    {
        if (prefabsNPCsPosibles == null || prefabsNPCsPosibles.Count == 0)
        {
            Debug.LogError("La lista 'prefabsNPCsPosibles' est� vac�a o no asignada.");
            return false;
        }
        if (puntoAparicion == null || posicionVentana == null || puntoSalidaNPC == null)
        {
            // A�adido posicionVentana a la verificaci�n
            Debug.LogError("�Falta asignar Punto Aparicion, Posicion Ventana o Punto Salida NPC!");
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
    // M�todos de Control P�blico
    // ------------------------------------------------------------------

    public void NPCTermino(NPCComprador npcQueTermino)
    {
        if (npcQueTermino == npcActualEnVentana)
        {
            npcActualEnVentana = null;
        }
        else
        {
            Debug.LogWarning($"Un NPC ({npcQueTermino?.gameObject.name}) que NO estaba en la ventana intent� notificar t�rmino. Esto es inesperado si la cola funciona bien.");
        }

        // No necesitamos forzar la generaci�n aqu�, el Update se encargar�
        // de asignar el siguiente NPC de la cola (si hay) y el temporizador 
        // se encargar� de generar uno nuevo si el l�mite lo permite.
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
            Debug.Log("No se puede abrir la tienda: no es de d�a.");
        }
    }

    /// <summary>
    /// Cierra la tienda, deshabilita la generaci�n de compradores y fuerza la salida de todos los NPCs activos.
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
    // M�todos de Limpieza y Reinicio
    // ------------------------------------------------------------------

    public void ReiniciarParaNuevoDia()
    {
        ForzarDespawnTodosNPCs(); // Limpiar la escena de cualquier NPC que haya quedado.
        temporizadorGeneracion = 0f;
        // La tienda se abre despu�s de esto mediante la llamada a AbrirTienda() o al evento del GestorJuego
    }

    /// <summary>
    /// Un �nico m�todo para gestionar la salida forzada de todos los NPCs, usado al cerrar la tienda o al reiniciar el d�a.
    /// Llama a Irse() en cada NPC, el cual se encargar� de su propia destrucci�n al llegar a puntoSalidaNPC.
    /// </summary>
    private void ForzarDespawnTodosNPCs()
    {
        // 1. NPC en ventanilla
        if (npcActualEnVentana != null)
        {
            // Llama al m�todo del NPC para que inicie su secuencia de salida y autodestrucci�n
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