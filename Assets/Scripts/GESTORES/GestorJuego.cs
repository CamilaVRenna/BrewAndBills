using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public enum HoraDelDia { Manana, Tarde, Noche }

[System.Serializable]
public class StockInicialIngrediente
{
    // AHORA USA UN STRING (la clave) que coincide con el 'nombreItem' de ItemCatalog.
    [Tooltip("La clave del ingrediente (string) del ItemCatalog.")]
    public string claveIngrediente; // <-- CORREGIDO
    public int stockInicial = 5;
}

[System.Serializable]
public class StockEntry
{
    // Usa el nombre del ítem (string) para la serialización del guardado.
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
    [Tooltip("Arrastra aquí tu ScriptableObject ItemCatalog para acceder a todos los datos de ítems.")]
    public ItemCatalog catalogoMaestro; // <-- CAMPO CRUCIAL

    // =========================================================================
    // ESTADO Y CONFIGURACIÓN DEL JUEGO
    // =========================================================================

    [Header("Límites Diarios")]
    public int limiteNPCsPorDia = 5;
    private int npcsGeneradosHoy = 0;

    [Header("Configuración Guardado y Spawn")]
    [Tooltip("Punto donde aparece el jugador al INICIO DEL DÍA (Empty GO cerca de la cama)")]
    private string nombrePuntoSpawnSiguiente = "SpawnInicialCama";

    [Header("Inventario/Stock Ingredientes")]
    // Usa la nueva estructura StockInicialIngrediente basada en string
    public List<StockInicialIngrediente> configuracionStockInicial;

    // MODIFICADO: Ahora el diccionario usa 'string' (el nombre del ítem) como clave.
    public Dictionary<string, int> stockIngredientesTienda = new Dictionary<string, int>();

    [Header("Estado del Juego")]
    public int diaActual = 1;
    public int dineroActual = 50;
    public HoraDelDia horaActual = HoraDelDia.Manana;

    [Header("Ciclo Día/Noche")]
    [Tooltip("Material Skybox para la mañana")]
    public Material skyboxManana;
    [Tooltip("Material Skybox para la tarde")]
    public Material skyboxTarde;
    [Tooltip("Material Skybox para la noche")]
    public Material skyboxNoche;

    [Header("Economía")]
    public int valorPocionCorrecta = 5;

    [Header("Referencias UI y Efectos")]
    public GestorUI gestorUI;
    public AudioClip sonidoGanarDinero;
    public AudioClip sonidoPerderDinero;
    public GestorCompradores gestorNPCs;
    public TMPro.TextMeshProUGUI textoMielesRecolectadas;

    [Header("Audio Ambiente")]
    [Tooltip("Música o sonido para el MenuPrincipal")]
    public AudioClip musicaMenu;
    [Tooltip("Música o sonido ambiente para el día (Mañana/Tarde)")]
    public AudioClip audioDia;
    [Tooltip("Música o sonido ambiente para la noche (grillos?)")]
    public AudioClip audioNoche;

    private Light luzDireccionalPrincipal = null;
    private bool durmiendo = false;

    // =========================================================================
    // CICLO DE VIDA Y EVENTOS
    // =========================================================================

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 1. Inicializa el Catálogo antes de cargar cualquier dato que dependa de él.
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
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStart -= OnNewDayStarted;
            TimeManager.Instance.OnNightStart -= OnNewNightStarted;
            TimeManager.Instance.OnNightEnd -= OnNightEnded;
        }
    }

    void Start()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStart += OnNewDayStarted;
            TimeManager.Instance.OnNightStart += OnNewNightStarted;
            TimeManager.Instance.OnNightEnd += OnNightEnded;
        }
        else
        {
            Debug.LogError("FATAL: TimeManager.Instance sigue siendo null en GestorJuego.Start().");
        }
        ActualizarAparienciaCiclo(true);
        Debug.Log("GestorJuego iniciado, Skybox inicial aplicado.");
    }

    public static void CargarEscenaConPantallaDeCarga(string nombreEscenaACargar)
    {
        if (string.IsNullOrEmpty(nombreEscenaACargar))
        {
            Debug.LogError("Se intentó cargar una escena con nombre vacío.");
            return;
        }
        // Asume que ControladorPantallaCarga.escenaACargar está definido en otro lugar
        // ControladorPantallaCarga.escenaACargar = nombreEscenaACargar;
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

    // --- MÉTODOS para manejar los eventos del TimeManager ---
    private void OnNewDayStarted()
    {
        Debug.Log("GestorJuego: ¡El TimeManager ha iniciado un nuevo día!");
        diaActual = TimeManager.Instance.currentDay;
        horaActual = HoraDelDia.Manana;

        GuardarDatos();
        npcsGeneradosHoy = 0;
        if (gestorNPCs != null) gestorNPCs.ReiniciarParaNuevoDia();

        ActualizarAparienciaCiclo(true);
        if (GestorAudio.Instancia != null) GestorAudio.Instancia.CambiarMusicaFondo(audioDia);
        if (gestorUI != null) gestorUI.MostrarInicioDia(diaActual);
        GameObject cartel = GameObject.Find("cartel");
        if (cartel != null) cartel.SetActive(true);

        // Lógica de spawn del jugador cerca de la cama
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
                jugador.GetComponent<ControladorJugador>()?.ResetearVistaVertical();
            }
            else { Debug.LogError("¡No se encontró 'SpawnInicialCama'!"); }
        }
        else { Debug.LogError("No se encontró jugador para reposicionar."); }

        ControladorJugador jugadorFinal = FindObjectOfType<ControladorJugador>();
        if (jugadorFinal != null) jugadorFinal.HabilitarMovimiento(true);
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

    private void OnNightEnded()
    {
        // Verifica si el jugador durmió manualmente usando el flag del TimeManager
        if (TimeManager.Instance.durmioManualmente)
        {
            Debug.Log("GestorJuego: La noche terminó porque el jugador durmió. Pasando al nuevo día.");
            // Si el jugador durmió, la secuencia de dormir se encarga de la transición
            StartCoroutine(SecuenciaDormir());
        }
        else
        {
            Debug.Log("GestorJuego: ¡El jugador no durmió! Ejecutando secuencia de desmayo.");
            StartCoroutine(SecuenciaDesmayo());
        }
    }

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
        if (durmiendo) return;

        Debug.Log("Intentando ir a dormir (llamado desde interacción)...");

        TimeManager.Instance.RegistrarDormir();
    }

    private IEnumerator SecuenciaDormir()
    {
        if (durmiendo) yield break;
        durmiendo = true;

        try
        {
            Debug.Log("Iniciando secuencia de sueño...");
            ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
            if (jugador != null) jugador.HabilitarMovimiento(false);
            if (gestorUI != null) yield return StartCoroutine(gestorUI.FundidoANegro());

            if (gestorUI != null) yield return StartCoroutine(gestorUI.FundidoDesdeNegro());
            if (jugador != null) jugador.HabilitarMovimiento(true);

            Debug.Log("Secuencia de sueño completada.");
        }
        finally
        {
            durmiendo = false;
        }
    }

    private IEnumerator SecuenciaDesmayo()
    {
        Debug.Log("¡El jugador se desmayó!");

        if (gestorUI != null)
        {
            gestorUI.MostrarMensajeFlotante("Te desmayaste...", 3f);
        }

        yield return new WaitForSeconds(3f);

        StartCoroutine(SecuenciaDormir());
    }

    // --- MÉTODOS DE ESTADO ---

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

    // =========================================================================
    // CÓDIGO CLAVE: Obtener Prefab desde el Catálogo
    // =========================================================================

    /// <summary>
    /// Utiliza el ItemCatalog centralizado para obtener el GameObject Prefab
    /// de la versión RECOLECTABLE de un ingrediente, dada su clave.
    /// </summary>
    /// <param name="claveIngrediente">La clave (string) del ingrediente.</param>
    /// <returns>El Prefab del GameObject recolectable, o null si no existe o no es un ingrediente.</returns>
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
            // Debug.LogWarning($"Catálogo: No se encontró ItemData para la clave: '{claveIngrediente}'."); // Evitar spam si el spawn falla intencionalmente
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


    // =========================================================================
    // MÉTODOS DE STOCK (USANDO STRING)
    // =========================================================================

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
            // Opcional: podrías validar que exista en el ItemCatalog aquí.
            stockIngredientesTienda.Add(nombreIngrediente, cantidadAAnadir);
            Debug.LogWarning($"Ingrediente '{nombreIngrediente}' no estaba en el stock inicial, añadido ahora.");
        }
        Debug.Log($"Añadido +{cantidadAAnadir} de {nombreIngrediente} al stock. Nuevo total: {stockIngredientesTienda[nombreIngrediente]}");
    }

    // =========================================================================
    // GUARDADO / CARGADO
    // =========================================================================

    public void GuardarDatos()
    {
        Debug.LogWarning($"--- GUARDANDO DATOS --- Día: {diaActual}, Hora: {horaActual}, Dinero: {dineroActual}");
        PlayerPrefs.SetInt("ExisteGuardado", 1);
        PlayerPrefs.SetInt("DiaActual", diaActual);
        PlayerPrefs.SetInt("DineroActual", dineroActual);
        PlayerPrefs.SetInt("HoraActual", (int)horaActual);

        StockDataWrapper stockWrapper = new StockDataWrapper();
        // Usa el string (nombre del ítem) como clave para guardar
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

        // 1. Siempre inicializar el stock con la CONFIGURACIÓN INICIAL por defecto.
        InicializarValoresPorDefecto(mantenerDatosDeGuardado: true);

        if (!PlayerPrefs.HasKey("ExisteGuardado") || PlayerPrefs.GetInt("ExisteGuardado") == 0)
        {
            Debug.LogWarning("--- NUEVA PARTIDA / NO EXISTE GUARDADO. Usando valores por defecto ---");
            return;
        }

        // Si existe guardado, sobreescribimos los valores por defecto con los guardados

        // Carga de datos simples
        diaActual = PlayerPrefs.GetInt("DiaActual", 1);
        dineroActual = PlayerPrefs.GetInt("DineroActual", 50);
        horaActual = (HoraDelDia)PlayerPrefs.GetInt("HoraActual", (int)HoraDelDia.Manana);

        // 2. Cargar Stock GUARDADO y SOBREESCRIBIR los valores por defecto.
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
                    // Si es un nuevo ítem que no estaba en la lista inicial, lo añadimos.
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
        if (!mantenerDatosDeGuardado) // Solo si es una partida totalmente nueva
        {
            Debug.Log("Inicializando valores por defecto para Nueva Partida...");
            diaActual = 1;
            dineroActual = 50;
            horaActual = HoraDelDia.Manana;
            nombrePuntoSpawnSiguiente = "SpawnInicialCama";
            npcsGeneradosHoy = 0;
        }

        // Siempre inicializar el diccionario de stock con la configuración de la lista
        stockIngredientesTienda.Clear();
        if (configuracionStockInicial != null)
        {
            foreach (var config in configuracionStockInicial)
            {
                // CORRECCIÓN: Usamos directamente la 'claveIngrediente' del string.
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