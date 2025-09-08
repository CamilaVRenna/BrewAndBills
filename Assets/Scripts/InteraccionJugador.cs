using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public enum TipoItem { Nada, Ingrediente, FrascoVacio, FrascoLleno }

[RequireComponent(typeof(AudioSource))]
public class InteraccionJugador : MonoBehaviour
{
    public float distanciaInteraccion = 3.0f;
    public Camera camaraJugador;
    public LayerMask capaInteraccion;

    public Image uiIconoItemSostenido;
    public TextMeshProUGUI uiNombreItemSostenido;
    public GameObject panelItemSostenido;

    public TextMeshProUGUI textoNotificacion;
    public float tiempoNotificacion = 2.5f;
    private float temporizadorNotificacion = 0f;

    public Transform puntoAnclajeItem3D;

    public AudioClip sonidoRecogerItem;
    public AudioClip sonidoRecogerPocion;
    public AudioClip sonidoTirarItem;
    public AudioClip sonidoError;

    private AudioSource audioSourceJugador;

    private ScriptableObject itemSostenido = null;
    private TipoItem tipoItemSostenido = TipoItem.Nada;
    public bool JugadorSostieneAlgo => tipoItemSostenido != TipoItem.Nada;
    private List<DatosIngrediente> contenidoFrascoLleno = null;
    private GameObject instanciaItem3D = null;

    private FuenteIngredientes fuenteIngredientesMirada = null;
    private Caldero calderoMiradoActual = null;
    private FuenteFrascos fuenteFrascosMirada = null;
    private NPCComprador npcMiradoActual = null;
    private LibroRecetasInteractuable libroMiradoActual = null;
    private PuertaCambioEscena puertaMiradaActual = null;
    private CamaInteractuable camaMiradaActual = null;
    private IngredienteRecolectable ingredienteRecolectableMirado = null;
    private GameObject cartelMiradoActual = null; // NUEVO

    public CatalogoRecetas catalogoRecetas;
    public Material materialPocionDesconocida;
    public GestorCompradores gestorNPC;
    public ControladorLibroUI controladorLibroUI;

    public TextMeshProUGUI textoInventario; // Arrástralo desde el inspector
    private bool inventarioVisible = false;

    private bool tiendaAbierta = false;
    private bool esperandoConfirmacionCerrarTienda = false;
    private Baul baulMiradoActual = null; 

   

    void Start()
    {
        audioSourceJugador = GetComponent<AudioSource>();
        if (panelItemSostenido != null) panelItemSostenido.SetActive(false);
        if (textoNotificacion != null) textoNotificacion.gameObject.SetActive(false);
        if (puntoAnclajeItem3D == null) Debug.LogWarning("FALTA ASIGNAR 'Punto Anclaje Item 3D'!", this.gameObject);
        if (gestorNPC == null) Debug.LogError("FALTA ASIGNAR 'Gestor NPC'!", this.gameObject);
        if (catalogoRecetas == null) Debug.LogError("FALTA ASIGNAR 'Catalogo Recetas'!", this.gameObject);
        if (materialPocionDesconocida == null) Debug.LogWarning("Material Pocion Desconocida no asignado.");
        if (controladorLibroUI == null) Debug.LogWarning("FALTA ASIGNAR 'Controlador Libro UI'!", this.gameObject);
    }

    void Update()
    {
        if (controladorLibroUI != null && controladorLibroUI.gameObject.activeInHierarchy) return;
        ManejarInteraccionMirada();
        ManejarEntradaAccion();
        ManejarNotificaciones();
        if (Input.GetKeyDown(KeyCode.Q) && tipoItemSostenido != TipoItem.Nada) { ReproducirSonidoJugador(sonidoTirarItem); LimpiarItemSostenido(); }
        if (Input.GetMouseButtonDown(1) && tipoItemSostenido == TipoItem.FrascoLleno) MostrarContenidoFrascoLleno();

        if (Input.GetKeyDown(KeyCode.Q) && fuenteIngredientesMirada != null)
        {
            string nombreIngrediente = fuenteIngredientesMirada.datosIngrediente.nombreIngrediente;
            int cantidadJugador = InventoryManager.Instance.ContarItem(nombreIngrediente);
            if (cantidadJugador > 0)
            {
                fuenteIngredientesMirada.DevolverIngrediente(); // <-- Quitar argumento
                // Muestra mensaje o actualiza UI
            }
            else
            {
                // Opcional: mensaje de que no tienes ese ingrediente
            }
        }

        // Sincroniza el item sostenido con el slot seleccionado del inventario
        if (InventoryManager.Instance != null)
        {
            int selIdx = InventoryManager.Instance.GetSelectedIndex();
            if (selIdx >= 0 && selIdx < InventoryManager.Instance.items.Count)
            {
                var stack = InventoryManager.Instance.items[selIdx];
                if (stack != null && (itemSostenido == null || (itemSostenido is DatosIngrediente di && di.nombreIngrediente != stack.nombre)))
                {
                    // Buscar el ScriptableObject correspondiente
                    DatosIngrediente ing = InventoryManager.Instance.todosLosIngredientes.Find(i => i.nombreIngrediente == stack.nombre);
                    if (ing != null)
                    {
                        itemSostenido = ing;
                        tipoItemSostenido = TipoItem.Ingrediente;
                    }
                    else
                    {
                        DatosFrasco frasco = InventoryManager.Instance.todosLosFrascos.Find(f => f.nombreItem == stack.nombre);
                        if (frasco != null)
                        {
                            // Si el nombre es exactamente "FrascoLleno", es un frasco lleno
                            if (stack.nombre == "FrascoLleno")
                            {
                                itemSostenido = frasco;
                                tipoItemSostenido = TipoItem.FrascoLleno;
                            }
                            else
                            {
                                itemSostenido = frasco;
                                tipoItemSostenido = TipoItem.FrascoVacio;
                            }
                        }
                        else
                        {
                            itemSostenido = null;
                            tipoItemSostenido = TipoItem.Nada;
                        }
                    }
                }
            }
            else if (selIdx == -1)
            {
                itemSostenido = null;
                tipoItemSostenido = TipoItem.Nada;
            }
        }

      
    }

    void ManejarInteraccionMirada()
    {
        RaycastHit hit;
        bool golpeoAlgo = Physics.Raycast(camaraJugador.transform.position, camaraJugador.transform.forward, out hit, distanciaInteraccion, capaInteraccion);
        GameObject objetoGolpeado = golpeoAlgo ? hit.collider.gameObject : null;

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
        if (npcMiradoActual != null && (!golpeoAlgo || objetoGolpeado != npcMiradoActual.gameObject))
        {
            npcMiradoActual = null;
        }
        if (libroMiradoActual != null && (!golpeoAlgo || objetoGolpeado != libroMiradoActual.gameObject))
        {
            libroMiradoActual.OcultarInformacion();
            libroMiradoActual = null;
        }
        if (puertaMiradaActual != null && (!golpeoAlgo || objetoGolpeado != puertaMiradaActual.gameObject))
        {
            puertaMiradaActual.OcultarInformacion();
            puertaMiradaActual = null;
            esperandoConfirmacionCerrarTienda = false; // Resetea confirmación si deja de mirar la puerta
        }
        if (camaMiradaActual != null && (!golpeoAlgo || objetoGolpeado != camaMiradaActual.gameObject))
        {
            camaMiradaActual.OcultarInformacion();
            camaMiradaActual = null;
        }

        if (baulMiradoActual != null && (!golpeoAlgo || objetoGolpeado != baulMiradoActual.gameObject))
        {
            baulMiradoActual = null;
        }


        if (ingredienteRecolectableMirado != null && (!golpeoAlgo || objetoGolpeado != ingredienteRecolectableMirado.gameObject))
        {
            ingredienteRecolectableMirado.OcultarInformacion();
            ingredienteRecolectableMirado = null;
        }
        if (cartelMiradoActual != null && (!golpeoAlgo || objetoGolpeado != cartelMiradoActual)) // NUEVO
        {
            cartelMiradoActual = null;
        } // NUEVO

        if (golpeoAlgo)
        {
            bool yaLoMiraba = (fuenteIngredientesMirada != null && objetoGolpeado == fuenteIngredientesMirada.gameObject) ||
                              (calderoMiradoActual != null && objetoGolpeado == calderoMiradoActual.gameObject) ||
                              (fuenteFrascosMirada != null && objetoGolpeado == fuenteFrascosMirada.gameObject) ||
                              (npcMiradoActual != null && objetoGolpeado == npcMiradoActual.gameObject) ||
                              (libroMiradoActual != null && objetoGolpeado == libroMiradoActual.gameObject) ||
                              (puertaMiradaActual != null && objetoGolpeado == puertaMiradaActual.gameObject) ||
                              (camaMiradaActual != null && objetoGolpeado == camaMiradaActual.gameObject);

            if (!yaLoMiraba)
            {
                if (objetoGolpeado.TryGetComponent(out FuenteIngredientes ingSrc)) { fuenteIngredientesMirada = ingSrc; ingSrc.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out FuenteFrascos fraSrc)) { fuenteFrascosMirada = fraSrc; fraSrc.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out Caldero caldSrc)) { calderoMiradoActual = caldSrc; return; }
                if (objetoGolpeado.GetComponentInParent<NPCComprador>() is NPCComprador npcCtrl) { npcMiradoActual = npcCtrl; return; }
                if (objetoGolpeado.TryGetComponent(out LibroRecetasInteractuable libroCtrl)) { libroMiradoActual = libroCtrl; libroCtrl.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out PuertaCambioEscena puertaCtrl)) { puertaMiradaActual = puertaCtrl; puertaMiradaActual.MostrarInformacion(); return; }
                if (objetoGolpeado.TryGetComponent(out CamaInteractuable camaCtrl)) { camaMiradaActual = camaCtrl; camaMiradaActual.MostrarInformacion(); }
                if (objetoGolpeado.TryGetComponent(out Baul baulCtrl))
                    {
                        baulMiradoActual = baulCtrl;
                        return;
                    }
                if (objetoGolpeado.TryGetComponent(out IngredienteRecolectable ingRecCtrl))
                {
                    if (puertaMiradaActual != null) { puertaMiradaActual.OcultarInformacion(); puertaMiradaActual = null; }
                    if (camaMiradaActual != null) { camaMiradaActual.OcultarInformacion(); camaMiradaActual = null; }
                    ingredienteRecolectableMirado = ingRecCtrl;
                    ingredienteRecolectableMirado.MostrarInformacion();
                }
                if (objetoGolpeado.name == "cartel") { cartelMiradoActual = objetoGolpeado; return; }
               
                
            }
        }
    }

    void ManejarEntradaAccion()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (fuenteIngredientesMirada != null) InteractuarConFuenteIngredientes();
        else if (fuenteFrascosMirada != null) InteractuarConFuenteFrascos();
        else if (calderoMiradoActual != null) InteractuarConCaldero();
        else if (npcMiradoActual != null) InteractuarConNPC();
        else if (libroMiradoActual != null) InteractuarConLibro();
        else if (puertaMiradaActual != null) InteractuarConPuerta();
        else if (camaMiradaActual != null) InteractuarConCama();
        else if (ingredienteRecolectableMirado != null) InteractuarConIngredienteRecolectable();
        else if (cartelMiradoActual != null) InteractuarConCartel();
        else if (baulMiradoActual != null) {
            baulMiradoActual.AbrirOCerrarBaul();
        }
    }

void InteractuarConFuenteIngredientes()
{
    if (tipoItemSostenido == TipoItem.Nada)
    {
        DatosIngrediente r = fuenteIngredientesMirada.IntentarRecoger();
        if (r != null)
        {
            EstablecerItemSostenido(r, TipoItem.Ingrediente);
            // Ya no agregamos al inventario aquí, lo hace la fuente
        }
        else
        {
            MostrarNotificacion($"¡No quedan más {fuenteIngredientesMirada.datosIngrediente.nombreIngrediente}!", -1f, true);
        }
    }
    // Permitir agarrar más del mismo ingrediente si ya lo tienes en la mano
    else if (tipoItemSostenido == TipoItem.Ingrediente && itemSostenido is DatosIngrediente ingActual
             && fuenteIngredientesMirada.datosIngrediente == ingActual)
    {
        DatosIngrediente r = fuenteIngredientesMirada.IntentarRecoger();
        if (r != null)
        {
            // Sumar al inventario lógico y visual
            //InventoryManager.Instance?.AddItem(ingActual.nombreIngrediente);
            //InventoryManager.Instance?.AddItemVisual(ingActual.icono, -1); // <-- slotIndex -1
            // Opcional: feedback visual/sonoro
            ReproducirSonidoJugador(sonidoRecogerItem);
        }
        else
        {
            MostrarNotificacion($"¡No quedan más {fuenteIngredientesMirada.datosIngrediente.nombreIngrediente}!", -1f, true);
        }
    }
    // --- NUEVO: Si tienes otro ingrediente, lo suelta automáticamente y agarra el nuevo ---
    else if (tipoItemSostenido == TipoItem.Ingrediente && itemSostenido is DatosIngrediente ingActual2
             && fuenteIngredientesMirada.datosIngrediente != ingActual2)
    {
        LimpiarItemSostenido();
        DatosIngrediente r = fuenteIngredientesMirada.IntentarRecoger();
        if (r != null)
        {
            EstablecerItemSostenido(r, TipoItem.Ingrediente);
        }
        else
        {
            MostrarNotificacion($"¡No quedan más {fuenteIngredientesMirada.datosIngrediente.nombreIngrediente}!", -1f, true);
        }
    }
    else
    {
        MostrarNotificacion("Ya tienes algo en la mano.", -1f, true);
    }
}

    void InteractuarConFuenteFrascos()
    {
        if (tipoItemSostenido == TipoItem.Nada)
        {
            DatosFrasco r = fuenteFrascosMirada.IntentarRecoger();
            if (r != null) EstablecerItemSostenido(r, TipoItem.FrascoVacio);
            else MostrarNotificacion($"¡No quedan más {fuenteFrascosMirada.datosFrasco.nombreItem}!", -1f, true);
        }
        else MostrarNotificacion("Ya tienes algo en la mano.", -1f, true);
    }

    void InteractuarConCaldero()
    {
        // Ahora usa el objeto seleccionado del inventario
        if (InventoryManager.Instance != null)
        {
            int selIdx = InventoryManager.Instance.GetSelectedIndex();
            if (selIdx >= 0 && selIdx < InventoryManager.Instance.items.Count)
            {
                var stack = InventoryManager.Instance.items[selIdx];
                // Buscar el ScriptableObject correspondiente
                DatosIngrediente ing = InventoryManager.Instance.todosLosIngredientes.Find(i => i.nombreIngrediente == stack.nombre);
                if (ing != null)
                {
                    // Agrega el ingrediente seleccionado al caldero
                    calderoMiradoActual.AgregarIngrediente(ing);
                    InventoryManager.Instance.RemoveItem(ing.nombreIngrediente, 1);
                    // Actualiza itemSostenido y tipoItemSostenido
                    itemSostenido = null;
                    tipoItemSostenido = TipoItem.Nada;
                    return;
                }
                // Si es frasco, lógica similar...
            }
        }
        // ...si no hay seleccionado, puedes mostrar mensaje de error...
    }

    void InteractuarConNPC()
    {
        if (npcMiradoActual.TryGetComponent<NPCComprador>(out NPCComprador clienteTienda))
        {
            if (clienteTienda.EstaEsperandoAtencion()) clienteTienda.IniciarPedidoYTimer();
            // --- NUEVO: Entrega directa desde el caldero ---
            else if (clienteTienda.EstaEsperandoEntrega())
            {
                // Buscar caldero en la escena
                Caldero caldero = FindObjectOfType<Caldero>();
                if (caldero != null && caldero.HayPocionListaParaEntregar())
                {
                    var pocion = caldero.ObtenerYConsumirUltimaPocion();
                    if (pocion != null)
                    {
                        clienteTienda.IntentarEntregarPocion(pocion);
                        MostrarNotificacion("¡Entregaste la poción al cliente!", 2f, false);
                        caldero.ReiniciarCaldero(); // <-- Limpia el caldero después de entregar
                        return;
                    }
                }
                MostrarNotificacion("Prepara una poción en el caldero para entregar.", -1f, true);
            }
            else MostrarNotificacion("Parece ocupado ahora mismo...", 2f, false);
        }
        else if (npcMiradoActual.TryGetComponent<NPCVendedor>(out NPCVendedor vendedor))
            vendedor.AbrirInterfazTienda();
        else if (npcMiradoActual.TryGetComponent<NPCDialogo>(out NPCDialogo dialogo))
            dialogo.IniciarDialogo();
        else MostrarNotificacion("No parece querer nada ahora...", 2f, false);
    }

    void InteractuarConLibro()
    {
        if (controladorLibroUI != null)
        {
            controladorLibroUI.AbrirLibro();
            fuenteIngredientesMirada = null; calderoMiradoActual = null; fuenteFrascosMirada = null; npcMiradoActual = null; puertaMiradaActual = null;
        }
        else { Debug.LogError("ControladorLibroUI no asignado!"); MostrarNotificacion("Error al abrir libro.", -1f, true); }
    }

    void InteractuarConPuerta()
    {
        if (!esperandoConfirmacionCerrarTienda)
        {
            UIMessageManager.Instance?.MostrarMensaje("¿Seguro que quieres salir de la tienda? Pulsa E para confirmar.");
            esperandoConfirmacionCerrarTienda = true;
        }
        else
        {
            esperandoConfirmacionCerrarTienda = false;
            UIMessageManager.Instance?.MostrarMensaje("Saliendo de la tienda...");

            // Desactivar el cartel hasta el próximo día
            GameObject cartel = GameObject.Find("cartel");
            if (cartel != null)
                cartel.SetActive(false);

            // Hacer de noche y bloquear la tienda
            if (GestorJuego.Instance != null)
            {
                GestorJuego.Instance.horaActual = HoraDelDia.Noche;
                if (GestorJuego.Instance.gestorNPCs != null)
                {
                    GestorJuego.Instance.gestorNPCs.tiendaAbierta = false;
                    GestorJuego.Instance.gestorNPCs.compradoresHabilitados = false;
                }
            }

            // Cambiar de escena usando la puerta
            if (puertaMiradaActual != null)
            {
                puertaMiradaActual.CambiarEscena();
            }
        }
    }

    void InteractuarConCama()
    {
        if (GestorJuego.Instance != null)
        {
            if (GestorJuego.Instance.PuedeDormir()) 
            {
                GestorJuego.Instance.IrADormir();

                // REACTIVAR EL CARTEL al dormir
                GameObject cartel = GameObject.Find("cartel");
                if (cartel != null)
                    cartel.SetActive(true);
            }
            else MostrarNotificacion("Solo puedes dormir por la noche...", 2f, false);
        }
        else Debug.LogError("No se encontró GestorJuego para intentar dormir.");
    }

    void InteractuarConIngredienteRecolectable()
    {
        if (ingredienteRecolectableMirado != null && ingredienteRecolectableMirado.datosIngrediente != null)
        {
            if (ingredienteRecolectableMirado.datosIngrediente.nombreIngrediente.ToLower() == "miel"
                && InventoryManager.Instance != null
                && InventoryManager.Instance.HasItem("palita"))
            {
                if (Random.value < 0.85f) // 85% de probabilidad de abejas
                {
                    ingredienteRecolectableMirado.IniciarMinijuegoAbejas();
                    return; // Espera a que termine el minijuego
                }
                else
                {
                    Debug.Log("No hay abejas");
                    ingredienteRecolectableMirado.Recolectar();
                    return;
                }
            }
        }

        // --- NUEVO: Agregar al caldero si hay slot seleccionado y el caldero está cerca ---
        if (InventoryManager.Instance != null)
        {
            int selIdx = InventoryManager.Instance.GetSelectedIndex();
            // Si el slot seleccionado es válido y está vacío (no hay stack), no hacemos nada
            if (selIdx >= 0 && selIdx < InventoryManager.Instance.items.Count)
            {
                var stack = InventoryManager.Instance.items[selIdx];
                if (stack == null || string.IsNullOrEmpty(stack.nombre) || stack.cantidad <= 0)
                {
                    // Slot vacío, no agregamos nada al caldero
                    MostrarNotificacion("Selecciona un slot válido para agregar al caldero.", 2f, true);
                    return;
                }
            }
        }

        // --- Lógica para agregar al caldero si está cerca y hay slot seleccionado ---
        if (calderoMiradoActual != null && ingredienteRecolectableMirado != null)
        {
            DatosIngrediente ing = ingredienteRecolectableMirado.datosIngrediente;
            if (ing != null)
            {
                bool agregado = calderoMiradoActual.AgregarIngrediente(ing);
                if (agregado)
                {
                    MostrarNotificacion($"Agregado {ing.nombreIngrediente} al caldero.", 2f, false);
                    ingredienteRecolectableMirado.Recolectar();
                    return;
                }
                else
                {
                    MostrarNotificacion("No se pudo agregar al caldero.", 2f, true);
                    return;
                }
            }
        }

        // --- Si no hay caldero cerca, recolecta normalmente ---
        if (tipoItemSostenido == TipoItem.Nada)
        {
            // Agrega la imagen al inventario visual
            if (InventoryManager.Instance != null)
            {
                var datos = ingredienteRecolectableMirado.datosIngrediente;
                //InventoryManager.Instance.AddItemVisual(datos.icono, -1); // <-- slotIndex -1
                InventoryManager.Instance.AddItem(datos.nombreIngrediente); // <-- Asegura que usa el nombre correcto
            }
            ingredienteRecolectableMirado.Recolectar();
        }
        else MostrarNotificacion("Tienes las manos llenas para recolectar.", 2f, true);
    }

    void InteractuarConCartel() // NUEVO
    {
        if (GestorJuego.Instance != null && GestorJuego.Instance.horaActual == HoraDelDia.Noche)
        {
            UIMessageManager.Instance?.MostrarMensaje("La tienda ya está cerrada por hoy. Vuelve mañana.");
            return;
        }

        if (!tiendaAbierta)
        {
            tiendaAbierta = true;
            if (GestorJuego.Instance != null && GestorJuego.Instance.gestorNPCs != null)
            {
                var gestorNPCs = GestorJuego.Instance.gestorNPCs;
                gestorNPCs.tiendaAbierta = true;

                // Si NO se genera el NPC Tienda porque ya tenés la palita, habilitá compradores directamente
                if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("palita"))
                {
                    gestorNPCs.compradoresHabilitados = true;
                }
                else
                {
                    gestorNPCs.compradoresHabilitados = false;
                    gestorNPCs.GenerarNPCTienda();
                }
            }
            UIMessageManager.Instance?.MostrarMensaje("¡Tienda abierta! El día comienza...");

            // OCULTAR EL CARTEL (no destruir)
            if (cartelMiradoActual != null)
            {
                cartelMiradoActual.SetActive(false);
                // cartelMiradoActual = null; // Opcional: mantener referencia
            }
        }
        else
        {
            UIMessageManager.Instance?.MostrarMensaje("La tienda ya está abierta.");
        }
    } // NUEVO

    void EstablecerItemSostenido(ScriptableObject itemData, TipoItem tipo)
    {
        itemSostenido = itemData;
        tipoItemSostenido = tipo;
        contenidoFrascoLleno = null;
        LimpiarInstanciaItem3D();

        string nombreMostrar = "Desconocido";
        Sprite iconoMostrar = null;
        GameObject prefab3D = null;
        Material matVacio = null;
        Vector3 rotacionItemEspecifica = Vector3.zero;

        if (tipo == TipoItem.Ingrediente && itemData is DatosIngrediente ingData)
        {
            nombreMostrar = ingData.nombreIngrediente;
            iconoMostrar = ingData.icono;
            prefab3D = ingData.prefabModelo3D;
            rotacionItemEspecifica = ingData.rotacionEnMano;
        }
        else if (tipo == TipoItem.FrascoVacio && itemData is DatosFrasco fraData)
        {
            nombreMostrar = fraData.nombreItem;
            iconoMostrar = fraData.icono;
            prefab3D = fraData.prefabModelo3D;
            matVacio = fraData.materialVacio;
            rotacionItemEspecifica = fraData.rotacionEnMano;
        }

        if (panelItemSostenido != null)
        {
            if (uiIconoItemSostenido != null && iconoMostrar != null) { uiIconoItemSostenido.sprite = iconoMostrar; uiIconoItemSostenido.enabled = true; }
            else if (uiIconoItemSostenido != null) { uiIconoItemSostenido.enabled = false; }
            if (uiNombreItemSostenido != null) { uiNombreItemSostenido.text = nombreMostrar; }
            panelItemSostenido.SetActive(true);
        }
        if (prefab3D != null && puntoAnclajeItem3D != null)
        {
            instanciaItem3D = Instantiate(prefab3D, puntoAnclajeItem3D);
            instanciaItem3D.transform.localPosition = Vector3.zero;
            instanciaItem3D.transform.localRotation = Quaternion.identity;

            if (rotacionItemEspecifica != Vector3.zero)
                instanciaItem3D.transform.localRotation *= Quaternion.Euler(rotacionItemEspecifica);

            if (tipo == TipoItem.FrascoVacio)
            {
                FrascoPocion scriptFrasco = instanciaItem3D.GetComponent<FrascoPocion>();
                if (scriptFrasco != null)
                {
                    if (itemData is DatosFrasco dFr) { scriptFrasco.materialVacio = dFr.materialVacio; scriptFrasco.materialLleno = dFr.materialLleno; }
                    scriptFrasco.EstablecerApariencia(false);
                }
                else
                {
                    MeshRenderer rend = instanciaItem3D.GetComponentInChildren<MeshRenderer>();
                    if (rend != null && matVacio != null) { rend.material = matVacio; }
                }
            }
        }
        ReproducirSonidoJugador(sonidoRecogerItem);
    }

    void LlenarFrascoSostenido(DatosIngrediente[] contenidoArray)
    {
        if (tipoItemSostenido != TipoItem.FrascoVacio || !(itemSostenido is DatosFrasco frascoData)) { Debug.LogError("Error llenar frasco: No se sostiene frasco vacío."); return; }
        if (contenidoArray == null || contenidoArray.Length == 0) { Debug.LogError("Error llenar frasco: Sin ingredientes."); return; }

        List<DatosIngrediente> contenidoLista = new List<DatosIngrediente>(contenidoArray);
        tipoItemSostenido = TipoItem.FrascoLleno;
        contenidoFrascoLleno = contenidoLista;

        string nombrePocion = "Poción Desconocida";
        Material materialAplicar = materialPocionDesconocida;
        PedidoPocionData recetaEncontrada = null;

        if (catalogoRecetas != null)
        {
            recetaEncontrada = catalogoRecetas.BuscarRecetaPorIngredientes(contenidoLista);
            if (recetaEncontrada != null)
            {
                nombrePocion = recetaEncontrada.nombreResultadoPocion;
                if (recetaEncontrada.materialResultado != null)
                    materialAplicar = recetaEncontrada.materialResultado;
            }
        }

        // --- NUEVO: Agregar el frasco lleno al inventario ---
        if (InventoryManager.Instance != null)
        {
            // Puedes usar un nombre genérico o uno específico según tu lógica
            string nombreFrascoLleno = "FrascoLleno";
            InventoryManager.Instance.AddItem(nombreFrascoLleno);

            // Selecciona el slot recién agregado (último)
            int idx = InventoryManager.Instance.items.FindIndex(i => i.nombre == nombreFrascoLleno);
            if (idx >= 0)
            {
                // Selecciona el slot del frasco lleno
                typeof(InventoryManager).GetField("selectedIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(InventoryManager.Instance, idx);
            }
            // Actualiza la UI
            InventoryManager.Instance.ActualizarUIVisual(null, null);
        }
        // --- FIN NUEVO ---

        if (panelItemSostenido != null)
        {
            if (uiIconoItemSostenido != null && frascoData.icono != null) { uiIconoItemSostenido.sprite = frascoData.icono; uiIconoItemSostenido.enabled = true; }
            if (uiNombreItemSostenido != null) uiNombreItemSostenido.text = nombrePocion;
            panelItemSostenido.SetActive(true);
        }

        if (instanciaItem3D != null)
        {
            MeshRenderer rendererFrasco = instanciaItem3D.GetComponentInChildren<MeshRenderer>();
            if (rendererFrasco != null && materialAplicar != null)
                rendererFrasco.material = Instantiate(materialAplicar);

            FrascoPocion scriptFrasco = instanciaItem3D.GetComponent<FrascoPocion>();
            if (scriptFrasco != null) scriptFrasco.Llenar(contenidoArray);
        }
        ReproducirSonidoJugador(sonidoRecogerPocion);
    }

    public void LimpiarItemSostenido()
    {
        itemSostenido = null;
        tipoItemSostenido = TipoItem.Nada;
        contenidoFrascoLleno = null;
        if (panelItemSostenido != null) panelItemSostenido.SetActive(false);
        LimpiarInstanciaItem3D();
    }

    void LimpiarInstanciaItem3D()
    {
        if (instanciaItem3D != null)
        {
            Destroy(instanciaItem3D);
            instanciaItem3D = null;
        }
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

    void MostrarContenidoFrascoLleno()
    {
        if (contenidoFrascoLleno != null && contenidoFrascoLleno.Count > 0)
        {
            string t = "Contenido: ";
            for (int i = 0; i < contenidoFrascoLleno.Count; i++)
            {
                t += (contenidoFrascoLleno[i]?.nombreIngrediente ?? "???");
                t += (i < contenidoFrascoLleno.Count - 1 ? ", " : "");
            }
            MostrarNotificacion(t, 4f);
        }
        else
        {
            MostrarNotificacion("Frasco lleno pero sin contenido registrado.", 2f, true);
        }
    }

    void ReproducirSonidoJugador(AudioClip clip)
    {
        if (audioSourceJugador != null && clip != null)
            audioSourceJugador.PlayOneShot(clip);
    }

   
} // Fin de la clase