using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.AI; // ¡Importante! Necesario para NavMeshAgent

[System.Serializable]
public class DialogoEspecificoNPC
{
    [Tooltip("La receta para la cual este NPC dirá algo único.")]
    public PedidoPocionData receta;
    [Tooltip("La frase exacta que dirá este NPC para esa receta.")]
    public string dialogoUnico;
}

public class NPCComprador : MonoBehaviour
{
    private enum EstadoNPC { MoviendoAVentana, EsperandoAtencion, EnVentanaEsperando, ProcesandoEntrega, EsperandoParaSalir, MoviendoASalida, Inactivo }
    private EstadoNPC estadoActual = EstadoNPC.Inactivo;

    [HideInInspector] public GestorCompradores gestor;

    // ELIMINADAS las variables de velocidadMovimiento y velocidadRotacion del movimiento simple.
    // El NavMeshAgent tiene su propia velocidad y velocidad angular en el Inspector.
    // [Header("Movimiento Simple")]
    // public float velocidadMovimiento = 4.0f;
    // public float velocidadRotacion = 360f;

    // --- AÑADIDO: Referencia al NavMeshAgent ---
    private NavMeshAgent navMeshAgent;

    [Header("Pedidos Posibles")]
    public List<PedidoPocionData> pedidosPosibles;
    public List<PedidoPocionData> listaPedidosEspecificos;
    public bool usarListaEspecifica = false;
    [Header("Diálogos Personalizados (Opcional)")]
    public List<DialogoEspecificoNPC> dialogosEspecificos;
    [Header("Feedback y Sonidos")]
    public string mensajeFeedbackCorrecto = "¡Muchas gracias!";
    public string mensajeFeedbackIncorrecto = "¡No sirves para nada!";
    public string mensajeSegundoFallo = "¡Nah! ¡Me voy de aquí!";
    public AudioClip sonidoPocionCorrecta;
    public AudioClip sonidoPocionIncorrecta;
    [Header("UI Bocadillo Pedido")]
    public GameObject prefabBocadilloUI;
    public Transform puntoAnclajeBocadillo;
    public float duracionFeedback = 3.0f;
    private PedidoPocionData pedidoActual = null;
    private int intentosFallidos = 0;
    private Vector3 destinoActual; // Se mantiene para debug o referencia, pero el NavMeshAgent usa su propio destino.
    private float tiempoRestanteEspera;
    private float tiempoRestanteEsperaAtencion;
    private bool mirandoVentana = false;
    private GameObject instanciaBocadilloActual = null;
    private TextMeshProUGUI textoBocadilloActual = null;
    private TextMeshProUGUI textoTemporizadorActual = null;
    private Coroutine coroutineOcultarBocadillo = null;
    private Coroutine coroutineRetrasarSalida = null;
    [Header("Temporizador Espera")]
    public float tiempoMaximoEsperaAtencion = 10.0f;
    public string mensajeTiempoEsperaAgotado = "¡Por lo que veo no tienen empleados, adiós!";
    public float tiempoMaximoEspera = 30.0f;
    public string mensajeTiempoAgotado = "¡Eres demasiado lento, adiós!";
    public AudioClip sonidoTiempoAgotado;
    [Header("UI General")]
    public TextMeshProUGUI textoTemporizadorCanvas;
    [Header("Animación")]
    private Animator animator;

    // Cambia el valor de la recompensa base y penalización
    private int recompensaBase = 20;
    private int penalizacionPorError = 5;

    public PedidoPocionData recetaInvisibilidad; // Arrastra la receta desde el Inspector

    public bool mostrarBocadilloAlIniciar = true;

    void Awake()
    {
        animator = GetComponent<Animator>();
        // --- AÑADIDO: Obtener referencia al NavMeshAgent y deshabilitarlo al inicio ---
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogError("¡NavMeshAgent no encontrado en el NPC! Asegúrate de que el GameObject tenga un componente NavMeshAgent.");
        }
        else
        {
            navMeshAgent.enabled = false; // Deshabilitar al inicio para que no interfiera.
            // Opcional: configurar si quieres que el agente rote el transform o el animator.
            // navMeshAgent.updateRotation = false; // Si tu Animator maneja la rotación con Root Motion
            // navMeshAgent.updatePosition = false; // Si tu Animator maneja la posición con Root Motion
        }

        estadoActual = EstadoNPC.Inactivo;
        tiempoRestanteEspera = tiempoMaximoEspera;
        tiempoRestanteEsperaAtencion = tiempoMaximoEsperaAtencion;
        if (textoTemporizadorCanvas == null)
            textoTemporizadorCanvas = GameObject.Find("Temporizador")?.GetComponent<TextMeshProUGUI>();
        Debug.Log($"[{gameObject.name}] Awake: Inicializado. Estado: {estadoActual}");
    }

    void Update()
    {
        // Debug.Log($"[{gameObject.name}] Update: Estado actual: {estadoActual}"); // <--- LOG OPCIONAL

        // --- MODIFICADO: La lógica de movimiento ahora se gestiona en MoverHaciaDestino(),
        // que a su vez interactúa con el NavMeshAgent. ---
        MoverHaciaDestino();

        if (estadoActual == EstadoNPC.EsperandoAtencion)
        {
            // --- Reincorporada la lógica de reducción de tiempo para EsperandoAtencion ---
            tiempoRestanteEsperaAtencion -= Time.deltaTime;
            if (tiempoRestanteEsperaAtencion <= 0)
            {
                TiempoAgotadoEsperandoAtencion();
                return;
            }

            Debug.Log($"[{gameObject.name}] Update: Estado EsperandoAtencion.");
            animator?.SetBool("Idle", true);
            animator?.SetBool("Caminata", false);
            if (instanciaBocadilloActual == null || !instanciaBocadilloActual.activeSelf)
                MostrarBocadillo("[E]", false);

            GirarHaciaVentana();
            // Actualiza el texto del temporizador si es visible y aplica al bocadillo de "esperando atención"
            if (textoTemporizadorActual != null && textoTemporizadorActual.gameObject.activeSelf)
            {
                textoTemporizadorActual.text = "Atención: " + Mathf.CeilToInt(tiempoRestanteEsperaAtencion).ToString();
            }
            return;
        }

        if (estadoActual == EstadoNPC.EnVentanaEsperando)
        {
            // --- Reincorporada la lógica de reducción de tiempo para EnVentanaEsperando ---
            tiempoRestanteEspera -= Time.deltaTime;
            if (tiempoRestanteEspera <= 0)
            {
                TiempoAgotado();
                return;
            }

            if (instanciaBocadilloActual == null)
            {
                Debug.LogError($"NPC {gameObject.name} en EnVentanaEsperando sin bocadillo. Forzando SolicitarPocion.");
                SolicitarPocion();
                return;
            }
            if (!instanciaBocadilloActual.activeSelf)
            {
                MostrarBocadillo(ObtenerTextoOriginalPedido(), false);
                Debug.LogWarning($"Reactivado bocadillo para {gameObject.name} (estaba inactivo).");
            }
            if (textoTemporizadorActual != null && !textoTemporizadorActual.gameObject.activeSelf)
            {
                textoTemporizadorActual.gameObject.SetActive(true);
                Debug.LogWarning($"Reactivado TextoTemporizador para {gameObject.name}.");
            }

            GirarHaciaVentana();
            // Actualiza el texto del temporizador si es visible y aplica al bocadillo de "esperando entrega"
            if (textoTemporizadorActual != null && textoTemporizadorActual.gameObject.activeSelf)
            {
                textoTemporizadorActual.text = "Entrega: " + Mathf.CeilToInt(tiempoRestanteEspera).ToString();
            }
        }
    }

    // Método para cuando se agota el tiempo de espera de atención inicial
    void TiempoAgotadoEsperandoAtencion()
    {
        Debug.Log($"{gameObject.name} se cansó de esperar atención.");
        GiveFeedback(mensajeTiempoEsperaAgotado, sonidoTiempoAgotado);
        Irse();
    }

    // Método para cuando se agota el tiempo de espera de la poción
    void TiempoAgotado()
    {
        Debug.Log($"{gameObject.name} se cansó de esperar la poción.");
        GiveFeedback(mensajeTiempoAgotado, sonidoTiempoAgotado);
        Irse();
    }

    void GirarHaciaVentana()
    {
        if (mirandoVentana || gestor == null || gestor.puntoMiradaVentana == null) return;

        // Si el NavMeshAgent está activo y gestiona la rotación, no hacemos nada aquí.
        // Se asume que navMeshAgent.updateRotation = true (por defecto) o que el Animator lo maneja
        // si navMeshAgent.updateRotation = false.
        // Aquí forzamos la rotación si el NavMeshAgent no está activo o lo hemos configurado para no rotar.
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.updateRotation)
        {
            Vector3 dir = gestor.puntoMiradaVentana.position - transform.position;
            Vector3 dirHoriz = new Vector3(dir.x, 0, dir.z);
            if (dirHoriz.sqrMagnitude > 0.001f)
            {
                Quaternion rotObj = Quaternion.LookRotation(dirHoriz);
                // Usamos la velocidad angular del NavMeshAgent si existe, sino una fija.
                float rotSpeed = (navMeshAgent != null && navMeshAgent.enabled) ? navMeshAgent.angularSpeed : 360f; // Usar una velocidad por defecto si el agente no está activo.
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObj, rotSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, rotObj) < 1.0f)
                {
                    transform.rotation = rotObj;
                    mirandoVentana = true;
                    Debug.Log($"{gameObject.name} terminó de girar hacia la ventana.");
                }
            }
            else mirandoVentana = true;
        }
        else
        {
            // Si el NavMeshAgent está activo y actualizando la rotación,
            // podemos considerar que ya está mirando en la dirección correcta o se ajustará.
            mirandoVentana = true;
        }
    }

    // --- MODIFICADO COMPLETAMENTE: MoverHaciaDestino ahora usa el NavMeshAgent ---
    void MoverHaciaDestino()
    {
        // Si no hay agente, o no está habilitado, o el estado no es de movimiento, no hacemos nada aquí.
        if (navMeshAgent == null || !navMeshAgent.enabled || (estadoActual != EstadoNPC.MoviendoAVentana && estadoActual != EstadoNPC.MoviendoASalida))
        {
            animator?.SetBool("Caminata", false); // Asegura que la animación de caminar se detenga si no hay movimiento activo
            animator?.SetBool("Idle", true);
            return;
        }

        // --- Control de animaciones basado en la velocidad del NavMeshAgent ---
        if (navMeshAgent.velocity.magnitude > 0.1f) // Si el agente se está moviendo realmente
        {
            animator?.SetBool("Caminata", true);
            animator?.SetBool("Idle", false);
        }
        else
        {
            animator?.SetBool("Caminata", false);
            animator?.SetBool("Idle", true);
        }

        // Comprobamos si el agente ha llegado al destino.
        // pathPending: true si está calculando el camino.
        // remainingDistance: distancia al destino final.
        // stoppingDistance: distancia a la que el agente se detendrá.
        // hasPath / velocity.sqrMagnitude: para asegurarnos de que realmente ha terminado de moverse.
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.1f) // Pequeño margen
        {
            if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
            {
                navMeshAgent.isStopped = true;   // Asegurarse de que el agente se detenga
                navMeshAgent.enabled = false;    // Deshabilitar el agente para que no controle más la posición/rotación
                // Opcional: ajustar la posición final para mayor precisión
                // transform.position = navMeshAgent.destination;

                animator?.SetBool("Caminata", false);
                animator?.SetBool("Idle", true);

                if (estadoActual == EstadoNPC.MoviendoAVentana)
                {
                    Debug.Log($"{gameObject.name} llegó a la ventana (NavMesh).");
                    estadoActual = EstadoNPC.EsperandoAtencion; // Cambiar a estado de espera de atención
                    mirandoVentana = false; // Para que gire hacia la ventana manualmente si NavMesh no rota
                    tiempoRestanteEsperaAtencion = tiempoMaximoEsperaAtencion;
                    MostrarBocadillo("[E]", false); // Muestra el bocadillo de espera de atención
                    if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(false);
                    // Aquí NO llamas a SolicitarPocion, eso ocurre cuando el jugador interactúa.
                }
                else if (estadoActual == EstadoNPC.MoviendoASalida)
                {
                    Debug.Log($"{gameObject.name} llegó a la salida (NavMesh). Destruyendo...");
                    estadoActual = EstadoNPC.Inactivo;
                    Destroy(gameObject);
                }
            }
        }
    }

    public void IrAVentana(Vector3 posVentana)
    {
        Debug.Log($"[{gameObject.name}] IrAVentana llamado. Estado ACTUAL al inicio: {estadoActual}");
        if (estadoActual != EstadoNPC.Inactivo)
        {
            Debug.LogWarning($"[{gameObject.name}] IrAVentana ignorado. El NPC ya está en estado: {estadoActual}");
            return;
        }
        destinoActual = posVentana; // Se mantiene para referencia y debug

        // --- MODIFICADO: Usar NavMeshAgent para establecer el destino ---
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = true; // Habilitar el NavMeshAgent
            navMeshAgent.isStopped = false; // Asegurarse de que no esté detenido
            navMeshAgent.SetDestination(posVentana); // ¡Establecer el destino!
            Debug.Log($"[{gameObject.name}] NavMeshAgent habilitado y destino establecido a: {posVentana}");
        }
        else
        {
            Debug.LogError($"NavMeshAgent es NULL en {gameObject.name}. No se puede mover.");
            // Si el NavMeshAgent es NULL, el NPC no se moverá. Considera un fallback o error.
        }

        estadoActual = EstadoNPC.MoviendoAVentana;
        gameObject.SetActive(true);
        Debug.Log($"[{gameObject.name}] IrAVentana: Estado cambiado a: {estadoActual}. Destino: {destinoActual}. Iniciando movimiento (NavMesh).");
    }

    void SolicitarPocion()
    {
        if (estadoActual != EstadoNPC.EnVentanaEsperando) return;
        List<PedidoPocionData> listaAUsar = usarListaEspecifica && listaPedidosEspecificos?.Count > 0 ? listaPedidosEspecificos :
                                             pedidosPosibles?.Count > 0 ? pedidosPosibles :
                                             gestor?.listaMaestraPedidos?.Count > 0 ? gestor.listaMaestraPedidos : null;
        if (listaAUsar == null || listaAUsar.Count == 0) return;

        pedidoActual = listaAUsar[Random.Range(0, listaAUsar.Count)];
        MostrarBocadillo(ObtenerTextoOriginalPedido(), false);
        if (instanciaBocadilloActual != null)
        {
            var timerTransform = instanciaBocadilloActual.transform.Find("CanvasBocadillo/FondoBocadillo/TextoTemporizador");
            if (timerTransform != null)
            {
                textoTemporizadorActual = timerTransform.GetComponent<TextMeshProUGUI>();
                if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(true);
                else Debug.LogError($"Objeto '{timerTransform.name}' encontrado, pero NO tiene componente TextMeshProUGUI!", timerTransform.gameObject);
            }
            else Debug.LogError("No se encontró la ruta 'CanvasBocadillo/FondoBocadillo/TextoTemporizador'.");
        }
        else Debug.LogError("instanciaBocadilloActual es NULL al intentar buscar el temporizador.");
        if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(true);
    }

    public void IntentarEntregarPocion(List<DatosIngrediente> pocionEntregada)
    {
        if (estadoActual != EstadoNPC.EnVentanaEsperando)
        {
            Debug.LogWarning($"Se intentó entregar poción a {gameObject.name} pero no estaba esperando (Estado: {estadoActual})");
            return;
        }
        if (pedidoActual == null)
        {
            Debug.LogWarning($"Se intentó entregar poción a {gameObject.name} pero no tenía pedido activo.");
            return;
        }
        Debug.Log($"NPC ({gameObject.name}) recibe poción. Comprobando...");
        estadoActual = EstadoNPC.ProcesandoEntrega;
        if (CompararListasIngredientes(pedidoActual.ingredientesRequeridos, pocionEntregada))
        {
            GiveFeedback(mensajeFeedbackCorrecto, sonidoPocionCorrecta);
            if (GestorJuego.Instance != null)
            {
                int recompensaFinal = Mathf.Max(0, recompensaBase - (penalizacionPorError * intentosFallidos));
                GestorJuego.Instance.AnadirDinero(recompensaFinal);
            }
            else Debug.LogError("¡GestorJuego no encontrado para añadir dinero!");
            Irse();
        }
        else
        {
            intentosFallidos++;
            Debug.Log($"Intento fallido #{intentosFallidos}");
            GiveFeedback(mensajeFeedbackIncorrecto, sonidoPocionIncorrecta);
            estadoActual = EstadoNPC.EnVentanaEsperando;
            StartCoroutine(RestaurarPedidoDespuesDeFeedback());
        }
    }

    public void Irse()
    {
        if (estadoActual == EstadoNPC.MoviendoASalida || estadoActual == EstadoNPC.Inactivo || coroutineRetrasarSalida != null || estadoActual == EstadoNPC.EsperandoParaSalir)
            return;
        if (estadoActual != EstadoNPC.ProcesandoEntrega && pedidoActual != null)
        {
            Debug.LogWarning($"Irse() llamado desde estado {estadoActual}. Forzando a ProcesandoEntrega.");
            estadoActual = EstadoNPC.ProcesandoEntrega;
        }
        estadoActual = EstadoNPC.EsperandoParaSalir;
        Debug.Log($"{gameObject.name} iniciando secuencia de salida (con retraso)...");
        coroutineRetrasarSalida = StartCoroutine(RetrasarSalidaCoroutine());
    }

    IEnumerator RetrasarSalidaCoroutine()
    {
        Debug.Log($"{gameObject.name}: Mostrando feedback final, esperando {duracionFeedback}s...");
        yield return new WaitForSeconds(duracionFeedback);
        coroutineRetrasarSalida = null;
        if (estadoActual == EstadoNPC.EsperandoParaSalir)
        {
            Debug.Log($"{gameObject.name}: Tiempo de espera terminado. Iniciando movimiento a salida.");
            if (gestor != null)
            {
                gestor.NPCTermino(this);
                Debug.Log($"NPCTermino notificado para {gameObject.name}");
            }
            else Debug.LogError("¡RetrasarSalidaCoroutine: El NPC no tiene referencia a su gestor!");
            IniciarMovimientoHaciaSalida();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Estado cambió mientras esperaba para salir ({estadoActual}). No se iniciará movimiento.");
            if (gestor != null) gestor.NPCTermino(this);
        }
    }

    void IniciarMovimientoHaciaSalida()
    {
        OcultarBocadillo();
        if (gestor != null && gestor.puntoSalidaNPC != null)
        {
            destinoActual = gestor.puntoSalidaNPC.position; // Se mantiene para referencia y debug

            // --- MODIFICADO: Usar NavMeshAgent para establecer el destino ---
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true; // Habilitar el NavMeshAgent
                navMeshAgent.isStopped = false; // Asegurarse de que no esté detenido
                navMeshAgent.SetDestination(destinoActual); // ¡Establecer el destino!
                Debug.Log($"[{gameObject.name}] NavMeshAgent habilitado y destino de salida establecido a: {destinoActual}");
            }
            else
            {
                Debug.LogError($"NavMeshAgent es NULL en {gameObject.name}. No se puede mover a la salida.");
                estadoActual = EstadoNPC.Inactivo;
                Destroy(gameObject); // Destruir si no se puede mover.
                return;
            }

            estadoActual = EstadoNPC.MoviendoASalida;
            Debug.Log($"{gameObject.name} se va hacia {destinoActual} (NavMesh)... Estado: {estadoActual}");
        }
        else
        {
            if (gestor == null) Debug.LogError("IniciarMovimientoHaciaSalida: gestor es null.");
            else if (gestor.puntoSalidaNPC == null) Debug.LogError("IniciarMovimientoHaciaSalida: gestor.puntoSalidaNPC es null. ¡Asigna el punto de salida en el Inspector del GestorNPCs!");
            Debug.LogError($"Punto de salida no config. o gestor no encontrado para NPC {gameObject.name}. Destruyendo.", this.gameObject);
            estadoActual = EstadoNPC.Inactivo;
            Destroy(gameObject);
        }
    }

    public void MostrarBocadillo(string texto, bool autoOcultar = false)
    {
        if (coroutineOcultarBocadillo != null)
        {
            StopCoroutine(coroutineOcultarBocadillo);
            coroutineOcultarBocadillo = null;
        }
        if (instanciaBocadilloActual == null)
        {
            if (prefabBocadilloUI != null && puntoAnclajeBocadillo != null)
            {
                instanciaBocadilloActual = Instantiate(prefabBocadilloUI, puntoAnclajeBocadillo.position, puntoAnclajeBocadillo.rotation, puntoAnclajeBocadillo);
                textoBocadilloActual = instanciaBocadilloActual.GetComponentInChildren<TextMeshProUGUI>();
                if (textoBocadilloActual == null)
                {
                    Debug.LogError("¡Prefab Bocadillo UI sin TextMeshProUGUI!", instanciaBocadilloActual);
                    Destroy(instanciaBocadilloActual);
                    instanciaBocadilloActual = null;
                    return;
                }
            }
            else
            {
                if (prefabBocadilloUI == null) Debug.LogError("¡Falta asignar 'Prefab Bocadillo UI'!", this.gameObject);
                if (puntoAnclajeBocadillo == null) Debug.LogError("¡Falta asignar 'Punto Anclaje Bocadillo'!", this.gameObject);
                return;
            }
        }
        if (textoBocadilloActual == null)
            textoBocadilloActual = instanciaBocadilloActual.GetComponentInChildren<TextMeshProUGUI>(true);

        if (textoBocadilloActual != null)
        {
            textoBocadilloActual.text = texto;
            instanciaBocadilloActual.SetActive(true);
            if (autoOcultar && duracionFeedback > 0)
                coroutineOcultarBocadillo = StartCoroutine(OcultarBocadilloDespuesDe(duracionFeedback));
        }
        else Debug.LogError("MostrarBocadillo: No se pudo encontrar/asignar textoBocadilloActual incluso después de instanciar.");
    }

    IEnumerator OcultarBocadilloDespuesDe(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        OcultarBocadillo();
        coroutineOcultarBocadillo = null;
    }

    void OcultarBocadillo()
    {
        if (coroutineOcultarBocadillo != null) { StopCoroutine(coroutineOcultarBocadillo); coroutineOcultarBocadillo = null; }
        if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(false);
        if (instanciaBocadilloActual != null) instanciaBocadilloActual.SetActive(false);
    }

    void GiveFeedback(string message, AudioClip sound)
    {
        if (GestorAudio.Instancia != null) GestorAudio.Instancia.ReproducirSonido(sound);
        MostrarBocadillo(message, true);
    }

    bool CompararListasIngredientes(List<DatosIngrediente> lista1, List<DatosIngrediente> lista2)
    {
        if (lista1 == null || lista2 == null || lista1.Count != lista2.Count) return false;
        var tempLista2 = new List<DatosIngrediente>(lista2);
        foreach (var item1 in lista1)
        {
            int idx = tempLista2.FindIndex(i => i == item1);
            if (idx < 0) return false;
            tempLista2.RemoveAt(idx);
        }
        return tempLista2.Count == 0;
    }

    void OnDestroy()
    {
        if (instanciaBocadilloActual != null) Destroy(instanciaBocadilloActual);
        if (coroutineOcultarBocadillo != null) StopCoroutine(coroutineOcultarBocadillo);
        if (coroutineRetrasarSalida != null) StopCoroutine(coroutineRetrasarSalida);
        // --- AÑADIDO: Asegurarse de que el NavMeshAgent se desactive al destruir el objeto ---
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }
    }

    private string ObtenerTextoOriginalPedido()
    {
        if (pedidoActual == null) return "¿Necesitas algo?";
        var especifico = dialogosEspecificos?.FirstOrDefault(d => d != null && d.receta == pedidoActual);
        if (especifico != null && !string.IsNullOrEmpty(especifico.dialogoUnico))
            return especifico.dialogoUnico;
        var genericos = pedidoActual.dialogosPedidoGenericos?.Where(d => !string.IsNullOrEmpty(d)).ToList();
        if (genericos != null && genericos.Count > 0)
            return genericos[Random.Range(0, genericos.Count)];
        return $"Mmm... ¿Tendrías una {pedidoActual.nombreResultadoPocion}?";
    }

    private IEnumerator RestaurarPedidoDespuesDeFeedback()
    {
        yield return new WaitForSeconds(duracionFeedback + 0.1f);
        if (estadoActual == EstadoNPC.EnVentanaEsperando && pedidoActual != null)
        {
            MostrarBocadillo(ObtenerTextoOriginalPedido(), false);
            if (textoTemporizadorActual != null)
            {
                textoTemporizadorActual.gameObject.SetActive(true);
            }
        }
    }

    public void IniciarPedidoYTimer()
    {
        if (estadoActual != EstadoNPC.EsperandoAtencion)
        {
            Debug.LogWarning($"Se intentó iniciar pedido para {gameObject.name} pero su estado era {estadoActual}");
            return;
        }
        Debug.Log($"Iniciando pedido y timer principal para {gameObject.name}");
        estadoActual = EstadoNPC.EnVentanaEsperando;
        SolicitarPocion();
        tiempoRestanteEspera = tiempoMaximoEspera;
        if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(true);
        if (mostrarBocadilloAlIniciar)
        {
            MostrarBocadillo(ObtenerTextoOriginalPedido(), false);
        }
    }

    public bool EstaEsperandoAtencion() => estadoActual == EstadoNPC.EsperandoAtencion;
    public bool EstaEsperandoEntrega() => estadoActual == EstadoNPC.EnVentanaEsperando;
}