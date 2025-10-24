using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

// =========================================================================
// DEFINICIONES AUXILIARES 
// =========================================================================

// Nota: Asumo que PedidoPocionData y GestorAudio existen en el proyecto.
// Nota: Asumo que NPCBocadilloUI existe y tiene los métodos/propiedades necesarios.

[System.Serializable]
public class DialogoEspecificoNPC
{
    [Tooltip("La receta para la cual este NPC dirá algo único.")]
    public PedidoPocionData receta;
    [Tooltip("La frase exacta que dirá este NPC para esa receta.")]
    [TextArea(1, 3)]
    public string dialogoUnico;
}

public enum EstiloDialogo
{
    Normal,
    Gruñon,
    Formal,
    Nervioso
}

// =========================================================================
// CLASE PRINCIPAL: NPCComprador
// =========================================================================

public class NPCComprador : MonoBehaviour
{
    private enum EstadoNPC { MoviendoAVentana, EsperandoAtencion, EnVentanaEsperando, ProcesandoEntrega, EsperandoParaSalir, MoviendoASalida, Inactivo }
    private EstadoNPC estadoActual = EstadoNPC.Inactivo;

    [HideInInspector] public GestorCompradores gestor; // Referencia al gestor

    // Referencias a los nuevos componentes
    private NPCMovimiento npcMovimiento;

    // Asumimos que NPCBocadilloUI es el script que gestiona la UI del bocadillo
    private NPCBocadilloUI npcUI;

    [Header("Pedidos Posibles")]
    public List<PedidoPocionData> pedidosPosibles;
    public List<PedidoPocionData> listaPedidosEspecificos;
    public bool usarListaEspecifica = false;

    [Header("Diálogos Personalizados (Opcional)")]
    public EstiloDialogo estiloPersonalidad = EstiloDialogo.Normal;
    public List<DialogoEspecificoNPC> dialogosEspecificos;

    [Header("Feedback y Sonidos")]
    public string mensajeFeedbackCorrecto = "¡Muchas gracias!";
    public string mensajeFeedbackIncorrecto = "¡No sirves para nada!";
    public string mensajeSegundoFallo = "¡Nah! ¡Me voy de aquí!";
    public AudioClip sonidoPocionCorrecta;
    public AudioClip sonidoPocionIncorrecta;
    public AudioClip sonidoTiempoAgotado;
    public string mensajeTiempoEsperaAgotado = "¡Por lo que veo no tienen empleados, adiós!";
    public string mensajeTiempoAgotado = "¡Eres demasiado lento, adiós!";

    [Header("Temporizador Espera")]
    public float tiempoMaximoEsperaAtencion = 10.0f;
    public float tiempoMaximoEspera = 30.0f;
    public bool mostrarBocadilloAlIniciar = true;

    // Variables internas de estado
    private PedidoPocionData pedidoActual = null;
    private int intentosFallidos = 0;
    private float tiempoRestanteEspera;
    private float tiempoRestanteEsperaAtencion;
    private Coroutine coroutineRetrasarSalida = null;

    // Recompensa y penalización
    private int recompensaBase = 20;
    private int penalizacionPorError = 5;

    // UI Específica
    // ELIMINADAS LAS REFERENCIAS A PREFAB Y PUNTO ANCLAJE DE ESTE SCRIPT.
    // ESTOS CAMPOS DEBEN ESTAR EN NPCBocadilloUI.cs SOLAMENTE.
    // [Header("Referencias UI Bocadillo")] 
    // public GameObject prefabBocadilloUI; 
    // public Transform puntoAnclajeBocadillo;


    void Awake()
    {
        npcMovimiento = GetComponent<NPCMovimiento>();
        npcUI = GetComponent<NPCBocadilloUI>(); // Obtener el componente NPCBocadilloUI

        if (npcMovimiento == null || npcUI == null)
        {
            Debug.LogError($"Faltan componentes NPCMovimiento o NPCBocadilloUI en {gameObject.name}. Asegúrate de añadirlos.");
            enabled = false;
            return;
        }

        // CORRECCIÓN CS1501: Inicializar la UI sin argumentos.
        // NPCBocadilloUI ahora usa sus referencias internas asignadas en su propio Inspector.
        npcUI.Inicializar();

        // Se mantiene la asignación de duracionFeedback, ya que es una propiedad de configuración
        // y no una referencia UI que rompa el encapsulamiento.
        npcUI.duracionFeedback = 3.0f;

        tiempoRestanteEspera = tiempoMaximoEspera;
        tiempoRestanteEsperaAtencion = tiempoMaximoEsperaAtencion;
        estadoActual = EstadoNPC.Inactivo;
    }

    void Update()
    {
        // 1. Manejo de Movimiento y Llegada (Delegado a NPCMovimiento)
        if (estadoActual == EstadoNPC.MoviendoAVentana || estadoActual == EstadoNPC.MoviendoASalida)
        {
            if (npcMovimiento.CheckearLlegadaDestino())
            {
                if (estadoActual == EstadoNPC.MoviendoAVentana)
                {
                    OnLlegarAVentana();
                }
                else if (estadoActual == EstadoNPC.MoviendoASalida)
                {
                    OnLlegarASalida();
                }
            }
        }

        // 2. Lógica de Temporizadores y Giro
        if (estadoActual == EstadoNPC.EsperandoAtencion)
        {
            ManejarEsperaAtencion();
        }
        else if (estadoActual == EstadoNPC.EnVentanaEsperando)
        {
            ManejarEsperaEntrega();
        }
    }

    // ------------------------------------------------------------------
    // MÉTODOS PÚBLICOS REQUERIDOS POR InteraccionJugador.cs (CS1061 FIX)
    // ------------------------------------------------------------------

    /// <summary>
    /// Muestra un bocadillo sobre el NPC. Llamado típicamente por InteraccionJugador.cs.
    /// </summary>
    /// <param name="texto">El mensaje a mostrar.</param>
    /// <param name="esFeedback">Si es un mensaje temporal de feedback.</param>
    public void MostrarBocadillo(string texto, bool esFeedback = false)
    {
        // Delegar la acción de UI al componente npcUI
        if (npcUI != null)
        {
            npcUI.MostrarBocadillo(texto, esFeedback);
        }
    }

    /// <summary>
    /// Oculta el bocadillo del NPC. Llamado típicamente por InteraccionJugador.cs.
    /// </summary>
    public void OcultarBocadillo()
    {
        // Delegar la acción de UI al componente npcUI
        if (npcUI != null)
        {
            npcUI.OcultarBocadillo();
        }
    }

    // ------------------------------------------------------------------
    // Lógica de Estados
    // ------------------------------------------------------------------

    void OnLlegarAVentana()
    {
        Debug.Log($"{gameObject.name} llegó a la ventana.");
        estadoActual = EstadoNPC.EsperandoAtencion;
        tiempoRestanteEsperaAtencion = tiempoMaximoEsperaAtencion;

        // Mostrar mensaje de interacción ([E])
        MostrarBocadillo("[E]");

        // Inicializar timer
        // NOTA: npcUI.TextoTemporizador es una propiedad Getter/Setter, acceder directamente
        if (npcUI.TextoTemporizador != null) npcUI.TextoTemporizador.gameObject.SetActive(true);
    }

    void OnLlegarASalida()
    {
        Debug.Log($"{gameObject.name} llegó a la salida. Destruyendo...");
        estadoActual = EstadoNPC.Inactivo;
        if (gestor != null) gestor.DespawnComprador(gameObject); // Dejar que el gestor lo destruya
        else Destroy(gameObject);
    }

    void ManejarEsperaAtencion()
    {
        tiempoRestanteEsperaAtencion -= Time.deltaTime;

        npcMovimiento.IntentarGirarHaciaVentana();

        if (tiempoRestanteEsperaAtencion <= 0)
        {
            GiveFeedback(mensajeTiempoEsperaAgotado, sonidoTiempoAgotado);
            Irse();
            return;
        }

        // Actualizar temporizador
        if (npcUI.TextoTemporizador != null && npcUI.TextoTemporizador.gameObject.activeSelf)
        {
            npcUI.TextoTemporizador.text = "Atención: " + Mathf.CeilToInt(tiempoRestanteEsperaAtencion).ToString();
        }
    }

    void ManejarEsperaEntrega()
    {
        tiempoRestanteEspera -= Time.deltaTime;

        // Si el bocadillo fue ocultado por el feedback, restaurar el pedido original.
        // Se usa el método público para asegurar que el componente npcUI lo maneje.
        if (!npcUI.EsBocadilloVisible() || npcUI.ObtenerTextoActual() == "[E]")
        {
            if (pedidoActual == null) SolicitarPocion();
            MostrarBocadillo(ObtenerTextoOriginalPedido());
        }

        npcMovimiento.IntentarGirarHaciaVentana();

        if (tiempoRestanteEspera <= 0)
        {
            GiveFeedback(mensajeTiempoAgotado, sonidoTiempoAgotado);
            Irse();
            return;
        }

        // Actualizar temporizador de entrega
        if (npcUI.TextoTemporizador != null && npcUI.TextoTemporizador.gameObject.activeSelf)
        {
            npcUI.TextoTemporizador.text = "Entrega: " + Mathf.CeilToInt(tiempoRestanteEspera).ToString();
        }
    }


    // ------------------------------------------------------------------
    // Métodos de Interacción y Entrega
    // ------------------------------------------------------------------

    /// <summary>
    /// MÉTODO DE INTERACCIÓN PRINCIPAL: Comprueba si el nombre del ítem entregado
    /// coincide con el pedido del NPC.
    /// </summary>
    public bool IntentarEntregarPocionPorNombre(string nombrePocionEntregada)
    {
        if (estadoActual != EstadoNPC.EnVentanaEsperando || pedidoActual == null)
        {
            GiveFeedback("No te he pedido nada...");
            return false;
        }

        string nombreEsperado = pedidoActual.nombreResultadoPocion.Trim().ToLower();
        string nombreEntregado = nombrePocionEntregada.Trim().ToLower();

        if (nombreEntregado == nombreEsperado)
        {
            LlamadaExito();
            return true;
        }
        else
        {
            LlamadaFallo(nombreEsperado);
            return false;
        }
    }

    void LlamadaExito()
    {
        GiveFeedback(mensajeFeedbackCorrecto, sonidoPocionCorrecta);

        if (GestorJuego.Instance != null)
        {
            int recompensaFinal = Mathf.Max(0, recompensaBase - (penalizacionPorError * intentosFallidos));
            GestorJuego.Instance.AnadirDinero(recompensaFinal);
        }
        Irse();
    }

    void LlamadaFallo(string nombreEsperado)
    {
        intentosFallidos++;
        Debug.Log($"{gameObject.name}: Esperaba {nombreEsperado}. Intento fallido #{intentosFallidos}.");

        if (intentosFallidos >= 2)
        {
            GiveFeedback(mensajeSegundoFallo, sonidoPocionIncorrecta);
            Irse();
            return;
        }

        GiveFeedback(mensajeFeedbackIncorrecto, sonidoPocionIncorrecta);
        StartCoroutine(RestaurarPedidoDespuesDeFeedback());
    }

    /// <summary>
    /// MÉTODO DE INTERACCIÓN: Llama al NPC cuando el jugador presiona 'E' para iniciar la atención.
    /// </summary>
    public void IniciarPedidoYTimer()
    {
        if (estadoActual != EstadoNPC.EsperandoAtencion) return;

        estadoActual = EstadoNPC.EnVentanaEsperando;
        tiempoRestanteEsperaAtencion = 0;

        SolicitarPocion();
        tiempoRestanteEspera = tiempoMaximoEspera;

        if (mostrarBocadilloAlIniciar)
        {
            // Usar el método público para actualizar el bocadillo
            MostrarBocadillo(ObtenerTextoOriginalPedido());
        }
    }

    // ------------------------------------------------------------------
    // Lógica de Movimiento Pública
    // ------------------------------------------------------------------

    /// <summary>
    /// Inicia el movimiento del NPC hacia la ventana. Acepta la posición de salida también.
    /// </summary>
    public void IrAVentana(Vector3 posVentana, Vector3 posSalida)
    {
        if (estadoActual != EstadoNPC.Inactivo) return;

        npcMovimiento.destinoSalida = posSalida; // Asignar destino de salida al componente de movimiento
        npcMovimiento.IniciarMovimiento(posVentana);

        estadoActual = EstadoNPC.MoviendoAVentana;
        gameObject.SetActive(true);
    }

    public void Irse()
    {
        // Evitar múltiples llamadas o llamadas en estados incorrectos
        if (estadoActual == EstadoNPC.MoviendoASalida || estadoActual == EstadoNPC.Inactivo || coroutineRetrasarSalida != null || estadoActual == EstadoNPC.EsperandoParaSalir)
            return;

        // Cambiar estado para que el Update no siga procesando temporizadores
        estadoActual = EstadoNPC.EsperandoParaSalir;
        coroutineRetrasarSalida = StartCoroutine(RetrasarSalidaCoroutine());
    }

    IEnumerator RetrasarSalidaCoroutine()
    {
        // Asegurarse de que el feedback visual haya terminado
        yield return new WaitForSeconds(npcUI.duracionFeedback);
        coroutineRetrasarSalida = null;

        if (estadoActual == EstadoNPC.EsperandoParaSalir)
        {
            if (gestor != null)
            {
                gestor.NPCTermino(this);
            }
            IniciarMovimientoHaciaSalida();
        }
    }

    void IniciarMovimientoHaciaSalida()
    {
        // Usar el método público OcultarBocadillo()
        OcultarBocadillo();

        if (npcMovimiento.destinoSalida != Vector3.zero)
        {
            npcMovimiento.IrseHaciaSalida(); // Delegar el movimiento al componente
            estadoActual = EstadoNPC.MoviendoASalida;
        }
        else
        {
            Debug.LogError("Destino de salida no asignado. Se autodestruye el NPC.");
            estadoActual = EstadoNPC.Inactivo;
            Destroy(gameObject);
        }
    }

    // ------------------------------------------------------------------
    // Métodos de Pedido y Diálogo
    // ------------------------------------------------------------------

    void SolicitarPocion()
    {
        if (estadoActual != EstadoNPC.EnVentanaEsperando)
        {
            return;
        }

        // Lógica para elegir la lista de pedidos (Mantenida)
        List<PedidoPocionData> listaAUsar = usarListaEspecifica && listaPedidosEspecificos?.Count > 0
                                                 ? listaPedidosEspecificos
                                                 : (pedidosPosibles?.Count > 0
                                                         ? pedidosPosibles
                                                         : (gestor?.listaMaestraPedidos?.Count > 0
                                                                 ? gestor.listaMaestraPedidos
                                                                 : null));

        if (listaAUsar == null || listaAUsar.Count == 0)
        {
            Debug.LogError($"NPC {gameObject.name} no pudo encontrar una lista de pedidos válida. ¡Se va!");
            GiveFeedback("¡No hay nada que pueda pedirte! ¡Me voy!");
            Irse();
            return;
        }

        pedidoActual = listaAUsar[UnityEngine.Random.Range(0, listaAUsar.Count)];
    }

    private string ObtenerTextoOriginalPedido()
    {
        if (pedidoActual == null) return "¿Necesitas algo?";

        // 1. Diálogo específico para este NPC/Receta
        var especifico = dialogosEspecificos?.FirstOrDefault(d => d != null && d.receta == pedidoActual);
        if (especifico != null && !string.IsNullOrEmpty(especifico.dialogoUnico))
            return especifico.dialogoUnico;

        // 2. Diálogo de respaldo por nombre
        return GenerarDialogoRespaldoConEstilo(pedidoActual.nombreResultadoPocion);
    }

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
        yield return new WaitForSeconds(npcUI.duracionFeedback + 0.1f);

        if (estadoActual == EstadoNPC.EnVentanaEsperando && pedidoActual != null)
        {
            // Vuelve a mostrar el texto del pedido original
            MostrarBocadillo(ObtenerTextoOriginalPedido());
        }
    }

    // ------------------------------------------------------------------
    // Métodos de Feedback y Limpieza
    // ------------------------------------------------------------------

    void GiveFeedback(string message, AudioClip sound = null)
    {
        if (estadoActual == EstadoNPC.EnVentanaEsperando || estadoActual == EstadoNPC.EsperandoAtencion)
        {
            // Usar el método público para mostrar como feedback (duración corta)
            MostrarBocadillo(message, true);
        }

        // Asumo que tienes un GestorAudio con un Singleton
        if (GestorAudio.Instancia != null && sound != null) GestorAudio.Instancia.ReproducirSonido(sound);
    }

    void OnDestroy()
    {
        if (coroutineRetrasarSalida != null) StopCoroutine(coroutineRetrasarSalida);

        // Limpieza de la UI del bocadillo al ser destruido el NPC
        if (npcUI != null)
        {
            npcUI.OcultarBocadillo();
        }
    }

    // ------------------------------------------------------------------
    // Getters de Estado
    // ------------------------------------------------------------------
    public bool EstaEsperandoAtencion() => estadoActual == EstadoNPC.EsperandoAtencion;
    public bool EstaEsperandoEntrega() => estadoActual == EstadoNPC.EnVentanaEsperando;
}