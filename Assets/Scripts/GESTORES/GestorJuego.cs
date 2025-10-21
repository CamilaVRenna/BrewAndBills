using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System; // Necesario para el Action

// Asegúrate de que TimeManager.cs esté presente y contenga OnNewDaySequenceStarted
// Si TimeManager no está definido, este script no compilará.

public enum HoraDelDia { Manana, Tarde, Noche }

[System.Serializable]
public class StockInicialIngrediente
{
    [Tooltip("La clave del ingrediente (string) del ItemCatalog.")]
    public string claveIngrediente;
    public int stockInicial = 5;
}

[System.Serializable]
public class StockEntry
{
    public string ingredienteAssetName;
    public int cantidad;
}

[System.Serializable]
public class StockDataWrapper
{
    public List<StockEntry> stockList = new List<StockEntry>();
}

public class GestorJuego : MonoBehaviour
{
    public static GestorJuego Instance { get; private set; }

    // =========================================================================
    // NUEVAS REFERENCIAS DE CATÁLOGO Y GESTIÓN DE DATOS
    // =========================================================================

    [Header("Catálogo de Datos Centralizado")]
    public ItemCatalog catalogoMaestro;

    // =========================================================================
    // ESTADO Y CONFIGURACIÓN DEL JUEGO
    // =========================================================================

    [Header("Límites Diarios")]
    public int limiteNPCsPorDia = 5;
    private int npcsGeneradosHoy = 0;

    [Header("Configuración Guardado y Spawn")]
    private string nombrePuntoSpawnSiguiente = "SpawnInicialCama";

    [Header("Inventario/Stock Ingredientes")]
    public List<StockInicialIngrediente> configuracionStockInicial;
    public Dictionary<string, int> stockIngredientesTienda = new Dictionary<string, int>();

    [Header("Estado del Juego")]
    public int diaActual = 1;
    public int dineroActual = 50;
    public HoraDelDia horaActual = HoraDelDia.Manana;

    [Header("Ciclo Día/Noche")]
    public Material skyboxManana;
    public Material skyboxTarde;
    public Material skyboxNoche;

    [Header("Economía")]
    public int valorPocionCorrecta = 5;

    [Header("Referencias UI y Efectos")]
    public GestorUI gestorUI;
    public AudioClip sonidoGanarDinero;
    public AudioClip sonidoPerderDinero;
    public GestorCompradores gestorNPCs;
    public TMPro.TextMeshProUGUI textoMielesRecolectadas; // Si no lo usas, puedes eliminarlo

    [Header("Audio Ambiente")]
    public AudioClip musicaMenu;
    public AudioClip audioDia;
    public AudioClip audioNoche;

    private Light luzDireccionalPrincipal = null;
    // ELIMINADA: La variable 'durmiendo' se gestiona en el TimeManager

    // =========================================================================
    // CICLO DE VIDA Y EVENTOS
    // =========================================================================

    void Awake()
    {
        // ... (Tu código original de Singleton y Awake)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (catalogoMaestro != null)
            {
                catalogoMaestro.Initialize();
                Debug.Log("[GestorJuego] Catálogo Maestro Inicializado.");
            }
            else
            {
                Debug.LogError("[GestorJuego] FATAL: ItemCatalog no asignado en el Inspector.");
            }

            CargarDatos();
        }
        else { Destroy(gameObject); }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += EscenaCargada;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= EscenaCargada;
        // 🚨 CRUCIAL: Desuscribir de los eventos del TimeManager para evitar errores al destruir.
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStart -= OnNewDayStarted;
            TimeManager.Instance.OnNightStart -= OnNewNightStarted;
            // ❌ ELIMINADO: OnNightEnd ya no es necesario aquí. Usamos OnNewDaySequenceStarted.
            TimeManager.Instance.OnNewDaySequenceStarted -= OnNewDaySequenceStarted; // <- AÑADIDO
        }
    }

    void Start()
    {
        if (TimeManager.Instance != null)
        {
            // Suscripción de eventos para el flujo del juego
            TimeManager.Instance.OnDayStart += OnNewDayStarted;
            TimeManager.Instance.OnNightStart += OnNewNightStarted;
            TimeManager.Instance.OnNewDaySequenceStarted += OnNewDaySequenceStarted; // <- CRUCIAL
        }
        else
        {
            Debug.LogError("FATAL: TimeManager.Instance sigue siendo null en GestorJuego.Start().");
        }
        ActualizarAparienciaCiclo(true);
        Debug.Log("GestorJuego iniciado, Skybox inicial aplicado.");
    }

    // ... (Tu código original de CargarEscenaConPantallaDeCarga y EscenaCargada)
    public static void CargarEscenaConPantallaDeCarga(string nombreEscenaACargar)
    {
        if (string.IsNullOrEmpty(nombreEscenaACargar))
        {
            Debug.LogError("Se intentó cargar una escena con nombre vacío.");
            return;
        }
        if (GestorJuego.Instance != null)
            GestorJuego.Instance.GuardarDatos();
        SceneManager.LoadScene("PantallaCarga");
    }

    void EscenaCargada(Scene escena, LoadSceneMode modo)
    {
        if (escena.name == "Arranque" || escena.name == "LoadingScreen" || escena.name == "PantallaCarga")
        {
            Debug.Log($"EscenaCargada: Ignorando escena de utilidad '{escena.name}'.");
            return;
        }

        Debug.Log($"---[EscenaCargada] Escena: '{escena.name}', Hora al entrar: {horaActual} ---");

        ActualizarAparienciaCiclo(true);

        Light[] luces = FindObjectsOfType<Light>();
        foreach (Light luz in luces)
        {
            if (luz.type == LightType.Directional)
            {
                luzDireccionalPrincipal = luz;
                Debug.Log($"Luz direccional encontrada: {luz.gameObject.name}");
                break;
            }
        }
        if (luzDireccionalPrincipal == null) Debug.LogWarning("No se encontró luz direccional principal.");

        gestorUI = FindObjectOfType<GestorUI>();
        gestorNPCs = FindObjectOfType<GestorCompradores>();

        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null)
        {
            PuntoSpawn[] puntos = FindObjectsOfType<PuntoSpawn>();
            Debug.Log($"EscenaCargada: Encontrados {puntos.Length} PuntoSpawn.");
            Debug.Log($"EscenaCargada: Buscando punto con nombre: '{nombrePuntoSpawnSiguiente}'");

            PuntoSpawn puntoDestino = puntos.FirstOrDefault(p => p != null && p.nombreIdentificador == nombrePuntoSpawnSiguiente);
            if (puntoDestino == null && nombrePuntoSpawnSiguiente != "SpawnInicialCama")
            {
                Debug.LogWarning($"No se encontró '{nombrePuntoSpawnSiguiente}'. Buscando fallback 'SpawnInicialCama'...");
                puntoDestino = puntos.FirstOrDefault(p => p != null && p.nombreIdentificador == "SpawnInicialCama");
            }
            if (puntoDestino != null)
            {
                Debug.Log($"Punto destino encontrado: '{puntoDestino.name}' en {puntoDestino.transform.position}. Intentando mover jugador...");
                CharacterController cc = jugador.GetComponent<CharacterController>();
                Vector3 posAntes = jugador.transform.position;
                if (cc != null) cc.enabled = false;
                jugador.transform.position = puntoDestino.transform.position;
                jugador.transform.rotation = puntoDestino.transform.rotation;
                if (cc != null) cc.enabled = true;
                Debug.Log($"Posición JUGADOR ANTES: {posAntes}, DESPUÉS: {jugador.transform.position}");
                jugador.ResetearVistaVertical();
            }
            else
            {
                Debug.LogError("¡No se encontró NINGÚN PuntoSpawn ('" + nombrePuntoSpawnSiguiente + "' o 'SpawnInicialCama') para posicionar al jugador!");
            }
        }
        else if (escena.name != "MenuPrincipal" && escena.name != "MainMenu")
        {
            Debug.LogWarning("No se encontró jugador en EscenaCargada.");
        }

        AudioClip clipPoner = null;
        if (escena.name == "MenuPrincipal")
        {
            clipPoner = musicaMenu;
            Debug.Log("EscenaCargada: Seleccionando música del menú.");
        }
        else if (escena.name == "TiendaDeMagia" || escena.name == "Bosque")
        {
            switch (horaActual)
            {
                case HoraDelDia.Manana:
                case HoraDelDia.Tarde:
                    clipPoner = audioDia;
                    break;
                case HoraDelDia.Noche:
                    clipPoner = audioNoche;
                    break;
            }
            Debug.Log($"EscenaCargada: Seleccionando audio para {horaActual}: {(clipPoner != null ? clipPoner.name : "Ninguno")}");
        }
        if (GestorAudio.Instancia != null)
        {
            GestorAudio.Instancia.CambiarMusicaFondo(clipPoner);
        }
        else { Debug.LogWarning("GestorAudio no encontrado para cambiar música."); }

        if (gestorUI == null && (escena.name == "TiendaDeMagia" || escena.name == "Bosque" || escena.name == "MainMenu"))
        {
            Debug.LogWarning($"GestorUI no encontrado en la escena {escena.name}. ¿Falta el objeto UIManager o el script?");
        }

        if (gestorNPCs == null && escena.name == "TiendaDeMagia")
        {
            Debug.LogWarning($"GestorNPCs no encontrado en la escena {escena.name}.");
        }
    }
    // ... (Fin de EscenaCargada)

    // --- MÉTODOS para manejar los eventos del TimeManager ---

    /// <summary>
    /// Se llama al inicio de la TRANSICIÓN (antes del fade a negro).
    /// Su trabajo es: Bloquear input, guardar datos y mostrar el mensaje de desmayo si aplica.
    /// </summary>
    private void OnNewDaySequenceStarted()
    {
        Debug.Log("GestorJuego: TimeManager inicia secuencia de fin de ciclo. BLOQUEANDO jugador y GUARDANDO.");

        // 1. Bloquear input del jugador (preparación para el fade)
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null)
        {
            jugador.HabilitarMovimiento(false);
        }
        else
        {
            Debug.LogError("No se encontró jugador para bloquear.");
        }

        // 2. Guardar el estado del juego
        GuardarDatos();

        // 3. Manejar desmayo visual si el jugador NO durmió manualmente
        if (TimeManager.Instance != null && !TimeManager.Instance.durmioManualmente)
        {
            // Mostrar mensaje flotante de desmayo (la duración del fade la maneja TimeManager)
            if (gestorUI != null) gestorUI.MostrarMensajeTemporal("¡Te desmayaste por el cansancio!", 3f);
        }
    }

    /// <summary>
    /// Se llama DESPUÉS de que el fade ha terminado (al comienzo real del nuevo día).
    /// Su trabajo es: Actualizar estado de NPCs, reposicionar jugador y DESBLOQUEAR.
    /// </summary>
    private void OnNewDayStarted()
    {
        Debug.Log("GestorJuego: ¡El TimeManager ha iniciado un nuevo día! Reposicionando jugador.");

        // 1. Actualizar estado del juego
        if (TimeManager.Instance != null)
        {
            diaActual = TimeManager.Instance.currentDay;
        }
        horaActual = HoraDelDia.Manana;

        npcsGeneradosHoy = 0;
        if (gestorNPCs != null) gestorNPCs.ReiniciarParaNuevoDia();

        // 2. Actualizar visuales y audio
        ActualizarAparienciaCiclo(true);
        if (GestorAudio.Instancia != null) GestorAudio.Instancia.CambiarMusicaFondo(audioDia);
        // El mensaje de inicio de día (DÍA X) se maneja ahora en el TimeManager, NO AQUÍ.
        GameObject cartel = GameObject.Find("cartel");
        if (cartel != null) cartel.SetActive(true);

        // 3. Reposicionar y DESBLOQUEAR el jugador
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null)
        {
            PuntoSpawn puntoCama = FindObjectsOfType<PuntoSpawn>().FirstOrDefault(p => p != null && p.nombreIdentificador == "SpawnInicialCama");
            if (puntoCama != null)
            {
                CharacterController cc = jugador.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                jugador.transform.position = puntoCama.transform.position;
                jugador.transform.rotation = puntoCama.transform.rotation;
                if (cc != null) cc.enabled = true;
                jugador.ResetearVistaVertical();
            }
            else { Debug.LogError("¡No se encontró 'SpawnInicialCama'!"); }

            // DESBLOQUEAR MOVIMIENTO
            jugador.HabilitarMovimiento(true);
        }
        else { Debug.LogError("No se encontró jugador para reposicionar."); }
    }

    private void OnNewNightStarted()
    {
        Debug.Log("GestorJuego: ¡El TimeManager ha iniciado la noche!");
        horaActual = HoraDelDia.Noche;
        ActualizarAparienciaCiclo(true);
        if (GestorAudio.Instancia != null) GestorAudio.Instancia.CambiarMusicaFondo(audioNoche);
        gestorNPCs?.DespawnTodosNPCsPorNoche();
        GameObject cartel = GameObject.Find("cartel");
        if (cartel != null) cartel.SetActive(false);
    }

    // ❌ ELIMINADO: OnNightEnded ya no necesita manejar el StartCoroutine de SecuenciaDormir/Desmayo.
    // La transición es iniciada por el TimeManager en su corrutina HandleDayEndTransition/TransitionToNewDay
    // que a su vez llama a OnNewDaySequenceStarted (definido arriba) para la lógica de bloqueo/guardado.
    // private void OnNightEnded() { ... }

    // --- MÉTODOS DE ECONOMÍA Y FLUJO DE JUEGO ---

    public void AnadirDinero(int cantidad)
    {
        dineroActual += cantidad;
        Debug.Log($"Dinero añadido: +{cantidad}. Total: {dineroActual}");
        if (gestorUI != null)
        {
            gestorUI.ActualizarUIDinero(dineroActual);
            gestorUI.MostrarCambioDinero(cantidad);
        }
        if (GestorAudio.Instancia != null && sonidoGanarDinero != null)
        {
            GestorAudio.Instancia.ReproducirSonido(sonidoGanarDinero);
        }
    }

    public void IrADormir()
    {
        // ❌ ELIMINADO: if (durmiendo) return; // La variable 'durmiendo' fue eliminada

        Debug.Log("Intentando ir a dormir (llamado desde interacción)...");

        // 🚨 CRUCIAL: El GestorJuego solo avisa. El TimeManager maneja TODA la secuencia de transición.
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RegistrarDormir();
        }
    }

    // ❌ ELIMINADOS: Se elimina toda la lógica de fades y transición del GestorJuego.
    // private IEnumerator SecuenciaDormir() { ... }
    // private IEnumerator SecuenciaDesmayo() { ... }

    // --- MÉTODOS DE ESTADO ---
    // ... (Tu código original de ObtenerNPCsGeneradosHoy, RegistrarNPCGeneradoHoy, PuedeDormir)
    public int ObtenerNPCsGeneradosHoy()
    {
        return npcsGeneradosHoy;
    }

    public void RegistrarNPCGeneradoHoy()
    {
        if (npcsGeneradosHoy < limiteNPCsPorDia)
        {
            npcsGeneradosHoy++;
            Debug.Log($"NPC Registrado hoy. Total: {npcsGeneradosHoy}/{limiteNPCsPorDia}");
        }
        else
        {
            Debug.LogWarning("Se intentó registrar NPC pero ya se alcanzó el límite diario.");
        }
    }

    public bool PuedeDormir()
    {
        return horaActual == HoraDelDia.Noche;
    }

    void ActualizarAparienciaCiclo(bool instantaneo = false)
    {
        Debug.Log($"[ActualizarAparienciaCiclo] Ejecutando para Hora: {horaActual}");

        Material skyboxAplicar = null;
        Color luzAmbiente = new Color(0.5f, 0.5f, 0.5f, 1f);
        float intensidadSol = 1.0f;
        Quaternion rotacionSol = Quaternion.Euler(50f, -30f, 0f);

        switch (horaActual)
        {
            case HoraDelDia.Manana:
                skyboxAplicar = skyboxManana;
                luzAmbiente = new Color(0.8f, 0.8f, 0.8f);
                rotacionSol = Quaternion.Euler(50f, -30f, 0f);
                intensidadSol = 1.0f;
                Debug.Log("[ActualizarAparienciaCiclo] Config Mañana.");
                break;
            case HoraDelDia.Tarde:
                skyboxAplicar = skyboxTarde;
                luzAmbiente = new Color(0.7f, 0.6f, 0.55f);
                rotacionSol = Quaternion.Euler(20f, -150f, 0f);
                intensidadSol = 0.75f;
                Debug.Log("[ActualizarAparienciaCiclo] Config Tarde.");
                break;
            case HoraDelDia.Noche:
                skyboxAplicar = skyboxNoche;
                luzAmbiente = new Color(0.1f, 0.1f, 0.18f);
                rotacionSol = Quaternion.Euler(-30f, -90f, 0f);
                intensidadSol = 0.08f;
                Debug.Log("[ActualizarAparienciaCiclo] Config Noche.");
                break;
        }

        Debug.Log($"[ActualizarAparienciaCiclo] Intentando aplicar Skybox: {(skyboxAplicar != null ? skyboxAplicar.name : "NINGUNO")}");

        if (skyboxAplicar != null) { RenderSettings.skybox = skyboxAplicar; DynamicGI.UpdateEnvironment(); }
        else { Debug.LogWarning($"Skybox NULO para {horaActual}."); }
        RenderSettings.ambientLight = luzAmbiente;
        Debug.Log($"[ActualizarAparienciaCiclo] Luz Ambiental aplicada: {luzAmbiente}");

        if (luzDireccionalPrincipal != null)
        {
            luzDireccionalPrincipal.intensity = intensidadSol;
            luzDireccionalPrincipal.transform.rotation = rotacionSol;
            Debug.Log($"[ActualizarAparienciaCiclo] Luz Direccional - Intensidad: {intensidadSol}, Rot: {rotacionSol.eulerAngles}");
        }
    }

    // ... (Tu código original de ObtenerPrefabRecolectable, ObtenerStockTienda, ConsumirStockTienda, AnadirStockTienda)
    public GameObject ObtenerPrefabRecolectable(string claveIngrediente)
    {
        if (catalogoMaestro == null)
        {
            Debug.LogError("GestorJuego: El ItemCatalog no está asignado o inicializado.");
            return null;
        }

        ItemCatalog.ItemData data = catalogoMaestro.GetItemData(claveIngrediente);

        if (data == null)
        {
            return null;
        }

        if (data.tipoDeItem != ItemCatalog.TipoDeItem.INGREDIENTE)
        {
            Debug.LogWarning($"Catálogo: '{claveIngrediente}' existe, pero no es de tipo INGREDIENTE. Tipo actual: {data.tipoDeItem}.");
            return null;
        }

        if (data.prefabRecolectable == null)
        {
            Debug.LogWarning($"Catálogo: '{claveIngrediente}' es INGREDIENTE, pero no tiene 'Prefab Recolectable' asignado en el ItemCatalog.");
            return null;
        }

        return data.prefabRecolectable;
    }


    public int ObtenerStockTienda(string nombreIngrediente)
    {
        if (string.IsNullOrEmpty(nombreIngrediente)) return 0;

        if (stockIngredientesTienda.TryGetValue(nombreIngrediente, out int cantidad))
        {
            return cantidad;
        }
        return 0;
    }

    public bool ConsumirStockTienda(string nombreIngrediente)
    {
        if (string.IsNullOrEmpty(nombreIngrediente))
        {
            Debug.LogWarning("Intento de consumir ingrediente con nombre vacío.");
            return false;
        }

        if (stockIngredientesTienda.TryGetValue(nombreIngrediente, out int cantidadActual) && cantidadActual > 0)
        {
            stockIngredientesTienda[nombreIngrediente]--;
            Debug.Log($"Consumido 1 de {nombreIngrediente} del stock. Quedan: {stockIngredientesTienda[nombreIngrediente]}");
            return true;
        }
        Debug.LogWarning($"No se pudo consumir '{nombreIngrediente}'. Stock insuficiente o no encontrado.");
        return false;
    }

    public void AnadirStockTienda(string nombreIngrediente, int cantidadAAnadir)
    {
        if (string.IsNullOrEmpty(nombreIngrediente))
        {
            Debug.LogWarning("Intento de añadir stock para un tipo de ingrediente con nombre vacío.");
            return;
        }
        if (cantidadAAnadir <= 0)
        {
            Debug.LogWarning($"Intento de añadir una cantidad no positiva ({cantidadAAnadir}) de {nombreIngrediente}.");
            return;
        }

        if (stockIngredientesTienda.ContainsKey(nombreIngrediente))
        {
            stockIngredientesTienda[nombreIngrediente] += cantidadAAnadir;
        }
        else
        {
            stockIngredientesTienda.Add(nombreIngrediente, cantidadAAnadir);
            Debug.LogWarning($"Ingrediente '{nombreIngrediente}' no estaba en el stock inicial, añadido ahora.");
        }
        Debug.Log($"Añadido +{cantidadAAnadir} de {nombreIngrediente} al stock. Nuevo total: {stockIngredientesTienda[nombreIngrediente]}");
    }

    // --- GUARDADO / CARGADO ---
    // ... (Tu código original de GuardarDatos, CargarDatos, InicializarValoresPorDefecto, SetSiguientePuntoSpawn)
    public void GuardarDatos()
    {
        Debug.LogWarning($"--- GUARDANDO DATOS --- Día: {diaActual}, Hora: {horaActual}, Dinero: {dineroActual}");
        PlayerPrefs.SetInt("ExisteGuardado", 1);
        PlayerPrefs.SetInt("DiaActual", diaActual);
        PlayerPrefs.SetInt("DineroActual", dineroActual);
        PlayerPrefs.SetInt("HoraActual", (int)horaActual);

        StockDataWrapper stockWrapper = new StockDataWrapper();
        foreach (var kvp in stockIngredientesTienda)
        {
            stockWrapper.stockList.Add(new StockEntry { ingredienteAssetName = kvp.Key, cantidad = kvp.Value });
        }
        string stockJson = JsonUtility.ToJson(stockWrapper);
        PlayerPrefs.SetString("StockIngredientes", stockJson);

        PlayerPrefs.Save();
        Debug.LogWarning("--- DATOS GUARDADOS ---");
    }

    private void CargarDatos()
    {
        Debug.LogWarning("--- INICIANDO CARGA DE DATOS ---");

        InicializarValoresPorDefecto(mantenerDatosDeGuardado: true);

        if (!PlayerPrefs.HasKey("ExisteGuardado") || PlayerPrefs.GetInt("ExisteGuardado") == 0)
        {
            Debug.LogWarning("--- NUEVA PARTIDA / NO EXISTE GUARDADO. Usando valores por defecto ---");
            return;
        }

        diaActual = PlayerPrefs.GetInt("DiaActual", 1);
        dineroActual = PlayerPrefs.GetInt("DineroActual", 50);
        horaActual = (HoraDelDia)PlayerPrefs.GetInt("HoraActual", (int)HoraDelDia.Manana);

        string stockJson = PlayerPrefs.GetString("StockIngredientes", "{}");
        StockDataWrapper stockWrapper = JsonUtility.FromJson<StockDataWrapper>(stockJson);

        if (stockWrapper?.stockList != null && stockWrapper.stockList.Count > 0)
        {
            foreach (var entry in stockWrapper.stockList)
            {
                string nombreIngrediente = entry.ingredienteAssetName;

                if (stockIngredientesTienda.ContainsKey(nombreIngrediente))
                {
                    stockIngredientesTienda[nombreIngrediente] = entry.cantidad;
                }
                else
                {
                    stockIngredientesTienda.Add(nombreIngrediente, entry.cantidad);
                }
            }
            Debug.Log($"Stock cargado y actualizado con {stockWrapper.stockList.Count} entradas guardadas.");
        }
        else
        {
            Debug.LogWarning("No se pudo deserializar stock o stock guardado vacío. Usando Stock Inicial por defecto.");
        }

        Debug.Log($"Datos Cargados - Día: {diaActual}, Dinero: {dineroActual}, Hora: {horaActual}");

        nombrePuntoSpawnSiguiente = "SpawnInicialCama";
        Debug.LogWarning("--- DATOS CARGADOS FINALIZADOS ---");
    }

    private void InicializarValoresPorDefecto(bool mantenerDatosDeGuardado = false)
    {
        if (!mantenerDatosDeGuardado)
        {
            Debug.Log("Inicializando valores por defecto para Nueva Partida...");
            diaActual = 1;
            dineroActual = 50;
            horaActual = HoraDelDia.Manana;
            nombrePuntoSpawnSiguiente = "SpawnInicialCama";
            npcsGeneradosHoy = 0;
        }

        stockIngredientesTienda.Clear();
        if (configuracionStockInicial != null)
        {
            foreach (var config in configuracionStockInicial)
            {
                if (!string.IsNullOrEmpty(config.claveIngrediente))
                {
                    string nombre = config.claveIngrediente;
                    if (!stockIngredientesTienda.ContainsKey(nombre))
                    {
                        stockIngredientesTienda.Add(nombre, config.stockInicial);
                        Debug.Log($"Stock Inicializado: {nombre} con {config.stockInicial}");
                    }
                }
            }
        }
        Debug.Log($"Stock inicializado por defecto con {stockIngredientesTienda.Count} tipos.");

        if (!mantenerDatosDeGuardado)
        {
            PlayerPrefs.DeleteKey("ExisteGuardado");
            PlayerPrefs.Save();
        }
    }

    public void SetSiguientePuntoSpawn(string nombrePunto)
    {
        if (!string.IsNullOrEmpty(nombrePunto))
        {
            nombrePuntoSpawnSiguiente = nombrePunto;
            Debug.Log($"Siguiente punto de spawn fijado a: {nombrePuntoSpawnSiguiente}");
        }
        else
        {
            Debug.LogWarning("Se intentó fijar un nombre de punto de spawn vacío. Se usará el anterior o por defecto.");
        }
    }
}