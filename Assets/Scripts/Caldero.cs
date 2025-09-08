using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class Caldero : MonoBehaviour
{
    // --- Configuracin Bsica, Referencias, Ajustes Minijuego, Cursor (igual) ---
    [Header("Configuracin Bsica")]
    public int maximoIngredientes = 5;
    public List<DatosIngrediente> ingredientesActuales = new List<DatosIngrediente>();
    public enum EstadoCaldero { Ocioso, ListoParaRemover, Removiendo, PocionLista, RemovidoFallido }
    public EstadoCaldero estadoActual = EstadoCaldero.Ocioso;
    private ControladorJugador controladorJugador;
    private InteraccionJugador interaccionJugador;
    [Tooltip("Cmara especfica para la vista del minijuego. Obligatoria!")]
    public Camera camaraMinijuego;
    [Tooltip("Objeto que contiene y rota la cuchara visualmente.")]
    public Transform pivoteRemover;
    [Tooltip("GameObject de la cuchara. Debe ser hijo de PivoteRemover y TENER UN COLLIDER.")]
    public GameObject objetoCuchara;
    [Header("Ajustes Minijuego Circular")]
    public float anguloObjetivoRemover = 1080f;
    public float radioMinimoGiro = 30f;
    public float sensibilidadGiro = 1.0f;
    public float velocidadVisualCucharaFija = 0f;
    [Header("Cursor Personalizado")]
    public Texture2D texturaCursorMinijuego;
    public Vector2 hotspotCursor = Vector2.zero;

    // --- UI Minijuego (Con Barra y Cursor Falso) ---
    [Header("UI Minijuego")] // Renombrado
    [Tooltip("Arrastra aqu la Imagen UI configurada como Radial 360 (la que se llena).")]
    public Image barraProgresoCircular;
    [Tooltip("Arrastra aqu el GameObject que acta como FONDO de la barra de progreso.")]
    public GameObject fondoBarraProgreso;
    [Tooltip("Imagen UI que simula el cursor pegado a la cuchara. Desactivar Raycast Target!")]
    public Image cursorEnJuegoUI; // Variable para el cursor falso

    // --- Sonidos, Recetas, Visual Caldero (igual) ---
    [Header("Sonidos Caldero")]
    public AudioClip sonidoAnadirIngrediente;
    public AudioClip sonidoRemoverBucle;
    public AudioClip sonidoPocionLista;
    public AudioClip sonidoPocionFallida;
    [Header("Recetas y Materiales")]
    public CatalogoRecetas catalogoRecetas;
    public Material materialPocionDesconocida;
    [Header("Configuracin Visual Caldero")]
    public MeshRenderer rendererLiquidoCaldero;
    public int indiceMaterialLiquido = 2;
    public Material materialLiquidoVacio;

    // --- Variables Internas Minijuego ---
    private Vector2 centroPantallaMinijuego;
    private float anguloTotalRemovido = 0f;
    private Vector2 ultimaPosicionRaton;
    private bool botonRatonRemoverPresionado = false;
    private bool cucharaAgarrada = false;
    private AudioSource audioSourceCaldero;
    private Vector3 offsetAgarreLocal; // <<--- NUEVO: Guarda dnde agarramos la cuchara (localmente)

    // --- NUEVO: Guardar la última poción creada ---
    private List<DatosIngrediente> ultimaPocionCreada = null;

    void Start()
    {
        controladorJugador = FindObjectOfType<ControladorJugador>();
        interaccionJugador = FindObjectOfType<InteraccionJugador>();
        audioSourceCaldero = GetComponent<AudioSource>();

        // Desactivaciones iniciales y comprobaciones
        if (camaraMinijuego) camaraMinijuego.gameObject.SetActive(false); else Debug.LogError("CamaraMinijuego no asignada!", this.gameObject);
        if (objetoCuchara) objetoCuchara.SetActive(false); else Debug.LogError("ObjetoCuchara no asignado!", this.gameObject);
        if (barraProgresoCircular != null) barraProgresoCircular.gameObject.SetActive(false); else Debug.LogWarning("BarraProgresoCircular no asignada.", this.gameObject);
        if (fondoBarraProgreso != null) fondoBarraProgreso.SetActive(false); else Debug.LogWarning("FondoBarraProgreso no asignado.", this.gameObject);
        if (cursorEnJuegoUI != null) cursorEnJuegoUI.gameObject.SetActive(false); else Debug.LogWarning("CursorEnJuegoUI no asignado.", this.gameObject); // Comprobacin
        if (audioSourceCaldero != null) audioSourceCaldero.loop = false;
        // Comprobaciones config
        if (objetoCuchara != null && pivoteRemover != null && objetoCuchara.transform.parent != pivoteRemover) { Debug.LogWarning("ADVERTENCIA: ObjetoCuchara NO es hijo de PivoteRemover.", this.gameObject); }
        if (objetoCuchara != null && objetoCuchara.GetComponent<Collider>() == null) { Debug.LogError("ERROR CRTICO! 'objetoCuchara' NO tiene Collider.", objetoCuchara); }
        if (pivoteRemover == null) { Debug.LogError("Falta asignar 'pivoteRemover'!", this.gameObject); }
        if (catalogoRecetas == null) { Debug.LogError("Falta asignar 'Catalogo Recetas'!", this.gameObject); }
        if (materialPocionDesconocida == null) { Debug.LogWarning("Material Pocion Desconocida no asignado."); }
        if (rendererLiquidoCaldero == null) { Debug.LogError("Falta asignar 'Renderer Liquido Caldero'!", this.gameObject); }

        // --- DEPURACION: Inicio del script ---
        Debug.Log("Caldero iniciado. Estado actual: " + estadoActual);
    }

    void Update()
    {
        // Cambia la tecla de E a R para iniciar el minijuego de remover
        if (estadoActual == EstadoCaldero.Removiendo) { ManejarEntradaRemover(); }
        else if (estadoActual == EstadoCaldero.ListoParaRemover && Input.GetKeyDown(KeyCode.R))
        {
            // --- DEPURACION: Intentando iniciar minijuego por tecla R ---
            Debug.Log("Tecla 'R' presionada. Intentando iniciar minijuego...");
            IntentarIniciarRemovido();
        }

        // --- DEPURACION: Comprobación continua del estado (cuidado con el spam de logs) ---
        // Descomenta la siguiente línea solo si es necesario para una depuración más detallada
        // Debug.Log("Estado actual del caldero: " + estadoActual + ", ingredientes: " + ingredientesActuales.Count);
    }

    public bool AnadirIngrediente(DatosIngrediente ingrediente)
    {
        // --- DEPURACION: Intentando agregar ingrediente ---
        Debug.Log("Intentando agregar ingrediente: " + (ingrediente?.nombreIngrediente ?? "null"));

        if (estadoActual != EstadoCaldero.Ocioso && estadoActual != EstadoCaldero.ListoParaRemover)
        {
            Debug.LogWarning("No se puede agregar ingrediente. El caldero no está ocioso o listo para remover. Estado: " + estadoActual);
            return false;
        }

        if (ingredientesActuales.Count >= maximoIngredientes)
        {
            Debug.LogWarning("Caldero lleno. No se puede agregar más ingredientes.");
            return false;
        }

        ingredientesActuales.Add(ingrediente);
        ReproducirSonidoCaldero(sonidoAnadirIngrediente);

        Debug.Log("Ingrediente '" + ingrediente.nombreIngrediente + "' agregado. Cantidad total: " + ingredientesActuales.Count);

        // Si se han agregado suficientes ingredientes, cambia el estado
        if (ingredientesActuales.Count >= 2)
        {
            estadoActual = EstadoCaldero.ListoParaRemover;
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("¡Listo para remover! (E)");
            // --- DEPURACION: Cambio de estado ---
            Debug.Log("Caldero ahora en estado: ListoParaRemover");
        }
        return true;
    }

    public void IntentarIniciarRemovido()
    {
        // --- DEPURACION: Llamada a IntentarIniciarRemovido() ---
        Debug.Log("Llamada a IntentarIniciarRemovido(). Estado del caldero: " + estadoActual);

        if (estadoActual == EstadoCaldero.ListoParaRemover)
        {
            Debug.Log("Estado OK. Iniciando minijuego de remoción...");
            IniciarMinijuegoRemover();
        }
        else
        {
            Debug.LogWarning("El caldero no está listo para remover. No se puede iniciar el minijuego.");
        }
    }

    void IniciarMinijuegoRemover()
    {
        estadoActual = EstadoCaldero.Removiendo;
        Debug.Log("Iniciando minijuego de remover (CIRCULAR)...");
        if (controladorJugador != null) { controladorJugador.AlmacenarRotacionActual(); }
        anguloTotalRemovido = 0f;
        botonRatonRemoverPresionado = false;
        cucharaAgarrada = false;
        if (pivoteRemover != null) { pivoteRemover.transform.localRotation = Quaternion.identity; }
        if (camaraMinijuego != null)
        {
            if (!camaraMinijuego.gameObject.activeSelf) camaraMinijuego.gameObject.SetActive(true);
            centroPantallaMinijuego = new Vector2(camaraMinijuego.pixelWidth / 2.0f, camaraMinijuego.pixelHeight / 2.0f);
        }
        else
        {
            centroPantallaMinijuego = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
        }
        if (controladorJugador != null) controladorJugador.HabilitarMovimiento(false);
        if (controladorJugador != null && controladorJugador.camaraJugador != null) controladorJugador.camaraJugador.gameObject.SetActive(false);
        if (audioSourceCaldero != null && sonidoRemoverBucle != null) { audioSourceCaldero.clip = sonidoRemoverBucle; audioSourceCaldero.loop = true; audioSourceCaldero.Play(); }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (texturaCursorMinijuego != null) { Cursor.SetCursor(texturaCursorMinijuego, hotspotCursor, CursorMode.Auto); } else { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); }
        if (cursorEnJuegoUI != null) cursorEnJuegoUI.gameObject.SetActive(false);
        if (objetoCuchara != null) objetoCuchara.SetActive(true);
        if (barraProgresoCircular != null)
        {
            barraProgresoCircular.fillAmount = 0;
            barraProgresoCircular.gameObject.SetActive(true);
            if (fondoBarraProgreso != null) fondoBarraProgreso.SetActive(true);
            ActualizarBarraProgreso();
        }
    }

    void ManejarEntradaRemover()
    {
        // --- Comprobar clic para AGARRAR ---
        if (Input.GetMouseButtonDown(0))
        {
            if (!cucharaAgarrada)
            {
                if (camaraMinijuego == null || objetoCuchara == null) return;
                Ray rayo = camaraMinijuego.ScreenPointToRay(Input.mousePosition); RaycastHit hit;
                if (Physics.Raycast(rayo, out hit, 100f))
                {
                    if (hit.collider.gameObject == objetoCuchara)
                    {
                        Debug.Log("Cuchara Agarrada!");
                        cucharaAgarrada = true; botonRatonRemoverPresionado = true; ultimaPosicionRaton = Input.mousePosition;

                        // --- CALCULAR Y GUARDAR OFFSET LOCAL --- <<<--- AADIDO
                        offsetAgarreLocal = objetoCuchara.transform.InverseTransformPoint(hit.point);
                        // -----------------------------------------

                        Cursor.visible = false; // Ocultar cursor sistema
                        if (cursorEnJuegoUI != null) { cursorEnJuegoUI.gameObject.SetActive(true); } // Mostrar cursor falso
                    }
                }
            }
            else { botonRatonRemoverPresionado = true; }
        }

        // --- Comprobar si se SUELTA ---
        if (Input.GetMouseButtonUp(0))
        {
            if (cucharaAgarrada)
            {
                Debug.Log("Cuchara Soltada.");
                Cursor.visible = true; // Mostrar cursor sistema
                if (cursorEnJuegoUI != null) { cursorEnJuegoUI.gameObject.SetActive(false); } // Ocultar cursor falso
                if (texturaCursorMinijuego != null) Cursor.SetCursor(texturaCursorMinijuego, hotspotCursor, CursorMode.Auto); else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            botonRatonRemoverPresionado = false;
            cucharaAgarrada = false;
        }

        // --- Procesar movimiento SI est agarrada Y presionando ---
        if (botonRatonRemoverPresionado && cucharaAgarrada)
        {
            // --- MOVER CURSOR FALSO AL PUNTO DE AGARRE EN PANTALLA --- <<<--- LGICA MODIFICADA
            if (cursorEnJuegoUI != null && camaraMinijuego != null && objetoCuchara != null)
            {
                Vector3 puntoAgarreActualMundo = objetoCuchara.transform.TransformPoint(offsetAgarreLocal);
                Vector2 posicionCursorPantalla = camaraMinijuego.WorldToScreenPoint(puntoAgarreActualMundo);
                cursorEnJuegoUI.rectTransform.position = posicionCursorPantalla; // Cursor UI sigue el punto de agarre
            }
            // ------------------------------------------------------------

            // --- Lgica de clculo de ngulo y rotacin (usa Input.mousePosition real) ---
            Vector2 posActual = Input.mousePosition; Vector2 vAnterior = ultimaPosicionRaton - centroPantallaMinijuego; Vector2 vActual = posActual - centroPantallaMinijuego;
            float dSqrAct = vActual.sqrMagnitude; float rMinSqr = radioMinimoGiro * radioMinimoGiro;
            if (dSqrAct > rMinSqr && (posActual - ultimaPosicionRaton).sqrMagnitude > 0.1f)
            {
                if (vAnterior.sqrMagnitude > rMinSqr)
                {
                    float deltaAngulo = Vector2.SignedAngle(vAnterior, vActual) * sensibilidadGiro;
                    if (deltaAngulo < -0.1f) { anguloTotalRemovido += deltaAngulo; }
                    if (pivoteRemover != null) { if (velocidadVisualCucharaFija <= 0) { pivoteRemover.transform.Rotate(Vector3.up, -deltaAngulo, Space.World); } else { pivoteRemover.transform.Rotate(Vector3.up, -velocidadVisualCucharaFija * Time.deltaTime, Space.World); } }
                    if (barraProgresoCircular != null) ActualizarBarraProgreso();
                    VerificarCompletadoRemover();
                }
            }
            ultimaPosicionRaton = posActual; // Actualizar para el siguiente frame
        }
    }

    void ActualizarBarraProgreso() { if (barraProgresoCircular != null) { float progreso = Mathf.Clamp01(Mathf.Abs(anguloTotalRemovido) / anguloObjetivoRemover); barraProgresoCircular.fillAmount = progreso; } }

    void VerificarCompletadoRemover()
    {
        if (estadoActual != EstadoCaldero.Removiendo) return;
        if (anguloTotalRemovido <= -anguloObjetivoRemover)
        {
            Debug.Log("Minijuego completado. Angulo alcanzado: " + anguloTotalRemovido);
            FinalizarMinijuegoRemover(true);
        }
    }

    void FinalizarMinijuegoRemover(bool exito)
    {
        if (estadoActual != EstadoCaldero.Removiendo) return;
        Debug.Log($"Minijuego terminado. Éxito: {exito}");
        estadoActual = exito ? EstadoCaldero.PocionLista : EstadoCaldero.RemovidoFallido;

        // Detener sonido, ocultar elementos (igual)
        if (audioSourceCaldero != null && audioSourceCaldero.isPlaying && audioSourceCaldero.clip == sonidoRemoverBucle) { audioSourceCaldero.Stop(); audioSourceCaldero.loop = false; }
        if (objetoCuchara != null) objetoCuchara.SetActive(false);
        // Ocultar UI minijuego
        if (barraProgresoCircular != null) { barraProgresoCircular.gameObject.SetActive(false); if (fondoBarraProgreso != null) fondoBarraProgreso.SetActive(false); }
        if (cursorEnJuegoUI != null) cursorEnJuegoUI.gameObject.SetActive(false);
        // Restaurar Cursor, Cmara, Jugador (igual)
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        if (camaraMinijuego != null) camaraMinijuego.gameObject.SetActive(false);
        if (controladorJugador != null && controladorJugador.camaraJugador != null) controladorJugador.camaraJugador.gameObject.SetActive(true);
        if (controladorJugador != null) { controladorJugador.RestaurarRotacionAlmacenada(); }
        if (controladorJugador != null) controladorJugador.HabilitarMovimiento(true);

        // Lgica post-minijuego
        if (exito)
        {
            Debug.Log("¡Poción lista!");
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("¡Poción lista! (E)");
            ReproducirSonidoCaldero(sonidoPocionLista);

            // --- LÓGICA ACTUALIZAR MATERIAL CALDERO CON LOGS ---
            Material materialAAplicar = materialPocionDesconocida;
            string nombreRecetaDebug = "Desconocida";
            PedidoPocionData recetaEncontrada = null; // Inicializar a null

            if (catalogoRecetas != null)
            {
                recetaEncontrada = catalogoRecetas.BuscarRecetaPorIngredientes(ingredientesActuales);
                Debug.Log("Buscando receta para " + ingredientesActuales.Count + " ingredientes...");

                if (recetaEncontrada != null)
                {
                    nombreRecetaDebug = recetaEncontrada.nombreResultadoPocion;
                    Debug.Log($"Caldero - Receta Encontrada: {nombreRecetaDebug}, Material Asignado: {(recetaEncontrada.materialResultado != null ? recetaEncontrada.materialResultado.name : "NINGUNO")}");
                    if (recetaEncontrada.materialResultado != null)
                    {
                        materialAAplicar = recetaEncontrada.materialResultado;
                    }
                    else
                    {
                        Debug.LogWarning($"Receta '{nombreRecetaDebug}' no tiene Material Resultado asignado en su asset.");
                    }
                }
                else
                {
                    Debug.Log("Caldero - No se encontró receta para esta combinación.");
                }

            }
            else { Debug.LogError("¡Catalogo de Recetas no asignado en Caldero!"); }

            Debug.Log($"Caldero - Material final a aplicar: {(materialAAplicar != null ? materialAAplicar.name : "NINGUNO (Usando Desconocido o Falló)")}");

            ActualizarMaterialLiquido(materialAAplicar);

            if (ingredientesActuales != null && ingredientesActuales.Count > 0)
            {
                ultimaPocionCreada = new List<DatosIngrediente>(ingredientesActuales);
                Debug.Log($"Poción creada y guardada. Contenido: {string.Join(", ", ultimaPocionCreada.Select(i => i.nombreIngrediente))}");
            }
            else
            {
                ultimaPocionCreada = null;
                Debug.Log("Poción creada pero sin ingredientes registrados. 'ultimaPocionCreada' es null.");
            }
        }
        else
        { // Fallo
            Debug.Log("¡Mezcla fallida!");
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("¡Mezcla fallida!");
            ReproducirSonidoCaldero(sonidoPocionFallida);
            ReiniciarCaldero();
            ultimaPocionCreada = null;
        }
    }

    void ReproducirSonidoCaldero(AudioClip clip) { if (audioSourceCaldero != null && clip != null) { audioSourceCaldero.PlayOneShot(clip); } }
    public bool EstaPocionLista() { return estadoActual == EstadoCaldero.PocionLista; }
    public DatosIngrediente[] RecogerPocion()
    {
        Debug.Log("Llamada a RecogerPocion(). Estado actual: " + estadoActual);
        if (estadoActual == EstadoCaldero.PocionLista)
        {
            Debug.Log("Poción lista. Entregando ingredientes y reiniciando caldero.");
            DatosIngrediente[] c = ingredientesActuales.ToArray();
            ReiniciarCaldero();
            ultimaPocionCreada = null;
            return c;
        }
        Debug.LogWarning("El caldero no tiene una poción lista para recoger. Estado: " + estadoActual);
        return null;
    }
    public void ReiniciarCaldero()
    {
        ingredientesActuales.Clear();
        estadoActual = EstadoCaldero.Ocioso;
        if (materialLiquidoVacio != null) { ActualizarMaterialLiquido(materialLiquidoVacio); }
        Debug.Log("Caldero reiniciado.");
        if (audioSourceCaldero != null && audioSourceCaldero.isPlaying && audioSourceCaldero.clip == sonidoRemoverBucle) { audioSourceCaldero.Stop(); audioSourceCaldero.loop = false; }
    }
    public void ActualizarMaterialLiquido(Material nuevoMaterial)
    {
        if (rendererLiquidoCaldero == null) return;
        if (nuevoMaterial == null) return;
        Material[] mats = rendererLiquidoCaldero.materials;
        if (indiceMaterialLiquido >= 0 && indiceMaterialLiquido < mats.Length)
        {
            mats[indiceMaterialLiquido] = Instantiate(nuevoMaterial);
            rendererLiquidoCaldero.materials = mats;
            Debug.Log("Material del caldero actualizado a: " + nuevoMaterial.name);
        }
        else
        {
            Debug.LogError($"ndice ({indiceMaterialLiquido}) fuera de rango ({mats.Length})", this.gameObject);
        }
    }

    public List<DatosIngrediente> ObtenerYConsumirUltimaPocion()
    {
        Debug.Log("Llamada a ObtenerYConsumirUltimaPocion()");
        if (ultimaPocionCreada == null || ultimaPocionCreada.Count == 0)
        {
            Debug.Log("No hay poción para entregar.");
            return null;
        }
        var resultado = new List<DatosIngrediente>(ultimaPocionCreada);
        ultimaPocionCreada = null;
        Debug.Log("Poción entregada. 'ultimaPocionCreada' limpiada.");
        return resultado;
    }

    public bool HayPocionListaParaEntregar()
    {
        return ultimaPocionCreada != null && ultimaPocionCreada.Count > 0;
    }

    public bool AgregarIngrediente(DatosIngrediente ingrediente)
    {
        return AnadirIngrediente(ingrediente);
    }
}