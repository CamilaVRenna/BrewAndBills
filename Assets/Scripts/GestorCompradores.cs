using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GestorCompradores : MonoBehaviour
{
    [Header("Configuraci�n General")]
    public Transform puntoAparicion;
    public Transform posicionVentana;
    public Transform puntoMiradaVentana;
    public float intervaloAparicion = 10.0f;
    public Transform puntoSalidaNPC;

    [Tooltip("Arrastra aqu� TODOS los prefabs de NPC diferentes que pueden aparecer.")]
    public List<GameObject> prefabsNPCsPosibles;

    [Tooltip("N�mero m�ximo de NPCs que pueden estar en la cola + en la ventanilla al mismo tiempo.")]
    public int maximoNPCsActivos = 5;

� � // A�ade esta l�nea en tu script GestorCompradores.cs
� � [Header("Cat�logo de Recetas")]
    public CatalogoRecetas catalogoRecetas;

    [Header("Pedidos y Sonidos")]
    public List<PedidoPocionData> listaMaestraPedidos;
    public AudioClip sonidoNuevoPedido;

� � // --- NUEVAS REFERENCIAS PARA CONECTAR LOS SCRIPTS ---
� � [Header("Referencias de UI y Audio")]
    public GestorUI uiGestor; // Esta referencia ya no necesitar� la l�gica de burbujas del GestorUI
� � public AudioSource audioSource;
� � // ---------------------------------------------------

� � [Header("NPC Especial (Palita)")]
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
            Debug.LogError("GestorJuego no encontrado para verificar el l�mite diario!");
            return false;
        }

        bool puedeGenerar = limiteConcurrenteOk && limiteDiarioOk && !esDeNoche;
        return puedeGenerar;
    }

    void GenerarNPC()
    {
        if (prefabsNPCsPosibles == null || prefabsNPCsPosibles.Count == 0)
        {
            Debug.LogError("�La lista 'prefabsNPCsPosibles' est� vac�a o no asignada en GestorCompradores! No se pueden generar NPCs.");
            return;
        }
        if (puntoAparicion == null)
        {
            Debug.LogError("�Falta asignar Punto Aparicion en GestorCompradores!");
            return;
        }

        int indicePrefab = Random.Range(0, prefabsNPCsPosibles.Count);
        GameObject prefabAUsar = prefabsNPCsPosibles[indicePrefab];

        if (prefabAUsar == null)
        {
            Debug.LogError($"El elemento {indicePrefab} en la lista 'prefabsNPCsPosibles' est� vac�o (None).");
            return;
        }

        GameObject objetoNPC = Instantiate(prefabAUsar, puntoAparicion.position, puntoAparicion.rotation);

        NPCComprador controladorNPC = objetoNPC.GetComponent<NPCComprador>();

        if (controladorNPC != null)
        {
            controladorNPC.gestor = this;

� � � � � � // --- C�DIGO A CORREGIR EN GestorCompradores.cs ---
� � � � � � // El NPC viejo no tiene 'public PedidoPocionData pedido', pero s� tiene 'public List<PedidoPocionData> pedidosPosibles'
� � � � � � // Y una l�gica interna para asignar 'pedidoActual'

� � � � � � // Aqu� NO asignamos directamente 'controladorNPC.pedido = ...'
� � � � � � // En cambio, asumimos que el NPC ya tiene su propia lista de 'pedidosPosibles'
� � � � � � // O que la listaMaestraPedidos del gestor se usar� si el NPC no tiene una.
� � � � � � // Para la versi�n antigua de NPCComprador, la l�gica de asignaci�n de pedido
� � � � � � // se hace INTERNAMENTE en el m�todo SolicitarPocion() del NPC,
� � � � � � // despu�s de que el NPC llegue a la ventana.

� � � � � � // Por lo tanto, esta secci�n de aqu� se elimina o se comenta,
� � � � � � // ya que el NPC viejo lo gestiona por s� mismo.
� � � � � � /*
� � � � � � if (listaMaestraPedidos != null && listaMaestraPedidos.Count > 0)
� � � � � � {
� � � � � � � � int indicePedido = Random.Range(0, listaMaestraPedidos.Count);
� � � � � � � � controladorNPC.pedido = listaMaestraPedidos[indicePedido];
� � � � � � }
� � � � � � else
� � � � � � {
� � � � � � � � Debug.LogError("La lista maestra de pedidos est� vac�a en GestorCompradores.");
� � � � � � � � Destroy(objetoNPC); // Destruir el NPC para evitar problemas
� � � � � � � � return;
� � � � � � }
� � � � � � */
� � � � � � // FIN DE C�DIGO A CORREGIR

� � � � � � colaNPCs.Enqueue(controladorNPC);

            if (GestorJuego.Instance != null)
            {
                GestorJuego.Instance.RegistrarNPCGeneradoHoy();
            }
            else { Debug.LogWarning("GenerarNPC: No se encontr� GestorJuego para registrar NPC diario."); }

            Debug.Log($"NPC {objetoNPC.name} (Tipo: {prefabAUsar.name}) generado y a�adido a la cola. (Total en cola: {colaNPCs.Count}, Total activos: {colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0)})");
        }
        else
        {
            Debug.LogError($"�El Prefab '{prefabAUsar.name}' no tiene el script 'NPCComprador'!");
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
� � � � // Cuando el NPC llega a la ventana, internamente cambiar� su estado a EsperandoAtencion
� � � � // y de ah� a EnVentanaEsperando, donde llamar� a SolicitarPocion() para mostrar la UI.
� � � � // NO necesitamos llamar a IniciarPedidoYTimer() o similar desde aqu�.
� � }

    public void NPCTermino(NPCComprador npcQueTermino)
    {
        if (npcQueTermino == npcActualEnVentana)
        {
            Debug.Log($"{npcQueTermino.gameObject.name} ha terminado en la ventana. Liberando puesto.");
            npcActualEnVentana = null;
� � � � � � // El NPC ya se encarga de ocultar su propio bocadillo al irse
� � � � � � // No necesitas uiGestor.OcultarPedido() aqu�.
� � � � }
        else
        {
            Debug.LogWarning($"Un NPC ({npcQueTermino?.gameObject.name}) que NO estaba en la ventana intent� notificar t�rmino.");
            if (npcActualEnVentana == npcQueTermino) { npcActualEnVentana = null; }
        }
    }

    public NPCComprador ObtenerNPCActual()
    {
        return npcActualEnVentana;
    }

    public void ReiniciarParaNuevoDia()
    {
        Debug.Log("GestorCompradores: Reiniciando para nuevo d�a...");

        if (npcActualEnVentana != null)
        {
� � � � � � // El NPC viejo gestiona su propia salida y destrucci�n.
� � � � � � // Le pedimos que se vaya en lugar de destruirlo directamente,
� � � � � � // para que su l�gica de OnDestroy (que limpia el bocadillo) se ejecute.
� � � � � � // Adem�s, el NPC ya deber�a ocultar su bocadillo internamente al irse.
� � � � � � npcActualEnVentana.Irse(); // Llama al m�todo de salida del NPC
� � � � � � npcActualEnVentana = null;
        }

        Debug.Log($"Limpiando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue();
            if (npcEnCola != null)
            {
                Debug.Log($"- Despachando NPC en cola: {npcEnCola.gameObject.name}");
� � � � � � � � // Le decimos al NPC que se vaya para que gestione su propia limpieza de UI.
� � � � � � � � npcEnCola.Irse();
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
� � � � � � // --- C�DIGO A CORREGIR ---
� � � � � � // En vez de IniciarRetirada (que no existe en el NPC viejo), llamamos a Irse()
� � � � � � npcActualEnVentana.Irse();
            npcActualEnVentana = null;
        }

        Debug.Log($"- Vaciando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue();
            if (npcEnCola != null)
            {
� � � � � � � � // --- C�DIGO A CORREGIR ---
� � � � � � � � // En vez de IniciarRetirada, llamamos a Irse()
� � � � � � � � npcEnCola.Irse();
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
            Debug.LogError("Faltan puntos de aparici�n, ventana o salida para NPCTienda.");
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