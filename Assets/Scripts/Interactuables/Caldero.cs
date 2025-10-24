using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Necesario para .OrderBy()
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class Caldero : MonoBehaviour
{
    // --- Configuración Básica, Referencias, Ajustes Minijuego, Cursor ---
    [Header("Configuración Básica")]
    public int maximoIngredientes = 5;
    // CRÍTICO: La lista de ingredientes ahora almacena NOMBRES (strings) para evitar errores de referencia SO.
    private List<string> nombresIngredientesActuales = new List<string>();
    public enum EstadoCaldero { Ocioso, ListoParaRemover, Removiendo, PocionLista, RemovidoFallido }
    public EstadoCaldero estadoActual = EstadoCaldero.Ocioso;
    private ControladorJugador controladorJugador;
    private InteraccionJugador interaccionJugador;

    [Header("Referencias de Gestión")]
    // ! DEBES ASIGNAR ESTOS EN EL INSPECTOR !
    public InventoryManager inventoryManager;

    [Tooltip("Referencia al catálogo centralizado de todos los ítems del juego. ¡Obligatorio!")]
    public ItemCatalog itemCatalog;

    // Mantenemos los campos FrascoVacioSO y PocionFallidaSO para obtener el NOMBRE, 
    // pero idealmente se usaría ItemCatalog en el futuro.
    public DatosFrasco FrascoVacioSO;
    public DatosFrasco PocionFallidaSO;

    [Tooltip("Cámara específica para la vista del minijuego. Obligatoria!")]
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

    // --- UI Minijuego ---
    [Header("UI Minijuego")]
    [Tooltip("Arrastra aquí la Imagen UI configurada como Radial 360 (la que se llena).")]
    public Image barraProgresoCircular;
    [Tooltip("Arrastra aquí el GameObject que actúa como FONDO de la barra de progreso.")]
    public GameObject fondoBarraProgreso;
    [Tooltip("Imagen UI que simula el cursor pegado a la cuchara. Desactivar Raycast Target!")]
    public Image cursorEnJuegoUI;

    // --- Sonidos, Recetas, Visual Caldero ---
    [Header("Sonidos Caldero")]
    public AudioClip sonidoAnadirIngrediente;
    public AudioClip sonidoRemoverBucle;
    public AudioClip sonidoPocionLista;
    public AudioClip sonidoPocionFallida;

    [Header("Recetas y Materiales")]
    public CatalogoRecetas catalogoRecetas;
    public Material materialPocionDesconocida;

    [Header("Configuración Visual Caldero")]
    public MeshRenderer rendererLiquidoCaldero;
    // ! RECUERDA: Este valor debe ser el índice del material del líquido dentro del MeshRenderer
    public int indiceMaterialLiquido = 2;
    public Material materialLiquidoVacio;

    // --- Variables Internas Minijuego ---
    private Vector2 centroPantallaMinijuego;
    private float anguloTotalRemovido = 0f;
    private Vector2 ultimaPosicionRaton;
    private bool botonRatonRemoverPresionado = false;
    private AudioSource audioSourceCaldero;

    void Start()
    {
        // Se asegura de encontrar la referencia de InteraccionJugador
        interaccionJugador = FindObjectOfType<InteraccionJugador>();
        controladorJugador = FindObjectOfType<ControladorJugador>();
        audioSourceCaldero = GetComponent<AudioSource>();

        // Desactivaciones iniciales y comprobaciones
        if (camaraMinijuego) camaraMinijuego.gameObject.SetActive(false); else Debug.LogError("CamaraMinijuego no asignada!", this.gameObject);
        if (objetoCuchara) objetoCuchara.SetActive(false); else Debug.LogError("ObjetoCuchara no asignado!", this.gameObject);
        if (barraProgresoCircular != null) barraProgresoCircular.gameObject.SetActive(false); else Debug.LogWarning("BarraProgresoCircular no asignada.", this.gameObject);
        if (fondoBarraProgreso != null) fondoBarraProgreso.SetActive(false); else Debug.LogWarning("FondoBarraProgreso no asignado.", this.gameObject);
        if (cursorEnJuegoUI != null) cursorEnJuegoUI.gameObject.SetActive(false); else Debug.LogWarning("CursorEnJuegoUI no asignada.", this.gameObject);
        if (audioSourceCaldero != null) audioSourceCaldero.loop = false;

        // Comprobaciones config
        if (objetoCuchara != null && pivoteRemover != null && objetoCuchara.transform.parent != pivoteRemover) { Debug.LogWarning("ADVERTENCIA: ObjetoCuchara NO es hijo de PivoteRemover.", this.gameObject); }
        if (objetoCuchara != null && objetoCuchara.GetComponent<Collider>() == null) { Debug.LogError("ERROR CRÍTICO! 'objetoCuchara' NO tiene Collider.", objetoCuchara); }
        if (pivoteRemover == null) { Debug.LogError("Falta asignar 'pivoteRemover'!", this.gameObject); }
        if (catalogoRecetas == null) { Debug.LogError("Falta asignar 'Catalogo Recetas'!", this.gameObject); }
        if (materialPocionDesconocida == null) { Debug.LogWarning("Material Pocion Desconocida no asignado."); }
        if (rendererLiquidoCaldero == null) { Debug.LogError("Falta asignar 'Renderer Liquido Caldero'!", this.gameObject); }
        if (inventoryManager == null) { Debug.LogError("Falta asignar 'InventoryManager'!", this.gameObject); }
        if (itemCatalog == null) { Debug.LogError("Falta asignar 'ItemCatalog'!", this.gameObject); }

        if (FrascoVacioSO == null) { Debug.LogWarning("FrascoVacioSO no asignado. No se consumirán frascos vacíos al recoger."); }
        if (PocionFallidaSO == null) { Debug.LogWarning("PocionFallidaSO no asignado. Se usará un nombre genérico si la receta no existe."); }
    }

    void Update()
    {
        // Cambia la tecla de E a R para iniciar el minijuego de remover (si está Listo)
        if (estadoActual == EstadoCaldero.Removiendo)
        {
            ManejarEntradaRemover();
        }
        else if (estadoActual == EstadoCaldero.ListoParaRemover && Input.GetKeyDown(KeyCode.R))
        {
            IntentarIniciarRemovido();
        }
    }

    // ====================================================================
    // 🔑 MÉTODOS PÚBLICOS DE INTERACCIÓN Y VALIDACIÓN
    // ====================================================================

    /// <summary>
    /// Retorna los ingredientes en el caldero como una lista de nombres.
    /// </summary>
    public string[] ObtenerContenidoNombres()
    {
        return nombresIngredientesActuales.ToArray();
    }

    /// <summary>
    /// **NUEVO:** Verifica si el ItemData puede ser añadido como ingrediente.
    /// Solo acepta items de tipo INGREDIENTE y si el caldero no está lleno o en estado de poción lista.
    /// </summary>
    public bool PuedeAnadirIngrediente(ItemCatalog.ItemData ingredienteData)
    {
        if (ingredienteData.tipoDeItem != ItemCatalog.TipoDeItem.INGREDIENTE)
        {
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("Solo se pueden añadir ingredientes al caldero.", -1f, true);
            return false;
        }

        if (estadoActual != EstadoCaldero.Ocioso && estadoActual != EstadoCaldero.ListoParaRemover)
        {
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("El caldero está ocupado o la poción ya está lista.", -1f, true);
            return false;
        }

        if (nombresIngredientesActuales.Count >= maximoIngredientes)
        {
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("El caldero está lleno.", -1f, true);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Añade un ingrediente al caldero, usando la data centralizada, después de la validación.
    /// </summary>
    public bool AnadirIngrediente(ItemCatalog.ItemData ingredienteData)
    {
        // Validar primero antes de añadir
        if (!PuedeAnadirIngrediente(ingredienteData))
        {
            return false;
        }

        // CRÍTICO: SOLO almacenamos el nombre del ítem (string).
        nombresIngredientesActuales.Add(ingredienteData.nombreItem);

        ReproducirSonidoCaldero(sonidoAnadirIngrediente);

        if (nombresIngredientesActuales.Count >= 2)
        {
            estadoActual = EstadoCaldero.ListoParaRemover;
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("Listo para remover! (R)");
        }
        return true;
    }

    /// <summary>
    /// **NUEVO:** Verifica si el ItemData puede usarse para recoger la poción.
    /// Solo acepta items de tipo FRASCO si la poción está lista.
    /// </summary>
    public bool PuedeRecogerConFrasco(ItemCatalog.ItemData frascoData)
    {
        // 1. ¿El caldero está listo?
        if (estadoActual != EstadoCaldero.PocionLista)
        {
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("No hay poción para embotellar. Necesitas remover.", -1f, true);
            return false;
        }

        // 2. ¿El ítem que sostiene el jugador es un FRASCO?
        if (frascoData.tipoDeItem != ItemCatalog.TipoDeItem.FRASCO)
        {
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("Necesitas un frasco vacío (tipo FRASCO) para recoger la poción.", -1f, true);
            return false;
        }

        // 3. ¿El ítem es el FRASCO VACÍO específico que debe consumirse?
        string nombreFrascoVacioRequerido = FrascoVacioSO != null ? FrascoVacioSO.nombreItem : "FrascoVacio";
        if (frascoData.nombreItem != nombreFrascoVacioRequerido)
        {
            if (interaccionJugador) interaccionJugador.MostrarNotificacion($"Ese no es el frasco correcto. Necesitas: {nombreFrascoVacioRequerido}.", -1f, true);
            return false;
        }

        return true;
    }

    // Alias para InteraccionJugador
    public bool AgregarIngrediente(ItemCatalog.ItemData ingredienteData)
    {
        return AnadirIngrediente(ingredienteData);
    }

    /// <summary>
    /// Gestiona la transacción de inventario y reinicia el caldero.
    /// Esta función ahora asume que el jugador YA fue validado con un frasco.
    /// </summary>
    public void RecogerPocionYReiniciar()
    {
        // NOTA: La validación de estado (PocionLista) y la posesión/tipo del frasco
        // deberían realizarse *antes* de llamar a esta función, idealmente en InteraccionJugador.

        if (itemCatalog == null)
        {
            Debug.LogError("RecogerPocionYReiniciar falló: ItemCatalog es nulo.");
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("Error de catálogo: Poción no existe.", -1f, true);
            return;
        }

        // 1. Obtener nombres requeridos.
        string nombreFrascoVacio = FrascoVacioSO != null ? FrascoVacioSO.nombreItem : "FrascoVacio";
        string nombreResultadoPocion;
        PedidoPocionData recetaEncontrada = null;

        if (catalogoRecetas != null)
        {
            // 🛑 CRÍTICO: Ordenar la lista de ingredientes para que el CatalogoRecetas pueda
            // encontrar la receta sin importar el orden en que se añadieron.
            List<string> ingredientesOrdenados = nombresIngredientesActuales.OrderBy(n => n).ToList();

            recetaEncontrada = catalogoRecetas.BuscarRecetaPorNombres(ingredientesOrdenados);
        }

        if (recetaEncontrada != null)
        {
            nombreResultadoPocion = recetaEncontrada.nombreResultadoPocion;
        }
        else
        {
            nombreResultadoPocion = PocionFallidaSO != null ? PocionFallidaSO.nombreItem : "PocionFallida";
        }

        // 2. Obtener los datos completos de la poción final del Catálogo.
        ItemCatalog.ItemData pocionFinalData = itemCatalog.GetItemData(nombreResultadoPocion);

        if (pocionFinalData == null)
        {
            Debug.LogError($"[RecogerPocion] El ítem '{nombreResultadoPocion}' no existe en el ItemCatalog. No se puede añadir.");
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("Error de catálogo: Poción no existe.", -1f, true);
            return;
        }

        // --- TRANSACCIÓN DE INVENTARIO (Eliminar frasco, añadir poción) ---

        // A. Remover el frasco vacío del inventario.
        // Aquí asumimos que la validación PuedeRecogerConFrasco se hizo correctamente
        // y el jugador tiene el frasco vacío en el slot de interacción.
        inventoryManager.RemoveItem(nombreFrascoVacio, 1);

        // B. Añadir el ítem de la poción real (SO) al inventario.
        inventoryManager.AddItem(nombreResultadoPocion);

        Debug.Log($"Poción '{nombreResultadoPocion}' añadida al inventario. Frasco vacío '{nombreFrascoVacio}' consumido.");
        if (interaccionJugador) interaccionJugador.MostrarNotificacion($"Recogida: {nombreResultadoPocion}", 2.0f);

        // 3. Reiniciar el estado del caldero.
        ReiniciarCaldero();
    }


    public void ReiniciarCaldero()
    {
        // Se borran los nombres de string almacenados.
        nombresIngredientesActuales.Clear();
        estadoActual = EstadoCaldero.Ocioso;
        if (materialLiquidoVacio != null)
        {
            ActualizarMaterialLiquido(materialLiquidoVacio);
        }
        Debug.Log("Caldero reiniciado.");
        if (audioSourceCaldero != null && audioSourceCaldero.isPlaying && audioSourceCaldero.clip == sonidoRemoverBucle)
        {
            audioSourceCaldero.Stop();
            audioSourceCaldero.loop = false;
        }
    }

    // ====================================================================
    // LÓGICA DEL MINIJUEGO (Sin cambios)
    // ====================================================================

    public void IntentarIniciarRemovido()
    {
        if (estadoActual == EstadoCaldero.ListoParaRemover)
        {
            IniciarMinijuegoRemover();
        }
    }

    void IniciarMinijuegoRemover()
    {
        estadoActual = EstadoCaldero.Removiendo;
        Debug.Log("Iniciando minijuego de remover (CIRCULAR)...");

        if (controladorJugador != null) { controladorJugador.AlmacenarRotacionActual(); }

        anguloTotalRemovido = 0f;
        botonRatonRemoverPresionado = false;

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
        if (controladorJugador != null && controladorJugador.camaraJugador != null) controladorJugador.camaraJugador.gameObject.SetActive(true);

        if (audioSourceCaldero != null && sonidoRemoverBucle != null)
        {
            audioSourceCaldero.clip = sonidoRemoverBucle;
            audioSourceCaldero.loop = true;
            audioSourceCaldero.Play();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true; // El cursor de OS es visible al inicio

        if (texturaCursorMinijuego != null)
        {
            Cursor.SetCursor(texturaCursorMinijuego, hotspotCursor, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

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

    // --- ManejarEntradaRemover (CORREGIDO: No requiere agarrar, solo click y arrastre) ---
    void ManejarEntradaRemover()
    {
        // Usamos GetMouseButton(0) para verificar si el botón izquierdo está presionado
        botonRatonRemoverPresionado = Input.GetMouseButton(0);

        // Si se suelta el botón en cualquier momento
        if (Input.GetMouseButtonUp(0))
        {
            // Restaurar cursor
            Cursor.visible = true;
            if (texturaCursorMinijuego != null) Cursor.SetCursor(texturaCursorMinijuego, hotspotCursor, CursorMode.Auto); else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            if (cursorEnJuegoUI != null) { cursorEnJuegoUI.gameObject.SetActive(false); }
        }

        // Si el botón está presionado, procesar el movimiento
        if (botonRatonRemoverPresionado)
        {
            // Ocultar el cursor del sistema mientras se remueve
            Cursor.visible = false;

            // Si es el primer frame que se presiona, inicializar la posición
            if (Input.GetMouseButtonDown(0))
            {
                ultimaPosicionRaton = Input.mousePosition;
                return;
            }

            // --- Lógica de cálculo de ángulo y rotación ---
            Vector2 posActual = Input.mousePosition;
            Vector2 vAnterior = ultimaPosicionRaton - centroPantallaMinijuego;
            Vector2 vActual = posActual - centroPantallaMinijuego;

            float dSqrAct = vActual.sqrMagnitude;
            float rMinSqr = radioMinimoGiro * radioMinimoGiro;

            // Comprobaciones: Estás fuera del radio mínimo Y hubo movimiento?
            if (dSqrAct > rMinSqr && (posActual - ultimaPosicionRaton).sqrMagnitude > 0.1f)
            {
                if (vAnterior.sqrMagnitude > rMinSqr)
                {
                    float deltaAngulo = Vector2.SignedAngle(vAnterior, vActual) * sensibilidadGiro;

                    // Acumular progreso (valor absoluto)
                    anguloTotalRemovido -= Mathf.Abs(deltaAngulo);

                    // Rotación visual de la cuchara
                    if (pivoteRemover != null)
                    {
                        if (velocidadVisualCucharaFija <= 0)
                        {
                            pivoteRemover.transform.Rotate(Vector3.up, -deltaAngulo, Space.World);
                        }
                        else
                        {
                            pivoteRemover.transform.Rotate(Vector3.up, -velocidadVisualCucharaFija * Time.deltaTime, Space.World);
                        }
                    }

                    if (barraProgresoCircular != null) ActualizarBarraProgreso();
                    VerificarCompletadoRemover();
                }
            }
            ultimaPosicionRaton = posActual; // Actualizar para el siguiente frame
        }
    }

    void ActualizarBarraProgreso()
    {
        if (barraProgresoCircular != null)
        {
            float progreso = Mathf.Clamp01(Mathf.Abs(anguloTotalRemovido) / anguloObjetivoRemover);
            barraProgresoCircular.fillAmount = progreso;
        }
    }

    void VerificarCompletadoRemover()
    {
        if (estadoActual != EstadoCaldero.Removiendo) return;
        if (anguloTotalRemovido <= -anguloObjetivoRemover)
        {
            FinalizarMinijuegoRemover(true);
        }
    }

    // FinalizarMinijuegoRemover (Solo gestiona el resultado visual y el estado, NO el inventario)
    void FinalizarMinijuegoRemover(bool exito)
    {
        if (estadoActual != EstadoCaldero.Removiendo) return;

        // El estado puede ser PocionLista o RemovidoFallido
        estadoActual = exito ? EstadoCaldero.PocionLista : EstadoCaldero.RemovidoFallido;

        // Detener sonido, ocultar elementos y restaurar control/cámaras (igual que antes)
        if (audioSourceCaldero != null && audioSourceCaldero.isPlaying && audioSourceCaldero.clip == sonidoRemoverBucle) { audioSourceCaldero.Stop(); audioSourceCaldero.loop = false; }
        if (objetoCuchara != null) objetoCuchara.SetActive(false);
        if (barraProgresoCircular != null) { barraProgresoCircular.gameObject.SetActive(false); if (fondoBarraProgreso != null) fondoBarraProgreso.SetActive(false); }
        if (cursorEnJuegoUI != null) cursorEnJuegoUI.gameObject.SetActive(false);

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (camaraMinijuego != null) camaraMinijuego.gameObject.SetActive(false);
        if (controladorJugador != null && controladorJugador.camaraJugador != null) controladorJugador.camaraJugador.gameObject.SetActive(true);
        if (controladorJugador != null) { controladorJugador.RestaurarRotacionAlmacenada(); }
        if (controladorJugador != null) controladorJugador.HabilitarMovimiento(true);

        // Lógica post-minijuego
        if (exito)
        {
            Debug.Log("¡Poción lista! Buscando material visual...");
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("¡Poción lista! (E para recoger)");
            ReproducirSonidoCaldero(sonidoPocionLista);

            Material materialAAplicar = materialPocionDesconocida;
            PedidoPocionData recetaEncontrada = null;

            // 1. OBTENER Y ORDENAR LA LISTA DE INGREDIENTES
            // 🛑 CRÍTICO: Ordenar la lista de ingredientes para que el CatalogoRecetas pueda
            // encontrar la receta sin importar el orden en que se añadieron.
            List<string> ingredientesOrdenados = nombresIngredientesActuales.OrderBy(n => n).ToList();


            // 2. BUSCAR LA RECETA
            if (catalogoRecetas != null)
            {
                // CRÍTICO: Llama al método que busca por NOMBRES (strings).
                recetaEncontrada = catalogoRecetas.BuscarRecetaPorNombres(ingredientesOrdenados);

                if (recetaEncontrada != null && recetaEncontrada.materialResultado != null)
                {
                    materialAAplicar = recetaEncontrada.materialResultado;
                }
            }

            Debug.Log($"Caldero - Material final aplicado: {(materialAAplicar != null ? materialAAplicar.name : "NINGUNO")}");
            ActualizarMaterialLiquido(materialAAplicar);
        }
        else
        {
            // Fallo
            Debug.Log("¡Mezcla fallida!");
            if (interaccionJugador) interaccionJugador.MostrarNotificacion("¡Mezcla fallida!");
            ReproducirSonidoCaldero(sonidoPocionFallida);
            // Si falla, reiniciamos inmediatamente.
            ReiniciarCaldero();
        }
    }

    // ====================================================================
    // MÉTODOS AUXILIARES Y DE VISUALIZACIÓN
    // ====================================================================

    void ReproducirSonidoCaldero(AudioClip clip)
    {
        if (audioSourceCaldero != null && clip != null)
        {
            audioSourceCaldero.PlayOneShot(clip);
        }
    }

    public bool EstaPocionLista()
    {
        return estadoActual == EstadoCaldero.PocionLista;
    }

    /// <summary>
    /// Se añade una validación para evitar el error 'IndexOutOfRangeException'
    /// si `indiceMaterialLiquido` es mayor que la cantidad de materiales en el Renderer.
    /// </summary>
    public void ActualizarMaterialLiquido(Material nuevoMaterial)
    {
        if (rendererLiquidoCaldero == null) return;
        if (nuevoMaterial == null) return;

        Material[] mats = rendererLiquidoCaldero.materials;

        if (indiceMaterialLiquido >= 0 && indiceMaterialLiquido < mats.Length)
        {
            // Es buena práctica instanciar el material para no modificar el original
            mats[indiceMaterialLiquido] = Instantiate(nuevoMaterial);
            rendererLiquidoCaldero.materials = mats;
        }
        else
        {
            // Mensaje de error más claro.
            Debug.LogError($"Índice de material ({indiceMaterialLiquido}) fuera de rango. El MeshRenderer solo tiene {mats.Length} materiales. ¡Revisa tu asignación de 'indiceMaterialLiquido'!", this.gameObject);
        }
    }
}
