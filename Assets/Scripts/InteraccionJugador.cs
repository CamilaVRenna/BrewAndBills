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

    // ************************************************************************************
    // NUEVO: Referencia al objeto 3D que el jugador está sosteniendo en la mano.
    private GameObject item3DSostenido;
    // ************************************************************************************


    // --- Referencias de Objetos Mirados ---
    private FuenteIngredientes fuenteIngredientesMirada = null;
    private Caldero calderoMiradoActual = null;
    private FuenteFrascos fuenteFrascosMirada = null;
    private NPCComprador npcMiradoActual = null;
    private LibroRecetasInteractuable libroMiradoActual = null;
    private CamaInteractuable camaMiradaActual = null;
    private IngredienteRecolectable ingredienteRecolectableMirado = null;

    // REEMPLAZADO: Referencia para el cartel con Raycast
    private ControladorCartelTienda cartelTiendaMiradoActual = null;
    private ObjetoRotatorioInteractivo objetoRotatorioActual = null;


    // ====================================================================================
    // ------------------------------------ START & UPDATE --------------------------------
    // ====================================================================================

    void Start()
    {
        audioSourceJugador = GetComponent<AudioSource>();
        if (panelItemSostenido != null) panelItemSostenido.SetActive(false);
        if (textoNotificacion != null) textoNotificacion.gameObject.SetActive(false);
        // Se elimina la suscripción a OnInventoryChange
    }

    void Update()
    {
        if (controladorLibroUI != null && controladorLibroUI.gameObject.activeInHierarchy) return;

        ManejarInteraccionMirada();
        ManejarEntradaAccion();
        ManejarNotificaciones();
        // Se mantiene esta llamada para que la UI se actualice a través del flujo existente (si lo hay)
        ActualizarInformacionUI();

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
        #region LimpiezaDeObjetosMirados
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
        // NPCComprador: Se mantiene la corrección que hiciste en el código original
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

        // NUEVO: Limpieza del ControladorCartelTienda
        if (cartelTiendaMiradoActual != null && (!golpeoAlgo || objetoGolpeado != cartelTiendaMiradoActual.gameObject))
        {
            cartelTiendaMiradoActual.OcultarInformacion();
            cartelTiendaMiradoActual = null;
        }

        // Limpieza de Objeto Rotatorio (Puerta)
        if (objetoRotatorioActual != null && (!golpeoAlgo || objetoGolpeado != objetoRotatorioActual.gameObject))
        {
            objetoRotatorioActual.OcultarInformacion();
            objetoRotatorioActual = null;
        }
        #endregion

        // --- Asignación de referencias si miramos algo nuevo ---
        if (golpeoAlgo)
        {
            bool yaLoMiraba = (fuenteIngredientesMirada != null && objetoGolpeado == fuenteIngredientesMirada.gameObject) ||
                             (calderoMiradoActual != null && objetoGolpeado == calderoMiradoActual.gameObject) ||
                             (fuenteFrascosMirada != null && objetoGolpeado == fuenteFrascosMirada.gameObject) ||
                             (npcMiradoActual != null && objetoGolpeado.GetComponentInParent<NPCComprador>() == npcMiradoActual) ||
                             (libroMiradoActual != null && objetoGolpeado == libroMiradoActual.gameObject) ||
                             (camaMiradaActual != null && objetoGolpeado == camaMiradaActual.gameObject) ||
                             (objetoRotatorioActual != null && objetoGolpeado == objetoRotatorioActual.gameObject) ||
                             (cartelTiendaMiradoActual != null && objetoGolpeado == cartelTiendaMiradoActual.gameObject);

            if (!yaLoMiraba)
            {
                // PRIORIDAD 1: Cartel de Tienda (ControladorCartelTienda)
                if (objetoGolpeado.TryGetComponent(out ControladorCartelTienda cartelCtrl))
                {
                    cartelTiendaMiradoActual = cartelCtrl;
                    cartelCtrl.MostrarInformacion();
                    return;
                }

                // PRIORIDAD 2: Objeto Rotatorio (Puerta)
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
            }
        }
    }

    // ====================================================================================
    // --------------------------------- ENTRADA Y ACCIONES -------------------------------
    // ====================================================================================

    void ManejarEntradaAccion()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        // PRIORIDAD 1: Cartel de Tienda
        if (cartelTiendaMiradoActual != null) { cartelTiendaMiradoActual.Interactuar(); return; }

        // PRIORIDAD 2: Objeto Rotatorio (Puerta)
        if (objetoRotatorioActual != null) { objetoRotatorioActual.Interactuar(); return; }


        if (fuenteIngredientesMirada != null) InteractuarConFuenteIngredientes();
        else if (fuenteFrascosMirada != null) InteractuarConFuenteFrascos();
        else if (calderoMiradoActual != null) InteractuarConCaldero();
        else if (npcMiradoActual != null) InteractuarConNPC();
        else if (libroMiradoActual != null) InteractuarConLibro();
        else if (camaMiradaActual != null) InteractuarConCama();
        else if (ingredienteRecolectableMirado != null) InteractuarConIngredienteRecolectable();
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: NPC (VENDER POCIÓN) 💰
    // ------------------------------------------------------------------------------------
    void InteractuarConNPC()
    {
        string selectedItem = InventoryManager.Instance.GetSelectedItem();
        // Obtener ItemData sin usar la propiedad 'tipoItem' eliminada.
        var itemData = InventoryManager.Instance.GetCatalog().GetItemData(selectedItem);

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

            // Se usa el check original (basado en nombre) para evitar el error CS1061 de 'tipoItem'.
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

            if (itemSeleccionadoNormalizado == frascoVacioNormalizado)
            {
                // 1. El caldero produce y añade la poción final al inventario.
                calderoMiradoActual.RecogerPocionYReiniciar();

                // 2. El inventario consume el Frasco Vacío que estaba seleccionado.
                InventoryManager.Instance.RemoveItem(NOMBRE_FRASCO_VACIO_ESTANDAR, 1);

                MostrarNotificacion($"Poción embotellada y añadida al inventario.", 2f, false);
                ReproducirSonidoJugador(sonidoRecogerPocion);
                InventoryManager.Instance.SetSelectedIndex(InventoryManager.Instance.GetSelectedIndex());
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

            // Se usa el check original (basado en nombre) para evitar el error CS1061 de 'tipoItem'.
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

        // Se asume que IntentarRecoger devuelve ItemCatalog.ItemData
        ItemCatalog.ItemData r = fuenteIngredientesMirada.IntentarRecoger();

        if (r != null)
        {
            // Usamos la propiedad .nombreItem del ItemData.
            InventoryManager.Instance.AddItem(r.nombreItem);

            ReproducirSonidoJugador(sonidoRecogerItem);
            int idx = InventoryManager.Instance.items.FindIndex(i =>
                i != null && i.nombre == r.nombreItem
            );
            // Se revierte el uso de SelectSlotContaining
            MostrarNotificacion($"Recogiste 1 {r.nombreItem}. (Slot {idx + 1} seleccionado)", 1.5f, false);
            return;
        }
        else
        {
            // Obtenemos el nombre para la UI usando la clave
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

        // Se asume que IntentarRecoger añade el ítem al inventario y devuelve el nombre.
        string nombreFrascoRecogido = fuenteFrascosMirada.IntentarRecoger();

        if (nombreFrascoRecogido != null)
        {
            ReproducirSonidoJugador(sonidoRecogerItem);
            MostrarNotificacion($"Recogiste 1 {nombreFrascoRecogido}.", 1.5f, false);
            // Se revierte el uso de SelectSlotContaining
        }
        else
        {
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
            }
            else MostrarNotificacion("Solo puedes dormir por la noche...", 2f, false);
        }
        else Debug.LogError("No se encontró GestorJuego para intentar dormir.");
    }

    // ------------------------------------------------------------------------------------
    // ACCIÓN: INGREDIENTE RECOLECTABLE (MUNDO) ✨
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
        // Se asume que el nombre del ítem es "Miel"
        if (ingData.nombreItem.ToLower() == "miel"
        && InventoryManager.Instance != null
        && InventoryManager.Instance.HasItem("palita"))
        {
            ingredienteRecolectableMirado.Recolectar(); // Recolectar ya usa la clave internamente
            MostrarNotificacion($"Recolectado {ingData.nombreItem} (añadido a la tienda).", 2f, false);
            return;
        }
        // Agregado check para indicar que necesita la palita
        else if (ingData.nombreItem.ToLower() == "miel" && !InventoryManager.Instance.HasItem("palita"))
        {
            MostrarNotificacion("Necesitas una palita para recoger la miel.", 2f, true);
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
            MostrarNotificacion($"Recolectado {ingData.nombreItem} (añadido a la tienda).", 2f, false);
        }
        else
        {
            MostrarNotificacion("Tienes un ítem seleccionado. Deselecciona o usa un slot vacío.", 2f, true);
        }
    }

    // ------------------------------------------------------------------------------------
    // MÉTODO AUXILIAR: DEVOLVER INGREDIENTE 🔄
    // ------------------------------------------------------------------------------------

    void IntentarDevolverIngrediente(string selectedItem)
    {
        string nombreIngredienteFuente = fuenteIngredientesMirada.claveIngrediente;

        if (selectedItem == nombreIngredienteFuente)
        {
            int cantidadJugador = InventoryManager.Instance.ContarItem(nombreIngredienteFuente);
            if (cantidadJugador > 0)
            {
                fuenteIngredientesMirada.DevolverIngrediente();
                InventoryManager.Instance.RemoveItem(nombreIngredienteFuente, 1);
                MostrarNotificacion($"Devuelto 1 {nombreIngredienteFuente} a la fuente.", 1.5f, false);
                // Actualizar el slot seleccionado (se mantiene la función original)
                InventoryManager.Instance.SetSelectedIndex(InventoryManager.Instance.GetSelectedIndex());
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

    // Se revierte la lógica a la versión original para evitar errores CS1061 de 'icono'.
    // Si tu InventoryManager tiene un evento o se llama desde otra parte, esta función se actualizará.
    void ActualizarInformacionUI()
    {
        string selectedItem = InventoryManager.Instance.GetSelectedItem();
        var itemData = InventoryManager.Instance.GetCatalog().GetItemData(selectedItem);

        if (string.IsNullOrEmpty(selectedItem) || itemData == null)
        {
            // Ocultar la UI 2D
            if (panelItemSostenido != null) panelItemSostenido.SetActive(false);
            if (textoInventario != null) textoInventario.text = "Ítem: Ninguno";

            // ************************************************************
            // LÓGICA DE LIMPIEZA 3D
            // ************************************************************
            if (item3DSostenido != null)
            {
                Destroy(item3DSostenido);
                item3DSostenido = null;
            }
            return;
        }

        // --- Lógica de UI 2D ---
        if (panelItemSostenido != null) panelItemSostenido.SetActive(true);
        if (uiNombreItemSostenido != null) uiNombreItemSostenido.text = selectedItem;

        // Se asume que el ItemData tiene una referencia al icono, si no la tiene, se mostrará solo el texto.
        if (uiIconoItemSostenido != null && itemData != null)
        {
            // Si 'icono' aún causa un error, este bloque debe ser comentado.
            // uiIconoItemSostenido.sprite = itemData.icono;
            uiIconoItemSostenido.color = Color.white;
        }

        // --- LÓGICA DE VISUALIZACIÓN DE ÍTEM 3D EN MANO ---
        // Asume que ItemData tiene un campo público 'prefab3D' de tipo GameObject
        // Revisa si hay un prefab 3D asociado y si no es el que ya tenemos instanciado.

        // ************************************************************
        // MODIFICACIÓN PRINCIPAL PARA VISUALIZACIÓN 3D
        // ************************************************************

        // 1. Limpiar el objeto 3D si el itemData.prefab3D es nulo, O si es un item diferente.
        // Se asume la existencia de 'itemData.prefab3D' como GameObject
        if (item3DSostenido != null)
        {
            // Comprobar si el item seleccionado *cambió* o si el nuevo item *no tiene* modelo 3D
            // Usamos itemData.nombreItem para comparar, asumiendo que el item3DSostenido se renombra al instanciar.
            bool itemCambio = item3DSostenido.name != itemData.nombreItem.Replace("(Clone)", "");
            bool prefabFalta = GetPrefab3D(itemData) == null;

            if (itemCambio || prefabFalta)
            {
                Destroy(item3DSostenido);
                item3DSostenido = null;
            }
        }

        // 2. Instanciar el nuevo objeto 3D si es necesario
        GameObject prefab3D = GetPrefab3D(itemData); // Método auxiliar para obtener el prefab.

        if (prefab3D != null && item3DSostenido == null && puntoAnclajeItem3D != null)
        {
            item3DSostenido = Instantiate(prefab3D, puntoAnclajeItem3D);
            // Opcional: ajustar la posición/rotación local si es necesario
            item3DSostenido.transform.localPosition = Vector3.zero;
            item3DSostenido.transform.localRotation = Quaternion.identity;

            // Renombrar para facilitar la comparación en el siguiente ciclo
            item3DSostenido.name = itemData.nombreItem;
        }
    }

    // Método auxiliar (necesario si prefab3D no es accesible directamente, o para evitar Try/Catch)
    // Deberías reemplazar 'ItemCatalog.ItemData' con el nombre de tu clase ItemData real.
    // **ADVERTENCIA:** Si tu ItemData no tiene 'prefab3D', tendrás que modificar la clase ItemData.
    private GameObject GetPrefab3D(ItemCatalog.ItemData data)
    {
        if (data == null) return null;

        // Intenta acceder al campo 'prefab3D' usando Reflection como una solución robusta 
        // si no quieres modificar ItemCatalog.ItemData, aunque lo ideal es que sea un campo público.
        // Si tienes el campo público 'public GameObject prefab3D;' en ItemCatalog.ItemData, 
        // cambia este método para que simplemente devuelva 'data.prefab3D;'

        try
        {
            var field = typeof(ItemCatalog.ItemData).GetField("prefab3D");
            if (field != null)
            {
                return field.GetValue(data) as GameObject;
            }
        }
        catch (System.Exception e)
        {
            // Debug.LogError($"Error al acceder a prefab3D en ItemData: {e.Message}");
        }

        // Fallback: Si el campo no se encuentra, devuelve null. 
        // Si tu prefab3D es público, reemplaza todo el try-catch con: return data.prefab3D;
        return null;
    }

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
