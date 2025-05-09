using UnityEngine;
using System.Collections.Generic;

public class GestorCompradores : MonoBehaviour
{
    [Header("Configuraci�n General")]
    //public GameObject prefabNPC;
    public Transform puntoAparicion;
    public Transform posicionVentana;
    public Transform puntoMiradaVentana;
    public float intervaloAparicion = 10.0f;
    public Transform puntoSalidaNPC;

    [Tooltip("Arrastra aqu� TODOS los prefabs de NPC diferentes que pueden aparecer.")]
    public List<GameObject> prefabsNPCsPosibles; // <<--- CAMBIADO A LISTA

    [Tooltip("N�mero m�ximo de NPCs que pueden estar en la cola + en la ventanilla al mismo tiempo.")]
    public int maximoNPCsActivos = 5; // <<--- NUEVA VARIABLE PARA EL L�MITE

    [Header("Pedidos y Sonidos")]
    public List<PedidoPocionData> listaMaestraPedidos;
    public AudioClip sonidoNuevoPedido;

    // --- Estado Interno ---
    private Queue<NPCComprador> colaNPCs = new Queue<NPCComprador>();
    private NPCComprador npcActualEnVentana = null;
    private float temporizador = 0f;

    void Update()
    {
        // Incrementa el temporizador
        temporizador += Time.deltaTime;

        // Si ha pasado suficiente tiempo Y A�N NO HEMOS ALCANZADO EL L�MITE...
        if (temporizador >= intervaloAparicion && PuedeGenerarMasNPCs()) // <<--- CONDICI�N MODIFICADA
        {
            temporizador = 0f; // Reinicia temporizador SOLO si generamos o intentamos generar
            GenerarNPC();
        }
        // Si no se cumple la condici�n (tiempo o l�mite), el temporizador sigue contando,
        // pero no se reinicia ni se genera NPC, esperando al siguiente ciclo.

        // Asigna al siguiente si la ventana est� libre (sin cambios)
        if (npcActualEnVentana == null && colaNPCs.Count > 0)
        {
            AsignarSiguienteNPC();
        }
    }

    // --- NUEVA FUNCI�N PRIVADA ---
    // Comprueba si se pueden generar m�s NPCs seg�n el l�mite
    private bool PuedeGenerarMasNPCs()
    {
        // L�mite concurrente (el que ya ten�amos)
        int totalNPCsActivos = colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0);
        bool limiteConcurrenteOk = totalNPCsActivos < maximoNPCsActivos;

        // L�mite diario (NUEVO)
        bool limiteDiarioOk = false;
        bool esDeNoche = false; // <<<--- DECLARACI�N DE LA VARIABLE

        if (GestorJuego.Instance != null) // Acceder via Singleton
        {
            // Comprobar si los generados HOY son menores que el l�mite diario
            limiteDiarioOk = GestorJuego.Instance.ObtenerNPCsGeneradosHoy() < GestorJuego.Instance.limiteNPCsPorDia;
            esDeNoche = GestorJuego.Instance.horaActual == HoraDelDia.Noche; // <<--- NUEVA COMPROBACI�N
        }
        else
        {
            Debug.LogError("GestorJuego no encontrado para verificar l�mite diario!");
            return false; // No generar si no podemos verificar
        }

        // Solo generar si se cumplen TODOS los l�mites Y NO es de noche
        bool puedeGenerar = limiteConcurrenteOk && limiteDiarioOk && !esDeNoche; // <<--- A�ADIDO !esDeNoche

        // Solo generar si AMBOS l�mites est�n OK
        if (!limiteConcurrenteOk) // Log opcional
        {
            // Debug.Log("No se genera NPC: L�mite concurrente alcanzado.");
        }
        if (!limiteDiarioOk) // Log opcional
        {
            // Debug.Log("No se genera NPC: L�mite diario alcanzado.");
        }

        return limiteConcurrenteOk && limiteDiarioOk && !esDeNoche;
    }
    // --- FIN NUEVA FUNCI�N ---

    // GenerarNPC (sin cambios internos, pero ahora solo se llama si PuedeGenerarMasNPCs es true)
    void GenerarNPC()
    {
        // --- MODIFICADO: Comprobar la LISTA de prefabs ---
        // Asegurarse de que la lista exista y tenga al menos un prefab asignado
        if (prefabsNPCsPosibles == null || prefabsNPCsPosibles.Count == 0)
        {
            Debug.LogError("�La lista 'prefabsNPCsPosibles' est� vac�a o no asignada en GestorCompradores! No se pueden generar NPCs.");
            return; // Salir si no hay prefabs para elegir
        }
        // Comprobar el punto de aparici�n (igual que antes)
        if (puntoAparicion == null)
        {
            Debug.LogError("�Falta asignar Punto Aparicion en GestorCompradores!");
            return;
        }
        // --- FIN COMPROBACI�N ---

        // --- NUEVO: Elegir un Prefab al Azar de la Lista ---
        int indicePrefab = Random.Range(0, prefabsNPCsPosibles.Count); // Elige un �ndice aleatorio
        GameObject prefabAUsar = prefabsNPCsPosibles[indicePrefab]; // Obtiene el prefab de esa posici�n en la lista

        // Comprobar si el prefab elegido es v�lido (por si un elemento de la lista qued� vac�o)
        if (prefabAUsar == null)
        {
            Debug.LogError($"El elemento {indicePrefab} en la lista 'prefabsNPCsPosibles' est� vac�o (None).");
            return; // No instanciar si el prefab elegido es nulo
        }
        // --- FIN ELEGIR PREFAB ---

        // --- MODIFICADO: Instanciar el prefab ELEGIDO ---
        GameObject objetoNPC = Instantiate(prefabAUsar, puntoAparicion.position, puntoAparicion.rotation);
        // ----------------------------------------------

        NPCComprador controladorNPC = objetoNPC.GetComponent<NPCComprador>();

        if (controladorNPC != null)
        {
            controladorNPC.gestor = this;
            colaNPCs.Enqueue(controladorNPC); // A�ade a la cola

            // Registrar NPC generado hoy (igual que antes)
            if (GestorJuego.Instance != null)
            {
                GestorJuego.Instance.RegistrarNPCGeneradoHoy();
            }
            else { Debug.LogWarning("GenerarNPC: No se encontr� GestorJuego para registrar NPC diario."); }


            // Log m�s informativo indicando qu� tipo de NPC se gener�
            Debug.Log($"NPC {objetoNPC.name} (Tipo: {prefabAUsar.name}) generado y a�adido a la cola. (Total en cola: {colaNPCs.Count}, Total activos: {colaNPCs.Count + (npcActualEnVentana != null ? 1 : 0)})");

        }
        else
        {
            // Log de error mencionando el prefab espec�fico que fall�
            Debug.LogError($"�El Prefab '{prefabAUsar.name}' no tiene el script 'NPCComprador'!");
            Destroy(objetoNPC); // Destruir la instancia creada incorrectamente
        }
    }

    // AsignarSiguienteNPC (sin cambios)
    void AsignarSiguienteNPC()
    {
        // ... (c�digo igual que antes) ...
        if (npcActualEnVentana != null) return;
        npcActualEnVentana = colaNPCs.Dequeue();
        Debug.Log($"Asignando a {npcActualEnVentana.gameObject.name} a la ventana. ({colaNPCs.Count} restantes en cola)");
        npcActualEnVentana.gameObject.SetActive(true);
        npcActualEnVentana.IrAVentana(posicionVentana.position);
    }

    // NPCTermino (sin cambios)
    public void NPCTermino(NPCComprador npcQueTermino)
    {
        // ... (c�digo igual que antes) ...
        if (npcQueTermino == npcActualEnVentana)
        {
            Debug.Log($"{npcQueTermino.gameObject.name} ha terminado en la ventana. Liberando puesto.");
            npcActualEnVentana = null;
        }
        else
        {
            Debug.LogWarning($"Un NPC ({npcQueTermino?.gameObject.name}) que NO estaba en la ventana intent� notificar t�rmino.");
            if (npcActualEnVentana == npcQueTermino) { npcActualEnVentana = null; }
        }
    }

    // ObtenerNPCActual (sin cambios)
    public NPCComprador ObtenerNPCActual()
    {
        // ... (c�digo igual que antes) ...
        return npcActualEnVentana;
    }

    // --- NUEVO M�TODO P�BLICO ---
    public void ReiniciarParaNuevoDia()
    {
        Debug.Log("GestorCompradores: Reiniciando para nuevo d�a...");

        // 1. Destruir el NPC actual en la ventana (si hay uno)
        if (npcActualEnVentana != null)
        {
            Debug.Log($"Destruyendo NPC en ventana: {npcActualEnVentana.gameObject.name}");
            Destroy(npcActualEnVentana.gameObject);
            npcActualEnVentana = null;
        }

        // 2. Destruir todos los NPCs en la cola de espera
        Debug.Log($"Limpiando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue(); // Saca el siguiente de la cola
            if (npcEnCola != null) // Comprobar por si acaso
            {
                Debug.Log($"- Destruyendo NPC en cola: {npcEnCola.gameObject.name}");
                Destroy(npcEnCola.gameObject); // Destruye su GameObject
            }
        }
        // Asegurarse de que la cola quede vac�a (Dequeue ya lo hace, pero Clear() es expl�cito)
        colaNPCs.Clear();

        // 3. Reiniciar el temporizador de aparici�n
        temporizador = 0f; // Para que el primer NPC tarde 'intervaloAparicion' en aparecer
        Debug.Log("GestorCompradores: Reinicio completado. Temporizador a 0.");
    }
    // --- FIN NUEVO M�TODO ---

    // M�todo para eliminar todos los NPCs activos cuando se hace de noche
    public void DespawnTodosNPCsPorNoche()
    {
        Debug.LogWarning("GestorCompradores: Se hizo de noche. Despachando a todos los NPCs...");

        // 1. NPC en la ventana: Simplemente lo destruimos para que desaparezca r�pido
        if (npcActualEnVentana != null)
        {
            Debug.Log($"- Despawneando NPC en ventana: {npcActualEnVentana.gameObject.name}");
            // npcActualEnVentana.Irse(); // Podr�a usar Irse, pero quiz�s es m�s directo destruir
            Destroy(npcActualEnVentana.gameObject);
            npcActualEnVentana = null;
        }

        // 2. NPCs en la cola
        Debug.Log($"- Vaciando cola de {colaNPCs.Count} NPCs...");
        while (colaNPCs.Count > 0)
        {
            NPCComprador npcEnCola = colaNPCs.Dequeue();
            if (npcEnCola != null)
            {
                Debug.Log($"- Despawneando NPC en cola: {npcEnCola.gameObject.name}");
                Destroy(npcEnCola.gameObject);
            }
        }
        colaNPCs.Clear(); // Asegurar que quede vac�a

        // 3. Resetear temporizador de spawn por si acaso
        temporizador = 0f;
        Debug.Log("GestorCompradores: NPCs despawneados por noche.");
    }

}