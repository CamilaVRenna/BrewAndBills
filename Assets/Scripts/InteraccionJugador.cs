using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;

// NOTA IMPORTANTE: Se asume que InventoryManager, ItemCatalog, Caldero, etc. están definidos y accesibles.

// Se recomienda revisar este enum si ya no se usa "FrascoLleno" como tipo de item.
public enum TipoItem { Nada, Ingrediente, FrascoVacío /*, FrascoLleno (Opcional: eliminar si no se usa) */ }

[RequireComponent(typeof(AudioSource))]
public class InteraccionJugador : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public float distanciaInteraccion = 3.0f;
    public Camera camaraJugador;
    public LayerMask capaInteraccion;

    [Header("UI y Feedback")]
    public Image uiIconoItemSostenido;
    public TextMeshProUGUI uiNombreItemSostenido;
    public GameObject panelItemSostenido;
    public TextMeshProUGUI textoNotificacion;
    public float tiempoNotificacion = 2.5f;
    private float temporizadorNotificacion = 0f;
    public TextMeshProUGUI textoInventario;

    [Header("Sonidos")]
    public AudioClip sonidoRecogerItem;
    public AudioClip sonidoRecogerPocion;
    public AudioClip sonidoTirarItem;
    public AudioClip sonidoError;

    [Header("Referencias de Sistema")]
    public Transform puntoAnclajeItem3D;
    public CatalogoRecetas catalogoRecetas;
    public Material materialPocionDesconocida;
    public GestorCompradores gestorNPC;
    public ControladorLibroUI controladorLibroUI;

    private AudioSource audioSourceJugador;
    private bool tiendaAbierta = false;

    // --- Referencias de Objetos Mirados ---
    // NOTA: Estas referencias aún requieren que los scripts FuenteIngredientes y FuenteFrascos existan
    private FuenteIngredientes fuenteIngredientesMirada = null;
    private Caldero calderoMiradoActual = null;
    private FuenteFrascos fuenteFrascosMirada = null;
    private NPCComprador npcMiradoActual = null;
    private LibroRecetasInteractuable libroMiradoActual = null;
    private CamaInteractuable camaMiradaActual = null;
    private IngredienteRecolectable ingredienteRecolectableMirado = null;
    private GameObject cartelMiradoActual = null;
    private ObjetoRotatorioInteractivo objetoRotatorioActual = null;


    // ====================================================================================
    // ------------------------------------ START & UPDATE --------------------------------
    // ====================================================================================

    void Start()
    {
        audioSourceJugador = GetComponent<AudioSource>();
        if (panelItemSostenido != null) panelItemSostenido.SetActive(false);
        if (textoNotificacion != null) textoNotificacion.gameObject.SetActive(false);
    }

    void Update()
    {
        if (controladorLibroUI != null && controladorLibroUI.gameObject.activeInHierarchy) return;

        ManejarInteraccionMirada();
        ManejarEntradaAccion();
        ManejarNotificaciones();

        string selectedItem = InventoryManager.Instance.GetSelectedItem();
        bool hasItemSelected = !string.IsNullOrEmpty(selectedItem);

        if (Input.GetKeyDown(KeyCode.Q) && hasItemSelected) { SoltarItemPersistente(); }

        if (Input.GetKeyDown(KeyCode.Q) && fuenteIngredientesMirada != null && hasItemSelected)
        {
            IntentarDevolverIngrediente(selectedItem);
        }
    }

    // ====================================================================================
    // --------------------------------- MANEJO DE RAYCAST --------------------------------
    // ====================================================================================

    void ManejarInteraccionMirada()
    {
        RaycastHit hit;
        bool golpeoAlgo = Physics.Raycast(camaraJugador.transform.position, camaraJugador.transform.forward, out hit, distanciaInteraccion, capaInteraccion);
        GameObject objetoGolpeado = golpeoAlgo ? hit.collider.gameObject : null;

        // --- Limpieza de referencias si dejamos de mirar ---
        if (fuenteIngredientesMirada != null && (!golpeoAlgo || objetoGolpeado != fuenteIngredientesMirada.gameObject))
        {
            fuenteIngredientesMirada.OcultarInformacion();
            fuenteIngredientesMirada = null;
        }
        if (calderoMiradoActual != null && (!golpeoAlgo || objetoGolpeado != calderoMiradoActual.gameObject))
        {
            calderoMiradoActual = null;
        }
        if (fuenteFrascosMirada != null && (!golpeoAlgo || objetoGolpeado != fuenteFrascosMirada.gameObject))
        {
            fuenteFrascosMirada.OcultarInformacion();
            fuenteFrascosMirada = null;
        }
        if (npcMiradoActual != null && (!golpeoAlgo || objetoGolpeado.GetComponentInParent<NPCComprador>() != npcMiradoActual))
        {
            npcMiradoActual.OcultarBocadillo();
            npcMiradoActual = null;
        }
        if (libroMiradoActual != null && (!golpeoAlgo || objetoGolpeado != libroMiradoActual.gameObject))
        {
            libroMiradoActual.OcultarInformacion();
            libroMiradoActual = null;
        }

        if (camaMiradaActual != null && (!golpeoAlgo || objetoGolpeado != camaMiradaActual.gameObject))
        {
            camaMiradaActual.OcultarInformacion();
            camaMiradaActual = null;
        }

        if (ingredienteRecolectableMirado != null && (!golpeoAlgo || objetoGolpeado != ingredienteRecolectableMirado.gameObject))
        {
            ingredienteRecolectableMirado.OcultarInformacion();
            ingredienteRecolectableMirado = null;
        }
        if (cartelMiradoActual != null && (!golpeoAlgo || objetoGolpeado != cartelMiradoActual))
        {
            cartelMiradoActual = null;
        }

        // AÑADIDO: Limpieza de Objeto Rotatorio (Puerta)
        if (objetoRotatorioActual != null && (!golpeoAlgo || objetoGolpeado != objetoRotatorioActual.gameObject))
        {
            objetoRotatorioActual.OcultarInformacion();
            objetoRotatorioActual = null;
        }


        // --- Asignación de referencias si miramos algo nuevo ---
        if (golpeoAlgo)
        {
            bool yaLoMiraba = (fuenteIngredientesMirada != null && objetoGolpeado == fuenteIngredientesMirada.gameObject) ||
                                (calderoMiradoActual != null && objetoGolpeado == calderoMiradoActual.gameObject) ||
                                (fuenteFrascosMirada != null && objetoGolpeado == fuenteFrascosMirada.gameObject) ||
                                (npcMiradoActual != null && objetoGolpeado.GetComponentInParent<NPCComprador>() == npcMiradoActual) ||
                                (libroMiradoActual != null && objetoGolpeado == libroMiradoActual.gameObject) ||
                                (camaMiradaActual != null && objetoGolpeado == camaMiradaActual.gameObject) ||
                                // AÑADIDO: Check para la puerta rotatoria
                                (objetoRotatorioActual != null && objetoGolpeado == objetoRotatorioActual.gameObject);

            if (!yaLoMiraba)
            {
                // AÑADIDO: Detección de Objeto Rotatorio (Puerta) - PRIORIDAD ALTA
                if (objetoGolpeado.TryGetComponent(out ObjetoRotatorioInteractivo rotCtrl)) { objetoRotatorioActual = rotCtrl; rotCtrl.MostrarInformacion(); return; }

                if (objetoGolpeado.TryGetComponent(out FuenteIngredientes ingSrc)) { fuenteIngredientesMirada = ingSrc; ingSrc.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out FuenteFrascos fraSrc)) { fuenteFrascosMirada = fraSrc; fraSrc.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out Caldero caldSrc)) { calderoMiradoActual = caldSrc; return; }
                if (objetoGolpeado.GetComponentInParent<NPCComprador>() is NPCComprador npcCtrl)
                {
                    npcMiradoActual = npcCtrl;
                    if (npcMiradoActual.EstaEsperandoAtencion() || npcMiradoActual.EstaEsperandoEntrega())
                    {
                        npcMiradoActual.MostrarBocadillo("[E]", false);
                    }
                    return;
                }
                if (objetoGolpeado.TryGetComponent(out LibroRecetasInteractuable libroCtrl)) { libroMiradoActual = libroCtrl; libroCtrl.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out CamaInteractuable camaCtrl)) { camaMiradaActual = camaCtrl; camaCtrl.MostrarInformacion(); }
                if (objetoGolpeado.TryGetComponent(out IngredienteRecolectable ingRecCtrl))
                {
                    if (camaMiradaActual != null) { camaMiradaActual.OcultarInformacion(); camaMiradaActual = null; }
                    ingredienteRecolectableMirado = ingRecCtrl;
                    ingredienteRecolectableMirado.MostrarInformacion();
                    return;
                }
                if (objetoGolpeado.name == "cartel") { cartelMiradoActual = objetoGolpeado; return; }
            }
        }
    }

    // ====================================================================================
    // --------------------------------- ENTRADA Y ACCIONES -------------------------------
    // ====================================================================================

    void ManejarEntradaAccion()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        // AÑADIDO: Prioridad 1: Objeto Rotatorio (Puerta)
        if (objetoRotatorioActual != null) { objetoRotatorioActual.Interactuar(); return; }


        if (fuenteIngredientesMirada != null) InteractuarConFuenteIngredientes();
        else if (fuenteFrascosMirada != null) InteractuarConFuenteFrascos();
        else if (calderoMiradoActual != null) InteractuarConCaldero();
        else if (npcMiradoActual != null) InteractuarConNPC();
        else if (libroMiradoActual != null) InteractuarConLibro();
        else if (camaMiradaActual != null) InteractuarConCama();
        else if (ingredienteRecolectableMirado != null) InteractuarConIngredienteRecolectable();
        else if (cartelMiradoActual != null) InteractuarConCartel();
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: NPC (VENDER POCIÓN) 💰
    // ------------------------------------------------------------------------------------
    void InteractuarConNPC()
    {
        string selectedItem = InventoryManager.Instance.GetSelectedItem();

        if (npcMiradoActual.EstaEsperandoAtencion())
        {
            npcMiradoActual.IniciarPedidoYTimer();
            MostrarNotificacion("El cliente ha realizado un pedido. ¡Atiéndele pronto!", 3f, false);
        }
        else if (npcMiradoActual.EstaEsperandoEntrega())
        {
            if (string.IsNullOrEmpty(selectedItem))
            {
                MostrarNotificacion("Selecciona la poción para entregar en tu inventario.", -1f, true);
                return;
            }

            // Asumiendo que cualquier ítem cuyo nombre contenga "pocion" o "frascofallido" es una poción.
            bool esPocion = selectedItem.ToLower().Contains("pocion") || selectedItem.ToLower().Contains("frascofallido");

            if (esPocion)
            {
                bool entregaExitosa = npcMiradoActual.IntentarEntregarPocionPorNombre(selectedItem);

                if (entregaExitosa)
                {
                    // La poción fue la correcta, se remueve del inventario.
                    InventoryManager.Instance.RemoveItem(selectedItem, 1);
                    MostrarNotificacion($"¡Entregaste la poción '{selectedItem}' al cliente!", 2f, false);
                    ReproducirSonidoJugador(sonidoRecogerPocion);
                    InventoryManager.Instance.SetSelectedIndex(InventoryManager.Instance.GetSelectedIndex());
                    return;
                }
                else
                {
                    MostrarNotificacion("Esa no es la poción que pedí...", -1f, true);
                }
            }
            else
            {
                MostrarNotificacion("Solo se puede entregar una poción al cliente.", -1f, true);
            }
        }
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: CALDERO (COCINAR / RECOGER) 🧪
    // ------------------------------------------------------------------------------------

    void InteractuarConCaldero()
    {
        string nombreItemSeleccionado = InventoryManager.Instance.GetSelectedItem();

        // 🌟 CONSTANTE ESTANDARIZADA DEL FRASCO VACÍO. Debe coincidir EXCACTAMENTE con ItemCatalog.
        const string NOMBRE_FRASCO_VACIO_ESTANDAR = "Frasco Vacío"; // <<< ASUME ESTE ES EL NOMBRE CORRECTO EN ITEMCATALOG

        // Obtenemos la data del ítem (puede ser null si no hay nada seleccionado)
        var itemData = InventoryManager.Instance.GetCatalog().GetItemData(nombreItemSeleccionado);


        // --- 1. RECOGER POCIÓN (SI ESTÁ LISTA) ---
        if (calderoMiradoActual.estadoActual == Caldero.EstadoCaldero.PocionLista)
        {
            // Normalizamos las cadenas para una comparación robusta (ignorando espacios o mayúsculas)
            string itemSeleccionadoNormalizado = nombreItemSeleccionado?.Trim().ToLower() ?? string.Empty;
            string frascoVacioNormalizado = NOMBRE_FRASCO_VACIO_ESTANDAR.Trim().ToLower();

            // 🌟 CORRECCIÓN DE LÓGICA: Se usa la comparación robusta.
            if (itemSeleccionadoNormalizado == frascoVacioNormalizado)
            {
                // 1. El caldero produce y añade la poción final al inventario.
                calderoMiradoActual.RecogerPocionYReiniciar();

                // 2. El inventario consume el Frasco Vacío que estaba seleccionado.
                // Usamos la constante limpia para la remoción.
                InventoryManager.Instance.RemoveItem(NOMBRE_FRASCO_VACIO_ESTANDAR, 1);

                MostrarNotificacion($"Poción embotellada y añadida al inventario.", 2f, false);
                ReproducirSonidoJugador(sonidoRecogerPocion);
                return;
            }
            else if (string.IsNullOrEmpty(nombreItemSeleccionado))
            {
                MostrarNotificacion("Selecciona un Frasco Vacío para embotellar la poción.", 2f, true);
            }
            else
            {
                MostrarNotificacion($"No puedes usar '{nombreItemSeleccionado}' para recoger la poción. Necesitas un Frasco Vacío.", 2f, true);
            }
            return;
        }

        // --- 2. INICIAR REMOVIDO (SI ESTÁ LISTO) ---
        if (calderoMiradoActual.estadoActual == Caldero.EstadoCaldero.ListoParaRemover)
        {
            calderoMiradoActual.IntentarIniciarRemovido();
            return;
        }

        // --- 3. AGREGAR INGREDIENTE DESDE INVENTARIO ---
        if (calderoMiradoActual.estadoActual == Caldero.EstadoCaldero.Ocioso || calderoMiradoActual.estadoActual == Caldero.EstadoCaldero.ListoParaRemover)
        {
            if (itemData == null)
            {
                MostrarNotificacion("Selecciona un ingrediente en tu inventario para añadir.", 2f, true);
                return;
            }

            // Validación mejorada: Asume que si no es FrascoVacio y no es Pocion/FrascoFallido, es un ingrediente.
            bool esPotencialIngrediente = itemData.nombreItem != NOMBRE_FRASCO_VACIO_ESTANDAR
                                             && !itemData.nombreItem.ToLower().Contains("pocion")
                                             && !itemData.nombreItem.ToLower().Contains("frascofallido");

            if (!esPotencialIngrediente)
            {
                MostrarNotificacion($"'{nombreItemSeleccionado}' no es un ingrediente válido para el caldero.", 2f, true);
                ReproducirSonidoJugador(sonidoError);
                return;
            }

            // Se pasa el ItemData al caldero
            // Nota: Se asume que 'AgregarIngrediente' del Caldero acepta ItemCatalog.ItemData
            bool agregado = calderoMiradoActual.AgregarIngrediente(itemData);

            if (agregado)
            {
                // El inventario consume el ítem solo si la adición fue exitosa
                InventoryManager.Instance.RemoveItem(itemData.nombreItem, 1);
                MostrarNotificacion($"Añadido {itemData.nombreItem} al caldero.", 1.5f, false);
                InventoryManager.Instance.SetSelectedIndex(InventoryManager.Instance.GetSelectedIndex());
                return;
            }
            else
            {
                // Mensaje de fallo si el caldero no lo acepta (ej. capacidad máxima o ingrediente duplicado)
                MostrarNotificacion("El caldero no puede aceptar ese ingrediente ahora.", 2f, true);
                return;
            }
        }

        // --- 4. OTROS ESTADOS (Fallido, Removiendo) ---
        if (calderoMiradoActual.estadoActual == Caldero.EstadoCaldero.RemovidoFallido)
        {
            calderoMiradoActual.ReiniciarCaldero();
            MostrarNotificacion("Caldero limpiado.", 2f, false);
        }
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: FUENTES DE INGREDIENTES 🥬
    // ------------------------------------------------------------------------------------

    void InteractuarConFuenteIngredientes()
    {
        if (InventoryManager.Instance == null) return;

        // 🛑 LÍNEA CORREGIDA (377): Cambiar el tipo de retorno a ItemCatalog.ItemData
        ItemCatalog.ItemData r = fuenteIngredientesMirada.IntentarRecoger();

        if (r != null)
        {
            // Usamos la propiedad .nombreItem del nuevo ItemData.
            InventoryManager.Instance.AddItem(r.nombreItem);

            ReproducirSonidoJugador(sonidoRecogerItem);
            int idx = InventoryManager.Instance.items.FindIndex(i =>
                i != null && i.nombre == r.nombreItem
            );
            // 🛑 LÍNEA CORREGIDA (395): Usamos la propiedad .nombreItem del nuevo ItemData.
            MostrarNotificacion($"Recogiste 1 {r.nombreItem}. (Slot {idx + 1} seleccionado)", 1.5f, false);
            return;
        }
        else
        {
            // 🛑 LÍNEA CORREGIDA: Acceder a la nueva propiedad pública 'claveIngrediente'
            // Luego, obtenemos el ItemData para el nombre legible.
            ItemCatalog.ItemData datosParaMostrar = GestorJuego.Instance.catalogoMaestro.GetItemData(fuenteIngredientesMirada.claveIngrediente);
            string nombreAMostrar = datosParaMostrar != null ? datosParaMostrar.nombreItem : "ingrediente";

            MostrarNotificacion($"¡No quedan más {nombreAMostrar}!", 2f, true);
        }
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: FUENTES DE FRASCOS 🥛
    // ------------------------------------------------------------------------------------

    void InteractuarConFuenteFrascos()
    {
        if (InventoryManager.Instance == null) return;

        // 🌟 CORRECCIÓN APLICADA: Ahora recibe un STRING (el nombre del ítem)
        string nombreFrascoRecogido = fuenteFrascosMirada.IntentarRecoger();

        if (nombreFrascoRecogido != null)
        {
            // El AddItem se debe haber llamado dentro de IntentarRecoger, 
            // si no lo hiciste, descomenta la línea de abajo:
            // InventoryManager.Instance.AddItem(nombreFrascoRecogido);

            ReproducirSonidoJugador(sonidoRecogerItem);
            Debug.Log($"Interactuando con fuente: {nombreFrascoRecogido}");
        }
        else
        {
            // Se usa el nombre del ítem estandarizado para la notificación (asumiendo que está en el script FuenteFrascos ahora)
            // Si el nombre aún no está disponible como string en FuenteFrascos (depende de cómo lo refactorizaste):
            MostrarNotificacion($"¡No quedan más frascos!", -1f, true);
        }
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: LIBRO DE RECETAS 📖
    // ------------------------------------------------------------------------------------

    void InteractuarConLibro()
    {
        if (controladorLibroUI != null)
        {
            controladorLibroUI.AbrirLibro();
            // Limpia referencias para evitar interacciones accidentales mientras el libro está abierto.
            fuenteIngredientesMirada = null; calderoMiradoActual = null; fuenteFrascosMirada = null; npcMiradoActual = null;
        }
        else { Debug.LogError("ControladorLibroUI no asignado!"); MostrarNotificacion("Error al abrir libro.", -1f, true); }
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: CAMA 🛌
    // ------------------------------------------------------------------------------------

    void InteractuarConCama()
    {
        // Sugerencia: Si GestorJuego.Instance existe y es un Singleton del componente, se podría simplificar el acceso
        if (GestorJuego.Instance != null && GestorJuego.Instance.GetComponent<GestorJuego>() != null)
        {
            var gestorJuego = GestorJuego.Instance.GetComponent<GestorJuego>();

            // Asumiendo que 'horaActual' es un enum o string en GestorJuego.
            if (gestorJuego.horaActual.ToString().ToLower() == "noche")
            {
                gestorJuego.IrADormir();

                GameObject cartel = GameObject.Find("cartel");
                if (cartel != null)
                    cartel.SetActive(true);
            }
            else MostrarNotificacion("Solo puedes dormir por la noche...", 2f, false);
        }
        else Debug.LogError("No se encontró GestorJuego para intentar dormir.");
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: INGREDIENTE RECOLECTABLE (MUNDO) ✨ (CORREGIDO)
    // ------------------------------------------------------------------------------------

    void InteractuarConIngredienteRecolectable()
    {
        string selectedItem = InventoryManager.Instance.GetSelectedItem();
        bool hasItemSelected = !string.IsNullOrEmpty(selectedItem);

        if (ingredienteRecolectableMirado == null) return;

        // PASO CLAVE: OBTENER EL ITEM DATA USANDO LA CLAVE (STRING)
        string claveIngrediente = ingredienteRecolectableMirado.claveIngrediente;
        ItemCatalog.ItemData ingData = GestorJuego.Instance.catalogoMaestro.GetItemData(claveIngrediente);

        if (ingData == null)
        {
            MostrarNotificacion($"Error: La clave '{claveIngrediente}' no existe en el catálogo.", 2f, true);
            return;
        }


        // Lógica especial para Miel (se mantiene)
        // REEMPLAZADO: ingredienteRecolectableMirado.datosIngrediente.nombreIngrediente -> ingData.nombreItem
        if (ingData.nombreItem.ToLower() == "miel"
            && InventoryManager.Instance != null
            && InventoryManager.Instance.HasItem("palita"))
        {
            ingredienteRecolectableMirado.Recolectar(); // Recolectar ya usa la clave internamente
            // REEMPLAZADO: ingredienteRecolectableMirado.datosIngrediente.nombreIngrediente -> ingData.nombreItem
            MostrarNotificacion($"Recolectado {ingData.nombreItem} (añadido a la tienda).", 2f, false);
            return;
        }

        // --- Intento de agregar al Caldero ---
        if (calderoMiradoActual != null)
        {
            // 1. PASAR EL TIPO CORRECTO (ItemCatalog.ItemData) al Caldero
            bool agregado = calderoMiradoActual.AgregarIngrediente(ingData);

            if (agregado)
            {
                MostrarNotificacion($"Agregado {ingData.nombreItem} al caldero.", 2f, false);
                ingredienteRecolectableMirado.Recolectar(); // Esto lo destruirá y aplicará cooldown
                return;
            }
            else
            {
                MostrarNotificacion("No se pudo agregar al caldero (lleno, o estado incorrecto).", 2f, true);
                return;
            }
        }

        // --- Lógica de Recolección Simple (si no hay caldero y no hay ítem seleccionado) ---
        if (!hasItemSelected)
        {
            ingredienteRecolectableMirado.Recolectar(); // Esto lo destruirá y aplicará cooldown
            // REEMPLAZADO: ingredienteRecolectableMirado.datosIngrediente.nombreIngrediente -> ingData.nombreItem
            MostrarNotificacion($"Recolectado {ingData.nombreItem} (añadido a la tienda).", 2f, false);
        }
        else
        {
            MostrarNotificacion("Tienes un ítem seleccionado. Deselecciona o usa un slot vacío.", 2f, true);
        }
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: CARTEL (ABRIR TIENDA) 🛒
    // ------------------------------------------------------------------------------------

    void InteractuarConCartel()
    {
        if (GestorJuego.Instance != null && GestorJuego.Instance.GetComponent<GestorJuego>().horaActual.ToString().ToLower() == "noche")
        {
            MostrarNotificacion("La tienda ya está cerrada por hoy. Vuelve mañana.", 2f, false);
            return;
        }

        if (!tiendaAbierta)
        {
            tiendaAbierta = true;
            if (GestorJuego.Instance != null && GestorJuego.Instance.gestorNPCs != null)
            {
                var gestorNPCs = GestorJuego.Instance.gestorNPCs;
                gestorNPCs.tiendaAbierta = true;

                if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("palita"))
                {
                    gestorNPCs.compradoresHabilitados = true;
                }
                else
                {
                    gestorNPCs.compradoresHabilitados = false;
                }
            }
            MostrarNotificacion("¡Tienda abierta! El día comienza...", 2f, false);

            if (cartelMiradoActual != null)
            {
                cartelMiradoActual.SetActive(false);
            }
        }
        else
        {
            MostrarNotificacion("La tienda ya está abierta.", 2f, false);
        }
    }

    // ------------------------------------------------------------------------------------
    // MÉTODO AUXILIAR: DEVOLVER INGREDIENTE 🔄
    // ------------------------------------------------------------------------------------

    void IntentarDevolverIngrediente(string selectedItem)
    {
        // 🛑 LÍNEA CORREGIDA (582): Acceder a la nueva propiedad pública 'claveIngrediente'
        string nombreIngredienteFuente = fuenteIngredientesMirada.claveIngrediente;

        if (selectedItem == nombreIngredienteFuente)
        {
            int cantidadJugador = InventoryManager.Instance.ContarItem(nombreIngredienteFuente);
            if (cantidadJugador > 0)
            {
                fuenteIngredientesMirada.DevolverIngrediente();
                InventoryManager.Instance.RemoveItem(nombreIngredienteFuente, 1);
                MostrarNotificacion($"Devuelto 1 {nombreIngredienteFuente} a la fuente.", 1.5f, false);
            }
            else
            {
                MostrarNotificacion($"No tienes {nombreIngredienteFuente} para devolver.", -1f, true);
            }
        }
    }

    // ------------------------------------------------------------------------------------
    // MÉTODO AUXILIAR: SOLTAR ÍTEM 🗑️
    // ------------------------------------------------------------------------------------

    void SoltarItemPersistente()
    {
        string selectedItem = InventoryManager.Instance.GetSelectedItem();
        if (string.IsNullOrEmpty(selectedItem)) return;

        ReproducirSonidoJugador(sonidoTirarItem);
        InventoryManager.Instance.RemoveItem(selectedItem, 1);
    }

    // ====================================================================================
    // ------------------------------- MÉTODOS AUXILIARES UI/SONIDO -----------------------
    // ====================================================================================

    public void MostrarNotificacion(string mensaje, float duracion = -1f, bool conSonidoError = false)
    {
        if (textoNotificacion != null)
        {
            textoNotificacion.text = mensaje;
            textoNotificacion.gameObject.SetActive(true);
            temporizadorNotificacion = (duracion > 0) ? duracion : tiempoNotificacion;
            if (conSonidoError) ReproducirSonidoJugador(sonidoError);
        }
    }

    void ManejarNotificaciones()
    {
        if (temporizadorNotificacion > 0)
        {
            temporizadorNotificacion -= Time.deltaTime;
            if (temporizadorNotificacion <= 0)
            {
                if (textoNotificacion != null) textoNotificacion.gameObject.SetActive(false);
            }
        }
    }

    void ReproducirSonidoJugador(AudioClip clip)
    {
        if (audioSourceJugador != null && clip != null)
            audioSourceJugador.PlayOneShot(clip);
    }
}