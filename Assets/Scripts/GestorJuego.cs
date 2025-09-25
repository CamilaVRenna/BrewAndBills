using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public enum HoraDelDia { Manana, Tarde, Noche }

[System.Serializable]
public class StockInicialIngrediente
{
    public DatosIngrediente ingrediente;
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
    public bool interactuoConCueva = false;

    [Header("Límites Diarios")]
    public int limiteNPCsPorDia = 5;
    private int npcsGeneradosHoy = 0;

    [Header("Configuración Guardado y Spawn")]
    [Tooltip("Punto donde aparece el jugador al INICIO DEL DÍA (Empty GO cerca de la cama)")]
    private string nombrePuntoSpawnSiguiente = "SpawnInicialCama";

    [Header("Inventario/Stock Ingredientes")]
    public List<StockInicialIngrediente> configuracionStockInicial;
    public Dictionary<DatosIngrediente, int> stockIngredientesTienda = new Dictionary<DatosIngrediente, int>();

    private bool durmiendo = false;
    public static GestorJuego Instance { get; private set; }

    public static void CargarEscenaConPantallaDeCarga(string nombreEscenaACargar)
    {
        if (string.IsNullOrEmpty(nombreEscenaACargar))
        {
            Debug.LogError("Se intentó cargar una escena con nombre vacío.");
            return;
        }
        ControladorPantallaCarga.escenaACargar = nombreEscenaACargar;
        if (GestorJuego.Instance != null)
            GestorJuego.Instance.GuardarDatos();
        SceneManager.LoadScene("PantallaCarga");
    }

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CargarDatos();
        }
        else { Destroy(gameObject); }
    }

    void OnEnable()
    {
        Debug.Log(">>> GESTOR JUEGO ON ENABLE - Suscribiendo a sceneLoaded y eventos de tiempo <<<");
        SceneManager.sceneLoaded += EscenaCargada;
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStart += OnNewDayStarted;
            TimeManager.Instance.OnNightEnd += OnNightEnded;
        }
        else
        {
            Debug.LogError("No se encontró TimeManager.Instance. Los eventos de ciclo de tiempo no se activarán.");
        }
        Debug.Log("GestorJuego suscrito a sceneLoaded.");
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= EscenaCargada;
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStart -= OnNewDayStarted;
            TimeManager.Instance.OnNightEnd -= OnNightEnded;
        }
        Debug.Log("GestorJuego desuscrito de sceneLoaded.");
    }

    [Header("Estado del Juego")]
    public int diaActual = 1;
    public int dineroActual = 50;

    [Header("Ciclo Día/Noche")]
    public HoraDelDia horaActual = HoraDelDia.Manana;
    [Tooltip("Material Skybox para la mañana")]
    public Material skyboxManana;
    [Tooltip("Material Skybox para la tarde")]
    public Material skyboxTarde;
    [Tooltip("Material Skybox para la noche")]
    public Material skyboxNoche;

    [Header("Economía")]
    public int valorPocionCorrecta = 5;
    public int costoRentaDiaria = 10;

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

    [Header("Progreso Cueva")]
    public int abejasMatadasCueva = 0;
    public bool misionCompleta = false;

    private Light luzDireccionalPrincipal = null;

    void Start()
    {
        ActualizarAparienciaCiclo(true);
        Debug.Log("GestorJuego iniciado, Skybox inicial aplicado.");
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

        if (gestorUI != null)
        {
            if (interactuoConCueva)
            {
                if (misionCompleta)
                {
                    gestorUI.ActualizarTextoMieles("¡Ya podés ir a la cueva!");
                }
                else
                {
                    gestorUI.ActualizarTextoMieles($"{abejasMatadasCueva}/3 mieles recolectadas");
                }
            }
            else
            {
                gestorUI.ActualizarTextoMieles("Mision");
            }
            Debug.Log("GestorUI encontrado en EscenaCargada. Actualizando UI.");
            gestorUI.ActualizarUIDinero(dineroActual);
            if (horaActual != HoraDelDia.Noche)
            {
                Debug.Log($"Mostrando UI del Día {diaActual} porque es {horaActual}");
                gestorUI.MostrarInicioDia(diaActual);
            }
            else
            {
                Debug.Log($"No se muestra UI del Día porque es {horaActual}");
            }
        }
        else if (escena.name == "TiendaDeMagia" || escena.name == "Bosque" || escena.name == "MainMenu")
        {
            Debug.LogWarning($"GestorUI no encontrado en la escena {escena.name}. ¿Falta el objeto UIManager o el script?");
        }
        if (gestorNPCs == null && escena.name == "TiendaDeMagia")
        {
            Debug.LogWarning($"GestorNPCs no encontrado en la escena {escena.name}.");
        }
    }

    // --- NUEVOS MÉTODOS para manejar los eventos del TimeManager ---
    private void OnNewDayStarted()
    {
        Debug.Log("GestorJuego: ¡El TimeManager ha iniciado un nuevo día!");
        // Aquí pon la lógica que quieres que ocurra al inicio de cada día.
        diaActual = TimeManager.Instance.currentDay;
        horaActual = HoraDelDia.Manana;
        GuardarDatos();
        DeducirRenta();
        npcsGeneradosHoy = 0;
        if (gestorNPCs != null) gestorNPCs.ReiniciarParaNuevoDia();
        ActualizarAparienciaCiclo(true);
        if (GestorAudio.Instancia != null) GestorAudio.Instancia.CambiarMusicaFondo(audioDia);
        if (gestorUI != null) gestorUI.MostrarInicioDia(diaActual);
        GameObject cartel = GameObject.Find("cartel");
        if (cartel != null) cartel.SetActive(true);
    }

    private void OnNewNightStarted()
    {
        Debug.Log("GestorJuego: ¡El TimeManager ha iniciado la noche!");
        // Aquí pon la lógica que quieres que ocurra al inicio de la noche.
        horaActual = HoraDelDia.Noche;
        ActualizarAparienciaCiclo(true);
        if (GestorAudio.Instancia != null) GestorAudio.Instancia.CambiarMusicaFondo(audioNoche);
        gestorNPCs?.DespawnTodosNPCsPorNoche();
        GameObject cartel = GameObject.Find("cartel");
        if (cartel != null) cartel.SetActive(false);
    }

    private void OnNightEnded()
    {
        Debug.Log("GestorJuego: La noche ha terminado, ¡es hora de dormir!");
        // Aquí puedes llamar a tu método de desmayo, si el jugador no ha dormido.
        StartCoroutine(SecuenciaDormir());
    }

    // --- MÉTODOS EXISTENTES CON MODIFICACIONES ---
    public void SumarAbejaMatada()
    {
        abejasMatadasCueva++;
        if (gestorUI != null)
        {
            if (abejasMatadasCueva >= 3)
            {
                gestorUI.ActualizarTextoMieles("¡Ya podés ir a la cueva!");
                misionCompleta = true;
            }
            else
            {
                gestorUI.ActualizarTextoMieles($"{abejasMatadasCueva}/3 mieles recolectadas");
            }
        }
    }

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

    private void DeducirRenta()
    {
        dineroActual -= costoRentaDiaria;
        Debug.Log($"Renta diaria deducida: -{costoRentaDiaria}. Total: {dineroActual}");
        if (gestorUI != null)
        {
            gestorUI.ActualizarUIDinero(dineroActual);
            gestorUI.MostrarCambioDinero(-costoRentaDiaria);
        }
        if (GestorAudio.Instancia != null && sonidoPerderDinero != null)
        {
            GestorAudio.Instancia.ReproducirSonido(sonidoPerderDinero);
        }
    }

    public void IrADormir()
    {
        if (durmiendo)
        {
            Debug.Log("Ya está en proceso de dormir, ignorando petición.");
            return;
        }

        Debug.Log("Intentando ir a dormir (llamado desde interacción)...");
        if (gestorUI != null)
        {
            StartCoroutine(SecuenciaDormir());
        }
        else
        {
            Debug.LogError("No se puede dormir, falta GestorUI.");
        }
    }

    private IEnumerator SecuenciaDormir()
    {
        if (durmiendo) yield break;
        durmiendo = true;

        try
        {
            Debug.Log("Iniciando secuencia de sueño...");
            ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
            if (jugador != null) { jugador.HabilitarMovimiento(false); }
            else { Debug.LogWarning("SecuenciaDormir: No se encontró ControladorJugador para deshabilitar."); }

            if (gestorUI != null) yield return StartCoroutine(gestorUI.FundidoANegro());
            else { Debug.LogWarning("SecuenciaDormir: GestorUI null, no se hará fundido a negro."); }

            // Lógica de avance de día, renta, NPCs, etc. MOVIDA a OnNewDayStarted()

            Debug.Log("Forzando posición del jugador junto a la cama...");
            jugador = FindObjectOfType<ControladorJugador>();
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

            if (gestorUI != null) yield return StartCoroutine(gestorUI.FundidoDesdeNegro());
            else { Debug.LogWarning("SecuenciaDormir: GestorUI null, no se hará fundido desde negro."); }

            jugador = FindObjectOfType<ControladorJugador>();
            if (jugador != null) { jugador.HabilitarMovimiento(true); }
            else { Debug.LogWarning("SecuenciaDormir: No se encontró ControladorJugador para habilitar."); }

            if (gestorUI != null) gestorUI.MostrarInicioDia(diaActual);
            if (interactuoConCueva)
            {
                Debug.Log("INTERACTUASTE CON LA CUEVA");
                if (gestorUI != null)
                    gestorUI.ActualizarTextoMieles("0/3 mieles recolectadas");
                interactuoConCueva = false;
            }
            else
            {
                Debug.Log("Días siguientes");
            }
            GameObject cartel = GameObject.Find("cartel");
            if (cartel != null)
                cartel.SetActive(true);
            Debug.Log("Secuencia de sueño completada.");
        }
        finally
        {
            durmiendo = false;
            Debug.Log("Flag 'durmiendo' puesto a false.");
        }
    }

    public void GuardarDatos()
    {
        Debug.LogWarning($"--- GUARDANDO DATOS --- Día: {diaActual}, Hora: {horaActual}, Dinero: {dineroActual}");
        PlayerPrefs.SetInt("ExisteGuardado", 1);
        PlayerPrefs.SetInt("DiaActual", diaActual);
        PlayerPrefs.SetInt("DineroActual", dineroActual);
        PlayerPrefs.SetInt("HoraActual", (int)horaActual);

        Debug.LogError($"GUARDANDO HoraActual como INT: {(int)horaActual} (Enum: {horaActual})");

        StockDataWrapper stockWrapper = new StockDataWrapper();
        foreach (var kvp in stockIngredientesTienda)
        {
            if (kvp.Key != null) stockWrapper.stockList.Add(new StockEntry { ingredienteAssetName = kvp.Key.name, cantidad = kvp.Value });
        }
        string stockJson = JsonUtility.ToJson(stockWrapper);
        PlayerPrefs.SetString("StockIngredientes", stockJson);
        Debug.Log($"Stock Guardado JSON: {stockJson.Substring(0, Mathf.Min(stockJson.Length, 100))}...");

        PlayerPrefs.Save();
        Debug.LogWarning("--- DATOS GUARDADOS ---");
    }

    private void CargarDatos()
    {
        if (!PlayerPrefs.HasKey("ExisteGuardado") || PlayerPrefs.GetInt("ExisteGuardado") == 0)
        {
            Debug.LogWarning("--- CARGANDO DATOS ---");
            InicializarValoresPorDefecto();
            return;
        }

        diaActual = PlayerPrefs.GetInt("DiaActual", 1);
        dineroActual = PlayerPrefs.GetInt("DineroActual", 50);
        horaActual = (HoraDelDia)PlayerPrefs.GetInt("HoraActual", (int)HoraDelDia.Manana);
        Debug.LogError($"CARGANDO HoraActual como INT: {PlayerPrefs.GetInt("HoraActual", -1)}, Convertido a Enum: {horaActual}");
        Debug.LogError($"--- HORA CARGADA DE PLAYERPREFS: {horaActual} ---");

        stockIngredientesTienda = new Dictionary<DatosIngrediente, int>();
        string stockJson = PlayerPrefs.GetString("StockIngredientes", "{}");
        StockDataWrapper stockWrapper = JsonUtility.FromJson<StockDataWrapper>(stockJson);
        if (stockWrapper?.stockList != null)
        {
            foreach (var entry in stockWrapper.stockList)
            {
                string resourcePath = $"Data/Ingredientes/{entry.ingredienteAssetName}";
                DatosIngrediente ingredienteAsset = Resources.Load<DatosIngrediente>(resourcePath);
                if (ingredienteAsset != null) stockIngredientesTienda[ingredienteAsset] = entry.cantidad;
                else Debug.LogWarning($"No se encontró DatosIngrediente '{entry.ingredienteAssetName}' en 'Resources/{resourcePath}'.");
            }
            Debug.Log($"Stock cargado con {stockIngredientesTienda.Count} tipos.");
        }
        else { Debug.LogWarning("No se pudo deserializar stock."); }

        Debug.Log($"Datos Cargados - Día: {diaActual}, Dinero: {dineroActual}, Hora: {horaActual}");

        nombrePuntoSpawnSiguiente = "SpawnInicialCama";
        Debug.LogWarning("--- DATOS CARGADOS ---");
    }

    private void InicializarValoresPorDefecto()
    {
        Debug.Log("Inicializando valores por defecto para Nueva Partida...");
        diaActual = 1;
        dineroActual = 50;
        horaActual = HoraDelDia.Manana;
        nombrePuntoSpawnSiguiente = "SpawnInicialCama";
        npcsGeneradosHoy = 0;

        stockIngredientesTienda = new Dictionary<DatosIngrediente, int>();
        if (configuracionStockInicial != null)
        {
            foreach (var config in configuracionStockInicial)
            {
                if (config?.ingrediente != null && !stockIngredientesTienda.ContainsKey(config.ingrediente))
                {
                    stockIngredientesTienda.Add(config.ingrediente, config.stockInicial);
                }
            }
        }
        Debug.Log($"Stock inicializado por defecto con {stockIngredientesTienda.Count} tipos.");
        PlayerPrefs.DeleteKey("ExisteGuardado");
        PlayerPrefs.Save();
    }

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

    public void RegistrarViaje(string escenaDestino)
    {
        Debug.Log($"Viaje a {escenaDestino} no cambia la hora. La hora actual es {horaActual}");
    }
    public int ObtenerStockTienda(DatosIngrediente tipo)
    {
        if (tipo != null && stockIngredientesTienda.TryGetValue(tipo, out int cantidad))
        {
            return cantidad;
        }
        return 0;
    }
    public bool ConsumirStockTienda(DatosIngrediente tipo)
    {
        if (tipo == null)
        {
            Debug.LogWarning("Intento de consumir ingrediente NULL.");
            return false;
        }
        if (stockIngredientesTienda.TryGetValue(tipo, out int cantidadActual) && cantidadActual > 0)
        {
            stockIngredientesTienda[tipo]--;
            Debug.Log($"Consumido 1 de {tipo.nombreIngrediente} del stock. Quedan: {stockIngredientesTienda[tipo]}");
            return true;
        }
        Debug.LogWarning($"No se pudo consumir '{tipo?.nombreIngrediente ?? "NULL"}'. Stock insuficiente o no encontrado.");
        return false;
    }
    public void AnadirStockTienda(DatosIngrediente tipo, int cantidadAAnadir)
    {
        if (tipo == null)
        {
            Debug.LogWarning("Intento de añadir stock para un tipo de ingrediente NULL.");
            return;
        }
        if (cantidadAAnadir <= 0)
        {
            Debug.LogWarning($"Intento de añadir una cantidad no positiva ({cantidadAAnadir}) de {tipo.nombreIngrediente}.");
            return;
        }
        if (stockIngredientesTienda.ContainsKey(tipo))
        {
            stockIngredientesTienda[tipo] += cantidadAAnadir;
        }
        else
        {
            stockIngredientesTienda.Add(tipo, cantidadAAnadir);
            Debug.LogWarning($"Ingrediente '{tipo.nombreIngrediente}' no estaba en el stock inicial, añadido ahora.");
        }
        Debug.Log($"Añadido +{cantidadAAnadir} de {tipo.nombreIngrediente} al stock. Nuevo total: {stockIngredientesTienda[tipo]}");
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