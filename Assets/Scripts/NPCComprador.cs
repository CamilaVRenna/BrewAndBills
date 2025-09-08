using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

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
    [Header("Movimiento Simple")]
    public float velocidadMovimiento = 4.0f;
    public float velocidadRotacion = 360f;
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
    private Vector3 destinoActual;
    private bool mirandoVentana = false;
    private GameObject instanciaBocadilloActual = null;
    private TextMeshProUGUI textoBocadilloActual = null;
    private Coroutine coroutineOcultarBocadillo = null;
    private Coroutine coroutineRetrasarSalida = null;
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
        estadoActual = EstadoNPC.Inactivo;
    }

    void Update()
    {
        if (estadoActual == EstadoNPC.MoviendoAVentana || estadoActual == EstadoNPC.MoviendoASalida)
        {
            animator?.SetBool("Caminata", true);
            animator?.SetBool("Idle", false);
            MoverHaciaDestino();
            return;
        }

        if (estadoActual == EstadoNPC.EsperandoAtencion)
        {
            animator?.SetBool("Idle", true);
            animator?.SetBool("Caminata", false);
            if (instanciaBocadilloActual == null || !instanciaBocadilloActual.activeSelf)
                MostrarBocadillo("[E]", false);

            GirarHaciaVentana();
            return;
        }

        if (estadoActual == EstadoNPC.EnVentanaEsperando)
        {
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

            GirarHaciaVentana();
        }
    }

    void GirarHaciaVentana()
    {
        if (mirandoVentana || gestor == null || gestor.puntoMiradaVentana == null) return;
        Vector3 dir = gestor.puntoMiradaVentana.position - transform.position;
        Vector3 dirHoriz = new Vector3(dir.x, 0, dir.z);
        if (dirHoriz.sqrMagnitude > 0.001f)
        {
            Quaternion rotObj = Quaternion.LookRotation(dirHoriz);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObj, velocidadRotacion * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, rotObj) < 1.0f)
            {
                transform.rotation = rotObj;
                mirandoVentana = true;
                Debug.Log($"{gameObject.name} terminó de girar hacia la ventana.");
            }
        }
        else mirandoVentana = true;
    }

    void MoverHaciaDestino()
    {
        Vector3 dir = destinoActual - transform.position;
        Vector3 dirHoriz = new Vector3(dir.x, 0, dir.z);
        if (dirHoriz.sqrMagnitude > 0.001f)
        {
            Quaternion rotObj = Quaternion.LookRotation(dirHoriz);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObj, velocidadRotacion * Time.deltaTime);
        }
        float paso = velocidadMovimiento * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, destinoActual, paso);

        if (Vector3.Distance(transform.position, destinoActual) < 0.05f)
        {
            transform.position = destinoActual;
            if (estadoActual == EstadoNPC.MoviendoAVentana)
            {
                Debug.Log($"{gameObject.name} llegó a la ventana.");
                estadoActual = EstadoNPC.EsperandoAtencion;
                mirandoVentana = false;
                MostrarBocadillo("[E]", false);
                if (GestorAudio.Instancia != null && gestor != null && gestor.sonidoNuevoPedido != null)
                    SolicitarPocion();
            }
            else if (estadoActual == EstadoNPC.MoviendoASalida)
            {
                Debug.Log($"{gameObject.name} llegó a la salida. Destruyendo...");
                estadoActual = EstadoNPC.Inactivo;
                Destroy(gameObject);
            }
        }
    }

    public void IrAVentana(Vector3 posVentana)
    {
        if (estadoActual != EstadoNPC.Inactivo) return;
        destinoActual = posVentana;
        estadoActual = EstadoNPC.MoviendoAVentana;
        gameObject.SetActive(true);
        Debug.Log($"{gameObject.name} yendo a la ventana (movimiento simple)...");
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

        // Se ha eliminado toda la lógica del temporizador para evitar el error.
        if (instanciaBocadilloActual == null)
        {
            Debug.LogError("instanciaBocadilloActual es NULL al intentar buscar el temporizador.");
        }
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

    void Irse()
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
            destinoActual = gestor.puntoSalidaNPC.position;
            estadoActual = EstadoNPC.MoviendoASalida;
            Debug.Log($"{gameObject.name} se va hacia {destinoActual} (movimiento simple)... Estado: {estadoActual}");
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
        }
    }

    public void IniciarPedidoYTimer()
    {
        if (estadoActual != EstadoNPC.EsperandoAtencion)
        {
            Debug.LogWarning($"Se intentó iniciar pedido para {gameObject.name} pero su estado era {estadoActual}");
            return;
        }
        Debug.Log($"Iniciando pedido para {gameObject.name} (sin temporizador).");
        estadoActual = EstadoNPC.EnVentanaEsperando;
        SolicitarPocion();
        if (mostrarBocadilloAlIniciar)
        {
            MostrarBocadillo(ObtenerTextoOriginalPedido(), false);
        }
    }

    public bool EstaEsperandoAtencion() => estadoActual == EstadoNPC.EsperandoAtencion;
    public bool EstaEsperandoEntrega() => estadoActual == EstadoNPC.EnVentanaEsperando;
}