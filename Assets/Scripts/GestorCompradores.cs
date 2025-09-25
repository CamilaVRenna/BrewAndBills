using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GestorCompradores : MonoBehaviour
{
    [Header("Configuración General")]
    public Transform puntoAparicion;
    public Transform posicionVentana;
    public Transform puntoMiradaVentana;
    public float intervaloAparicion = 10.0f;
    public Transform puntoSalidaNPC;

    [Tooltip("Arrastra aquí TODOS los prefabs de NPC diferentes que pueden aparecer.")]
    public List<GameObject> prefabsNPCsPosibles;

    [Tooltip("Número máximo de NPCs que pueden estar en la cola + en la ventanilla al mismo tiempo.")]
    public int maximoNPCsActivos = 5;

    // Añade esta línea en tu script GestorCompradores.cs
    [Header("Catálogo de Recetas")]
    public CatalogoRecetas catalogoRecetas;

    [Header("Pedidos y Sonidos")]
    public List<PedidoPocionData> listaMaestraPedidos;
    public AudioClip sonidoNuevoPedido;

    // --- NUEVAS REFERENCIAS PARA CONECTAR LOS SCRIPTS ---
    [Header("Referencias de UI y Audio")]
    public GestorUI uiGestor; // Esta referencia ya no necesitará la lógica de burbujas del GestorUI
    public AudioSource audioSource;
    // ---------------------------------------------------

    [Header("NPC Especial (Palita)")]
    public GameObject prefabNPCTienda;
    private bool npctiendaEntregadoHoy = false;
    private bool npctiendaActivo = false;

    private Queue<NPCComprador> colaNPCs = new Queue<NPCComprador>();
    private NPCComprador npcActualEnVentana = null;
    private float temporizador = 0f;

    private const string PREF_NPCTIENDA_ENTREGADO = "NPCTiendaEntregado";

    private bool NPCTiendaYaEntregado
    {
        get => PlayerPrefs.GetInt(PREF_NPCTIENDA_ENTREGADO, 0) == 1;
        set => PlayerPrefs.SetInt(PREF_NPCTIENDA_ENTREGADO, value ? 1 : 0);
    }

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
        bool esDeNoche = false;

        if (GestorJuego.Instance != null)
        {
            limiteDiarioOk = GestorJuego.Instance.ObtenerNPCsGeneradosHoy() < GestorJuego.Instance.limiteNPCsPorDia;
            esDeNoche = GestorJuego.Instance.horaActual == HoraDelDia.Noche;
        }
        else
        {
            Debug.LogError("GestorJuego no encontrado para verificar el límite diario!");
            return false;
        }

        bool puedeGenerar = limiteConcurrenteOk && limiteDiarioOk && !esDeNoche;
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

            // --- CÓDIGO A CORREGIR EN GestorCompradores.cs ---
            // El NPC viejo no tiene 'public PedidoPocionData pedido', pero sí tiene 'public List<PedidoPocionData> pedidosPosibles'
            // Y una lógica interna para asignar 'pedidoActual'

            // Aquí NO asignamos directamente 'controladorNPC.pedido = ...'
            // En cambio, asumimos que el NPC ya tiene su propia lista de 'pedidosPosibles'
            // O que la listaMaestraPedidos del gestor se usará si el NPC no tiene una.
            // Para la versión antigua de NPCComprador, la lógica de asignación de pedido
            // se hace INTERNAMENTE en el método SolicitarPocion() del NPC,
            // después de que el NPC llegue a la ventana.

            // Por lo tanto, esta sección de aquí se elimina o se comenta,
            // ya que el NPC viejo lo gestiona por sí mismo.
            /*
            if (listaMaestraPedidos != null && listaMaestraPedidos.Count > 0)
            {
                int indicePedido = Random.Range(0, listaMaestraPedidos.Count);
                controladorNPC.pedido = listaMaestraPedidos[indicePedido];
            }
            else
            {
                Debug.LogError("La lista maestra de pedidos está vacía en GestorCompradores.");
                Destroy(objetoNPC); // Destruir el NPC para evitar problemas
                return;
            }
            */
            // FIN DE CÓDIGO A CORREGIR

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
        npcActualEnVentana.IrAVentana(posicionVentana.position);
        // Cuando el NPC llega a la ventana, internamente cambiará su estado a EsperandoAtencion
        // y de ahí a EnVentanaEsperando, donde llamará a SolicitarPocion() para mostrar la UI.
        // NO necesitamos llamar a IniciarPedidoYTimer() o similar desde aquí.
    }

    public void NPCTermino(NPCComprador npcQueTermino)
    {
        if (npcQueTermino == npcActualEnVentana)
        {
            Debug.Log($"{npcQueTermino.gameObject.name} ha terminado en la ventana. Liberando puesto.");
            npcActualEnVentana = null;
            // El NPC ya se encarga de ocultar su propio bocadillo al irse
            // No necesitas uiGestor.OcultarPedido() aquí.
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
            // El NPC viejo gestiona su propia salida y destrucción.
            // Le pedimos que se vaya en lugar de destruirlo directamente,
            // para que su lógica de OnDestroy (que limpia el bocadillo) se ejecute.
            // Además, el NPC ya debería ocultar su bocadillo internamente al irse.
            npcActualEnVentana.Irse(); // Llama al método de salida del NPC
            npcActualEnVentana = null;
        }

        Debug.Log($"Limpiando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue();
            if (npcEnCola != null)
            {
                Debug.Log($"- Despachando NPC en cola: {npcEnCola.gameObject.name}");
                // Le decimos al NPC que se vaya para que gestione su propia limpieza de UI.
                npcEnCola.Irse();
            }
        }
        colaNPCs.Clear();

        temporizador = 0f;
        Debug.Log("GestorCompradores: Reinicio completado. Temporizador a 0.");

        npctiendaEntregadoHoy = false;
    }

    public void DespawnTodosNPCsPorNoche()
    {
        Debug.LogWarning("GestorCompradores: Se hizo de noche. Despachando a todos los NPCs...");

        if (npcActualEnVentana != null)
        {
            // --- CÓDIGO A CORREGIR ---
            // En vez de IniciarRetirada (que no existe en el NPC viejo), llamamos a Irse()
            npcActualEnVentana.Irse();
            npcActualEnVentana = null;
        }

        Debug.Log($"- Vaciando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue();
            if (npcEnCola != null)
            {
                // --- CÓDIGO A CORREGIR ---
                // En vez de IniciarRetirada, llamamos a Irse()
                npcEnCola.Irse();
            }
        }
        colaNPCs.Clear();

        temporizador = 0f;
        Debug.Log("GestorCompradores: NPCs despachados por noche.");
    }

    public void AbrirTienda()
    {
        if (GestorJuego.Instance != null && GestorJuego.Instance.horaActual != HoraDelDia.Noche)
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

    public void NPCTiendaTermino()
    {
        npctiendaActivo = false;
        NPCTiendaYaEntregado = true;
        compradoresHabilitados = true;
    }

    public void GenerarNPCTienda()
    {
        if (puntoAparicion == null || posicionVentana == null || puntoSalidaNPC == null)
        {
            Debug.LogError("Faltan puntos de aparición, ventana o salida para NPCTienda.");
            return;
        }

        GameObject obj = Instantiate(prefabNPCTienda, puntoAparicion.position, puntoAparicion.rotation);
        NPCTienda npcTienda = obj.GetComponent<NPCTienda>();
        if (npcTienda != null)
        {
            npcTienda.puntoVentana = posicionVentana;
            npcTienda.puntoSalida = puntoSalidaNPC;
            npcTienda.gestor = this;
        }
        else
        {
            Debug.LogError("El prefabNPCTienda no tiene el script NPCTienda.");
            Destroy(obj);
        }
    }

    [HideInInspector]
    public bool tiendaAbierta = false;

    [HideInInspector]
    public bool compradoresHabilitados = false;
}