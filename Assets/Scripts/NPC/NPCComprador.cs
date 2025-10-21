using UnityEngine;

using System.Collections;

using System.Collections.Generic;

using System.Linq;

using TMPro;

using UnityEngine.AI;



[System.Serializable]

public class DialogoEspecificoNPC

{

    [Tooltip("La receta para la cual este NPC dirá algo único.")]

    public PedidoPocionData receta;

    [Tooltip("La frase exacta que dirá este NPC para esa receta.")]

    [TextArea(1, 3)]

    public string dialogoUnico;

}



// Define diferentes estilos de personalidad para el diálogo de respaldo.

public enum EstiloDialogo

{

    Normal,

    Gruñon,

    Formal,

    Nervioso

}



public class NPCComprador : MonoBehaviour

{

    private enum EstadoNPC { MoviendoAVentana, EsperandoAtencion, EnVentanaEsperando, ProcesandoEntrega, EsperandoParaSalir, MoviendoASalida, Inactivo }

    private EstadoNPC estadoActual = EstadoNPC.Inactivo;



    [HideInInspector] public GestorCompradores gestor;



    private NavMeshAgent navMeshAgent;



    [Header("Pedidos Posibles")]

    public List<PedidoPocionData> pedidosPosibles;

    public List<PedidoPocionData> listaPedidosEspecificos;

    public bool usarListaEspecifica = false;



    [Header("Diálogos Personalizados (Opcional)")]

    public EstiloDialogo estiloPersonalidad = EstiloDialogo.Normal; // Campo de personalidad

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

    private Vector3 destinoActual;

    private float tiempoRestanteEspera;

    private float tiempoRestanteEsperaAtencion;

    private bool mirandoVentana = false;

    private GameObject instanciaBocadilloActual = null;

    private TextMeshProUGUI textoBocadilloActual = null;

    private TextMeshProUGUI textoTemporizadorActual = null;

    private Coroutine coroutineOcultarBocadillo = null;

    private Coroutine coroutineRetrasarSalida = null;

    private Vector3 destinoSalida; // Almacena la posición de salida.



    [Header("Temporizador Espera")]

    public float tiempoMaximoEsperaAtencion = 10.0f;

    public string mensajeTiempoEsperaAgotado = "¡Por lo que veo no tienen empleados, adiós!";

    public float tiempoMaximoEspera = 30.0f;

    public string mensajeTiempoAgotado = "¡Eres demasiado lento, adiós!";

    public AudioClip sonidoTiempoAgotado;



    [Header("UI General")]

    public TextMeshProUGUI textoTemporizadorCanvas; // Referencia global que se mantuvo



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

        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)

        {

            Debug.LogError($"¡NavMeshAgent no encontrado en el NPC {gameObject.name}! Asegúrate de que el GameObject tenga un componente NavMeshAgent.");

        }

        else

        {

            // Desactivar al inicio. Esto previene que se mueva antes de tiempo.

            // La llamada a 'isStopped = true' se ELIMINA para evitar el error de NavMesh.

            navMeshAgent.enabled = false;

        }



        estadoActual = EstadoNPC.Inactivo;

        tiempoRestanteEspera = tiempoMaximoEspera;

        tiempoRestanteEsperaAtencion = tiempoMaximoEsperaAtencion;



        // Búsqueda opcional de la UI

        if (textoTemporizadorCanvas == null)

            textoTemporizadorCanvas = GameObject.Find("Temporizador")?.GetComponent<TextMeshProUGUI>();



        Debug.Log($"[{gameObject.name}] Awake: Inicializado. Estado: {estadoActual}");

    }



    void Update()

    {

        MoverHaciaDestino();



        if (estadoActual == EstadoNPC.EsperandoAtencion)

        {

            tiempoRestanteEsperaAtencion -= Time.deltaTime;

            if (tiempoRestanteEsperaAtencion <= 0)

            {

                TiempoAgotadoEsperandoAtencion();

                return;

            }



            // Muestra el bocadillo de "Atención [E]"

            if (instanciaBocadilloActual == null || !instanciaBocadilloActual.activeSelf || textoBocadilloActual.text != "[E]")

                MostrarBocadillo("[E]", false);



            GirarHaciaVentana();



            // Actualizar temporizador de atención en el bocadillo

            if (textoTemporizadorActual != null && textoTemporizadorActual.gameObject.activeSelf)

            {

                textoTemporizadorActual.text = "Atención: " + Mathf.CeilToInt(tiempoRestanteEsperaAtencion).ToString();

            }

            return;

        }



        if (estadoActual == EstadoNPC.EnVentanaEsperando)

        {

            tiempoRestanteEspera -= Time.deltaTime;

            if (tiempoRestanteEspera <= 0)

            {

                TiempoAgotado();

                return;

            }



            // Asegurar que el bocadillo del pedido esté visible

            if (instanciaBocadilloActual == null || !instanciaBocadilloActual.activeSelf || textoBocadilloActual.text == "[E]")

            {

                // Solo regenerar el pedido si es necesario

                if (pedidoActual == null) SolicitarPocion();

                MostrarBocadillo(ObtenerTextoOriginalPedido(), false);

            }



            // Asegurar que el temporizador de entrega esté visible

            if (textoTemporizadorActual != null && !textoTemporizadorActual.gameObject.activeSelf)

            {

                // Nota: Esto solo funciona si el bocadillo tiene el componente hijo llamado "TextoTemporizador"

                var timerTransform = instanciaBocadilloActual?.transform.Find("CanvasBocadillo/FondoBocadillo/TextoTemporizador");

                if (timerTransform != null) textoTemporizadorActual = timerTransform.GetComponent<TextMeshProUGUI>();



                if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(true);

            }



            GirarHaciaVentana();



            // Actualizar temporizador de entrega en el bocadillo

            if (textoTemporizadorActual != null && textoTemporizadorActual.gameObject.activeSelf)

            {

                textoTemporizadorActual.text = "Entrega: " + Mathf.CeilToInt(tiempoRestanteEspera).ToString();

            }

        }

    }



    // ------------------------------------------------------------------

    // Métodos de Interacción del Jugador

    // ------------------------------------------------------------------



    /// <summary>

    /// MÉTODO DE INTERACCIÓN PRINCIPAL: Comprueba si el nombre del ítem entregado

    /// (del inventario) coincide con el pedido del NPC.

    /// </summary>

    /// <param name="nombrePocionEntregada">Nombre del ítem seleccionado del inventario.</param>

    /// <returns>True si es la poción correcta y la entrega es exitosa.</returns>

    public bool IntentarEntregarPocionPorNombre(string nombrePocionEntregada)

    {

        if (estadoActual != EstadoNPC.EnVentanaEsperando || pedidoActual == null)

        {

            Debug.LogWarning($"Entrega por nombre fallida: NPC {gameObject.name} no estaba esperando o no tenía pedido. Estado: {estadoActual}");

            GiveFeedback("No te he pedido nada...");

            return false;

        }



        // --- Lógica de Comprobación ---

        string nombreEsperado = pedidoActual.nombreResultadoPocion.Trim().ToLower();

        string nombreEntregado = nombrePocionEntregada.Trim().ToLower();



        if (nombreEntregado == nombreEsperado)

        {

            Debug.Log($"{gameObject.name}: ¡Éxito! Entregada la poción correcta: {nombrePocionEntregada}.");



            // Lógica de éxito: Pagar, dar feedback y salir.

            GiveFeedback(mensajeFeedbackCorrecto, sonidoPocionCorrecta);

            if (GestorJuego.Instance != null)

            {

                int recompensaFinal = Mathf.Max(0, recompensaBase - (penalizacionPorError * intentosFallidos));

                GestorJuego.Instance.AnadirDinero(recompensaFinal);

                // NOTA: La remoción del ítem del inventario DEBE hacerse en InteraccionJugador.cs.

            }

            else Debug.LogError("¡GestorJuego no encontrado para añadir dinero!");



            Irse(); // El NPC se va

            return true;

        }

        else

        {

            // Lógica de fallo: Feedback y posible salida si es el segundo fallo.

            intentosFallidos++;

            Debug.Log($"{gameObject.name}: Esta no es la poción que pedí. Esperaba {nombreEsperado}. Intento fallido #{intentosFallidos}.");



            if (intentosFallidos >= 2)

            {

                GiveFeedback(mensajeSegundoFallo, sonidoPocionIncorrecta);

                Irse();

                return false;

            }



            GiveFeedback(mensajeFeedbackIncorrecto, sonidoPocionIncorrecta);

            // Vuelve a mostrar el pedido original después del feedback de fallo

            StartCoroutine(RestaurarPedidoDespuesDeFeedback());

            return false;

        }

    }



    /// <summary>

    /// MÉTODO DE INTERACCIÓN: Llama al NPC cuando el jugador presiona 'E' para iniciar la atención.

    /// </summary>

    public void IniciarPedidoYTimer()

    {

        if (estadoActual != EstadoNPC.EsperandoAtencion)

        {

            Debug.LogWarning($"Se intentó iniciar pedido para {gameObject.name} pero su estado era {estadoActual}");

            return;

        }

        Debug.Log($"Iniciando pedido y timer principal para {gameObject.name}");



        // Transicionar a esperar entrega

        estadoActual = EstadoNPC.EnVentanaEsperando;

        tiempoRestanteEsperaAtencion = 0; // Detiene el timer de espera de atención



        SolicitarPocion();

        tiempoRestanteEspera = tiempoMaximoEspera;



        // Configurar la UI del timer y bocadillo

        if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(true);

        if (mostrarBocadilloAlIniciar)

        {

            MostrarBocadillo(ObtenerTextoOriginalPedido(), false);

        }

    }



    // ------------------------------------------------------------------

    // Métodos de Movimiento y Estado

    // ------------------------------------------------------------------



    void TiempoAgotadoEsperandoAtencion()

    {

        Debug.Log($"{gameObject.name} se cansó de esperar atención.");

        GiveFeedback(mensajeTiempoEsperaAgotado, sonidoTiempoAgotado);

        Irse();

    }



    void TiempoAgotado()

    {

        Debug.Log($"{gameObject.name} se cansó de esperar la poción.");

        GiveFeedback(mensajeTiempoAgotado, sonidoTiempoAgotado);

        Irse();

    }



    void GirarHaciaVentana()

    {

        // Solo girar si no estamos ya mirando la ventana

        if (mirandoVentana || gestor == null || gestor.puntoMiradaVentana == null) return;



        // Si NavMeshAgent está activo, usa su rotación automática (si no está detenido).

        if (navMeshAgent != null && navMeshAgent.enabled && !navMeshAgent.isStopped && navMeshAgent.updateRotation)

        {

            mirandoVentana = true;

            return;

        }



        // Rotación manual si el NavMeshAgent está detenido o deshabilitado.

        Vector3 dir = gestor.puntoMiradaVentana.position - transform.position;

        Vector3 dirHoriz = new Vector3(dir.x, 0, dir.z);

        if (dirHoriz.sqrMagnitude > 0.001f)

        {

            Quaternion rotObj = Quaternion.LookRotation(dirHoriz);

            // Usamos una velocidad de giro fija para que no dependa del NavMeshAgent.

            float rotSpeed = 360f;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObj, rotSpeed * Time.deltaTime);



            if (Quaternion.Angle(transform.rotation, rotObj) < 1.0f)

            {

                transform.rotation = rotObj;

                mirandoVentana = true;

            }

        }

        else mirandoVentana = true;

    }



    void MoverHaciaDestino()

    {

        if (navMeshAgent == null || !navMeshAgent.enabled || (estadoActual != EstadoNPC.MoviendoAVentana && estadoActual != EstadoNPC.MoviendoASalida))

        {

            animator?.SetBool("Caminata", false);

            animator?.SetBool("Idle", true);

            return;

        }



        // Control de animación

        if (navMeshAgent.velocity.magnitude > 0.1f)

        {

            animator?.SetBool("Caminata", true);

            animator?.SetBool("Idle", false);

        }

        else

        {

            animator?.SetBool("Caminata", false);

            animator?.SetBool("Idle", true);

        }



        // Lógica de llegada al destino

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.1f)

        {

            if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)

            {

                navMeshAgent.isStopped = true;

                navMeshAgent.enabled = false;



                animator?.SetBool("Caminata", false);

                animator?.SetBool("Idle", true);



                if (estadoActual == EstadoNPC.MoviendoAVentana)

                {

                    Debug.Log($"{gameObject.name} llegó a la ventana (NavMesh).");

                    estadoActual = EstadoNPC.EsperandoAtencion;

                    mirandoVentana = false; // Forzar el giro en el update

                    tiempoRestanteEsperaAtencion = tiempoMaximoEsperaAtencion;

                    MostrarBocadillo("[E]", false);



                    // Inicializar el texto del temporizador de atención (que está dentro del bocadillo)

                    var timerTransform = instanciaBocadilloActual?.transform.Find("CanvasBocadillo/FondoBocadillo/TextoTemporizador");

                    if (timerTransform != null) textoTemporizadorActual = timerTransform.GetComponent<TextMeshProUGUI>();

                    if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(true);

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



    /// <summary>

    /// Inicia el movimiento del NPC hacia la ventana. Acepta la posición de salida también.

    /// </summary>

    /// <param name="posVentana">Posición a la que debe ir el NPC para atender.</param>

    /// <param name="posSalida">Posición final a la que debe ir el NPC al salir.</param>

    public void IrAVentana(Vector3 posVentana, Vector3 posSalida)

    {

        if (estadoActual != EstadoNPC.Inactivo)

        {

            Debug.LogWarning($"[{gameObject.name}] IrAVentana ignorado. El NPC ya está en estado: {estadoActual}");

            return;

        }

        destinoActual = posVentana;

        destinoSalida = posSalida; // Almacenar el punto de salida



        if (navMeshAgent != null)

        {

            navMeshAgent.enabled = true;

            navMeshAgent.isStopped = false;

            navMeshAgent.SetDestination(posVentana);

        }

        else

        {

            Debug.LogError($"NavMeshAgent es NULL en {gameObject.name}. No se puede mover.");

        }



        estadoActual = EstadoNPC.MoviendoAVentana;

        gameObject.SetActive(true);

    }



    void SolicitarPocion()

    {

        if (estadoActual != EstadoNPC.EnVentanaEsperando) return;



        // Lógica para elegir la lista de pedidos

        List<PedidoPocionData> listaAUsar = usarListaEspecifica && listaPedidosEspecificos?.Count > 0 ? listaPedidosEspecificos :

                      pedidosPosibles?.Count > 0 ? pedidosPosibles :

                      gestor?.listaMaestraPedidos?.Count > 0 ? gestor.listaMaestraPedidos : null;



        if (listaAUsar == null || listaAUsar.Count == 0)

        {

            Debug.LogError($"NPC {gameObject.name} no pudo encontrar una lista de pedidos válida.");

            return;

        }



        pedidoActual = listaAUsar[Random.Range(0, listaAUsar.Count)];

        MostrarBocadillo(ObtenerTextoOriginalPedido(), false);



        // Inicializar la referencia al TextMeshPro del temporizador dentro del bocadillo

        if (instanciaBocadilloActual != null)

        {

            var timerTransform = instanciaBocadilloActual.transform.Find("CanvasBocadillo/FondoBocadillo/TextoTemporizador");

            if (timerTransform != null)

            {

                textoTemporizadorActual = timerTransform.GetComponent<TextMeshProUGUI>();

                if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(true);

            }

        }

    }



    public void Irse()

    {

        // Evitar que se llame dos veces o en un estado de transición

        if (estadoActual == EstadoNPC.MoviendoASalida || estadoActual == EstadoNPC.Inactivo || coroutineRetrasarSalida != null || estadoActual == EstadoNPC.EsperandoParaSalir)

            return;



        // Si el NPC se va sin una entrega procesada, forzar el estado de salida para el feedback.

        if (estadoActual != EstadoNPC.ProcesandoEntrega && estadoActual != EstadoNPC.MoviendoASalida)

        {

            estadoActual = EstadoNPC.ProcesandoEntrega;

        }



        estadoActual = EstadoNPC.EsperandoParaSalir;

        coroutineRetrasarSalida = StartCoroutine(RetrasarSalidaCoroutine());

    }



    IEnumerator RetrasarSalidaCoroutine()

    {

        // Asegura que el feedback visual y sonoro se muestre antes de que el NPC se vaya.

        yield return new WaitForSeconds(duracionFeedback);

        coroutineRetrasarSalida = null;



        if (estadoActual == EstadoNPC.EsperandoParaSalir)

        {

            if (gestor != null)

            {

                // Notifica al gestor que el slot de la ventana está libre.

                gestor.NPCTermino(this);

            }

            IniciarMovimientoHaciaSalida();

        }

    }



    void IniciarMovimientoHaciaSalida()

    {

        OcultarBocadillo();

        // Usamos la posición de salida almacenada en destinoSalida

        if (destinoSalida != Vector3.zero)

        {

            destinoActual = destinoSalida;



            if (navMeshAgent != null)

            {

                navMeshAgent.enabled = true;

                navMeshAgent.isStopped = false;

                navMeshAgent.SetDestination(destinoActual);

            }

            else

            {

                Debug.LogError($"NavMeshAgent es NULL en {gameObject.name}. No se puede mover a la salida.");

                estadoActual = EstadoNPC.Inactivo;

                Destroy(gameObject);

                return;

            }



            estadoActual = EstadoNPC.MoviendoASalida;

        }

        else

        {

            // Salida de emergencia si la configuración es incorrecta

            Debug.LogError("IniciarMovimientoHaciaSalida: Destino de salida no fue asignado en IrAVentana. Se autodestruye el NPC.");

            estadoActual = EstadoNPC.Inactivo;

            Destroy(gameObject);

        }

    }



    // ------------------------------------------------------------------

    // Métodos de UI y Helpers

    // ------------------------------------------------------------------



    public void MostrarBocadillo(string texto, bool autoOcultar = false)

    {

        if (coroutineOcultarBocadillo != null)

        {

            StopCoroutine(coroutineOcultarBocadillo);

            coroutineOcultarBocadillo = null;

        }



        // Creación perezosa del bocadillo

        if (instanciaBocadilloActual == null)

        {

            if (prefabBocadilloUI != null && puntoAnclajeBocadillo != null)

            {

                instanciaBocadilloActual = Instantiate(prefabBocadilloUI, puntoAnclajeBocadillo.position, puntoAnclajeBocadillo.rotation, puntoAnclajeBocadillo);

                textoBocadilloActual = instanciaBocadilloActual.GetComponentInChildren<TextMeshProUGUI>(true); // Buscar inactivos

                if (textoBocadilloActual == null)

                {

                    Debug.LogError("¡Prefab Bocadillo UI sin TextMeshProUGUI!", instanciaBocadilloActual);

                    Destroy(instanciaBocadilloActual);

                    instanciaBocadilloActual = null;

                    return;

                }



                // Intentar encontrar el TextMeshPro del temporizador

                var timerTransform = instanciaBocadilloActual.transform.Find("CanvasBocadillo/FondoBocadillo/TextoTemporizador");

                if (timerTransform != null) textoTemporizadorActual = timerTransform.GetComponent<TextMeshProUGUI>();

            }

            else

            {

                return;

            }

        }



        if (textoBocadilloActual != null)

        {

            textoBocadilloActual.text = texto;

            instanciaBocadilloActual.SetActive(true);



            // Ocultar el temporizador si estamos mostrando feedback o el mensaje de atención

            if (texto == "[E]" || autoOcultar)

            {

                if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(false);

            }



            if (autoOcultar && duracionFeedback > 0)

                coroutineOcultarBocadillo = StartCoroutine(OcultarBocadilloDespuesDe(duracionFeedback));

        }

    }



    IEnumerator OcultarBocadilloDespuesDe(float segundos)

    {

        yield return new WaitForSeconds(segundos);

        OcultarBocadillo();

        coroutineOcultarBocadillo = null;

    }



    /// <summary>

    /// Oculta el bocadillo y detiene su temporizador de auto-ocultamiento.

    /// </summary>

    public void OcultarBocadillo()

    {

        if (coroutineOcultarBocadillo != null) { StopCoroutine(coroutineOcultarBocadillo); coroutineOcultarBocadillo = null; }

        if (textoTemporizadorActual != null) textoTemporizadorActual.gameObject.SetActive(false);

        if (instanciaBocadilloActual != null) instanciaBocadilloActual.SetActive(false);

    }



    /// <summary>

    /// NUEVA SOBRECARGA para mensajes sin sonido.

    /// </summary>

    void GiveFeedback(string message)

    {

        GiveFeedback(message, null);

    }



    void GiveFeedback(string message, AudioClip sound)

    {

        // Se asegura de que el NPC esté en un estado donde el feedback sea relevante

        if (estadoActual == EstadoNPC.EsperandoParaSalir) { /* Ya se va, no hace falta más feedback de texto */ }

        else if (estadoActual == EstadoNPC.EnVentanaEsperando || estadoActual == EstadoNPC.EsperandoAtencion)

        {

            MostrarBocadillo(message, true);

        }



        if (GestorAudio.Instancia != null && sound != null) GestorAudio.Instancia.ReproducirSonido(sound);

    }



    void OnDestroy()

    {

        if (instanciaBocadilloActual != null) Destroy(instanciaBocadilloActual);

        if (coroutineOcultarBocadillo != null) StopCoroutine(coroutineOcultarBocadillo);

        if (coroutineRetrasarSalida != null) StopCoroutine(coroutineRetrasarSalida);



        if (navMeshAgent != null)

        {

            // Se ELIMINA la llamada a navMeshAgent.isStopped = true para evitar el error.

            navMeshAgent.enabled = false;

        }

    }



    private string ObtenerTextoOriginalPedido()

    {

        if (pedidoActual == null) return "¿Necesitas algo?";



        // 1. Diálogo específico para este NPC/Receta

        var especifico = dialogosEspecificos?.FirstOrDefault(d => d != null && d.receta == pedidoActual);

        if (especifico != null && !string.IsNullOrEmpty(especifico.dialogoUnico))

            return especifico.dialogoUnico;



        // 2. Diálogo genérico de la propia receta

        var genericos = pedidoActual.dialogosPedidoGenericos?.Where(d => !string.IsNullOrEmpty(d)).ToList();

        if (genericos != null && genericos.Count > 0)

            return genericos[Random.Range(0, genericos.Count)];



        // 3. Diálogo de respaldo por nombre (MODIFICADO para usar la personalidad)

        return GenerarDialogoRespaldoConEstilo(pedidoActual.nombreResultadoPocion);

    }



    /// <summary>

    /// Genera una frase de pedido de respaldo basada en la personalidad del NPC.

    /// </summary>

    private string GenerarDialogoRespaldoConEstilo(string nombrePocion)

    {

        switch (estiloPersonalidad)

        {

            case EstiloDialogo.Gruñon:

                return $"¡Dame una {nombrePocion} rápido y no me hagas perder el tiempo!";

            case EstiloDialogo.Formal:

                return $"Estimado alquimista, ¿sería tan amable de prepararme una {nombrePocion} a la brevedad?";

            case EstiloDialogo.Nervioso:

                return $"N-Necesito, uhm... ¿una {nombrePocion}? ¡Por favor, que nadie me vea!";

            case EstiloDialogo.Normal:

            default:

                return $"Mmm... ¿Tendrías una {nombrePocion}?";

        }

    }



    private IEnumerator RestaurarPedidoDespuesDeFeedback()

    {

        // Espera un poco más que la duración del feedback para asegurarse de que el bocadillo se oculte

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



    // ------------------------------------------------------------------

    // Getters de Estado

    // ------------------------------------------------------------------

    public bool EstaEsperandoAtencion() => estadoActual == EstadoNPC.EsperandoAtencion;

    public bool EstaEsperandoEntrega() => estadoActual == EstadoNPC.EnVentanaEsperando;



    // Métodos Antiguos de Entrega (por ingredientes), se mantienen pero no se usan con el nuevo sistema de inventario.

    public void IntentarEntregarPocion(List<DatosIngrediente> pocionEntregada)

    {

        // Lógica de comprobación por ingredientes si la usas con el caldero.

        Debug.LogWarning("IntentarEntregarPocion (por ingredientes) llamado. Asegúrate de usar IntentarEntregarPocionPorNombre si usas inventario.");

        // ... (Tu lógica de comprobación de ingredientes aquí)

    }



    bool CompararListasIngredientes(List<DatosIngrediente> lista1, List<DatosIngrediente> lista2)

    {

        // Lógica de comparación de ingredientes

        return false;

    }

}