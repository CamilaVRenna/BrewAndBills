using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;

// Asegúrate de que TimeManager.cs esté presente y contenga OnNewDaySequenceStarted

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
    // ESTADO Y CONFIGURACIÓN DEL JUEGO
    // =========================================================================

    [Header("Catálogo de Datos Centralizado")]
    public ItemCatalog catalogoMaestro;

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
    public TMPro.TextMeshProUGUI textoMielesRecolectadas;

    [Header("Audio Ambiente")]
    public AudioClip musicaMenu;
    public AudioClip audioDia;
    public AudioClip audioNoche;

    private Light luzDireccionalPrincipal = null;

    // ❌ ELIMINADAS: Variables y lógica de límites diarios de NPC.

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

            if (catalogoMaestro != null)
            {
                catalogoMaestro.Initialize();
            }
            else
            {
                Debug.LogError("[GestorJuego] FATAL: ItemCatalog no asignado en el Inspector.");
            }

            // CRUCIAL: Cargar datos para restaurar o inicializar una partida.
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
            TimeManager.Instance.OnNewDaySequenceStarted -= OnNewDaySequenceStarted;
        }
    }

    void Start()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayStart += OnNewDayStarted;
            TimeManager.Instance.OnNightStart += OnNewNightStarted;
            TimeManager.Instance.OnNewDaySequenceStarted += OnNewDaySequenceStarted;
        }
        else
        {
            Debug.LogError("FATAL: TimeManager.Instance sigue siendo null en GestorJuego.Start().");
        }
        ActualizarAparienciaCiclo(true);
    }

    public static void CargarEscenaConPantallaDeCarga(string nombreEscenaACargar)
    {
        if (string.IsNullOrEmpty(nombreEscenaACargar)) return;

        // Guardar antes de cargar, solo si hay una instancia
        if (GestorJuego.Instance != null)
            GestorJuego.Instance.GuardarDatos();

        SceneManager.LoadScene("PantallaCarga");
    }

    void EscenaCargada(Scene escena, LoadSceneMode modo)
    {
        if (escena.name.Contains("Carga") || escena.name.Contains("Menu")) return;

        ActualizarAparienciaCiclo(true);

        // Buscar Luz Direccional (el "sol")
        luzDireccionalPrincipal = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
        if (luzDireccionalPrincipal == null) Debug.LogWarning("No se encontró luz direccional principal.");

        // Buscar referencias de escena
        gestorUI = FindObjectOfType<GestorUI>();
        gestorNPCs = FindObjectOfType<GestorCompradores>();

        // Lógica de Spawn del Jugador
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null)
        {
            PuntoSpawn[] puntos = FindObjectsOfType<PuntoSpawn>();
            PuntoSpawn puntoDestino = puntos.FirstOrDefault(p => p != null && p.nombreIdentificador == nombrePuntoSpawnSiguiente);

            if (puntoDestino == null) puntoDestino = puntos.FirstOrDefault(p => p != null && p.nombreIdentificador == "SpawnInicialCama");

            if (puntoDestino != null)
            {
                CharacterController cc = jugador.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                jugador.transform.position = puntoDestino.transform.position;
                jugador.transform.rotation = puntoDestino.transform.rotation;
                if (cc != null) cc.enabled = true;
                jugador.ResetearVistaVertical();
            }
            else
            {
                Debug.LogError("¡No se encontró NINGÚN PuntoSpawn para posicionar al jugador!");
            }
        }

        // Lógica de Audio
        AudioClip clipPoner = null;
        if (escena.name.Contains("Menu")) clipPoner = musicaMenu;
        else if (horaActual == HoraDelDia.Noche) clipPoner = audioNoche;
        else clipPoner = audioDia;

        if (GestorAudio.Instancia != null) GestorAudio.Instancia.CambiarMusicaFondo(clipPoner);
    }

    // --- MÉTODOS para manejar los eventos del TimeManager ---

    private void OnNewDaySequenceStarted()
    {
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null)
        {
            jugador.HabilitarMovimiento(false);
        }

        GuardarDatos();

        if (TimeManager.Instance != null && !TimeManager.Instance.durmioManualmente)
        {
            if (gestorUI != null) gestorUI.MostrarMensajeTemporal("¡Te desmayaste por el cansancio!", 3f);
        }
    }

    private void OnNewDayStarted()
    {
        if (TimeManager.Instance != null)
        {
            diaActual = TimeManager.Instance.currentDay;
        }
        horaActual = HoraDelDia.Manana;

        // Limpia NPCs de la escena (solo cola/ventana, no hay contadores diarios)
        if (gestorNPCs != null) gestorNPCs.ReiniciarParaNuevoDia();

        ActualizarAparienciaCiclo(true);
        if (GestorAudio.Instancia != null) GestorAudio.Instancia.CambiarMusicaFondo(audioDia);

        // Ejemplo: Mostrar/Ocultar cartel de Abierto/Cerrado
        GameObject cartel = GameObject.Find("cartel");
        if (cartel != null) cartel.SetActive(true);

        // Reposicionamiento del jugador a la cama
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
                jugador.HabilitarMovimiento(true);
            }
        }
    }

    private void OnNewNightStarted()
    {
        horaActual = HoraDelDia.Noche;
        ActualizarAparienciaCiclo(true);
        if (GestorAudio.Instancia != null) GestorAudio.Instancia.CambiarMusicaFondo(audioNoche);

        CerrarTiendaPorNoche();

        GameObject cartel = GameObject.Find("cartel");
        if (cartel != null) cartel.SetActive(false);
    }

    public void CerrarTiendaPorNoche()
    {
        // Usa el método unificado CerrarTienda del gestor de compradores
        if (gestorNPCs != null)
        {
            gestorNPCs.CerrarTienda();
        }
    }

    // --- MÉTODOS DE ESTADO Y CONSULTA ---

    public bool EstaDeDia()
    {
        return horaActual == HoraDelDia.Manana || horaActual == HoraDelDia.Tarde;
    }

    public bool PuedeDormir()
    {
        return horaActual == HoraDelDia.Noche;
    }

    // [Omisión de AnadirDinero, IrADormir, ActualizarAparienciaCiclo, ObtenerPrefabRecolectable, 
    // ObtenerStockTienda, ConsumirStockTienda, AnadirStockTienda, SetSiguientePuntoSpawn por brevedad,
    // ya que no fueron modificados y son correctos.]

    // Métodos para evitar errores de compilación si otras clases llaman:
    // (Estos ahora son obsoletos/innecesarios según la nueva lógica, pero se dejan para compatibilidad)
    [System.Obsolete("El sistema de NPCs ya no usa límites diarios. Siempre retorna 0.")]
    public int ObtenerNPCsGeneradosHoy() => 0;

    [System.Obsolete("El sistema de NPCs ya no usa límites diarios. No hace nada.")]
    public void RegistrarNPCGeneradoHoy() { /* No action needed */ }

    // =========================================================================
    // GUARDADO / CARGADO Y NUEVA PARTIDA
    // =========================================================================

    public void IniciarNuevaPartida()
    {
        InicializarValoresPorDefecto(esCargaDeDatos: false);
    }

    public void GuardarDatos()
    {
        // 1. Guardar variables de estado
        PlayerPrefs.SetInt("ExisteGuardado", 1);
        PlayerPrefs.SetInt("DiaActual", diaActual);
        PlayerPrefs.SetInt("DineroActual", dineroActual);
        PlayerPrefs.SetInt("HoraActual", (int)horaActual);
        // ELIMINADO: No se guarda el contador de NPCs

        // 2. Serializar el Stock (Diccionario)
        StockDataWrapper stockWrapper = new StockDataWrapper();
        foreach (var kvp in stockIngredientesTienda)
        {
            stockWrapper.stockList.Add(new StockEntry { ingredienteAssetName = kvp.Key, cantidad = kvp.Value });
        }
        string stockJson = JsonUtility.ToJson(stockWrapper);
        PlayerPrefs.SetString("StockIngredientes", stockJson);

        // 3. Persistir los datos
        PlayerPrefs.Save();
    }

    private void CargarDatos()
    {
        bool existeGuardado = PlayerPrefs.HasKey("ExisteGuardado") && PlayerPrefs.GetInt("ExisteGuardado") == 1;

        if (!existeGuardado)
        {
            InicializarValoresPorDefecto(esCargaDeDatos: false);
            return;
        }

        InicializarValoresPorDefecto(esCargaDeDatos: true);

        // Cargar y SOBREESCRIBIR valores básicos guardados
        diaActual = PlayerPrefs.GetInt("DiaActual", 1);
        dineroActual = PlayerPrefs.GetInt("DineroActual", 50);
        horaActual = (HoraDelDia)PlayerPrefs.GetInt("HoraActual", (int)HoraDelDia.Manana);
        // ELIMINADO: No se carga el contador de NPCs

        // Cargar y SOBREESCRIBIR el Stock guardado
        string stockJson = PlayerPrefs.GetString("StockIngredientes", "{}");
        StockDataWrapper stockWrapper = JsonUtility.FromJson<StockDataWrapper>(stockJson);

        if (stockWrapper?.stockList != null && stockWrapper.stockList.Count > 0)
        {
            foreach (var entry in stockWrapper.stockList)
            {
                // Asegurar que solo se carguen elementos con nombres válidos para evitar errores
                if (!string.IsNullOrEmpty(entry.ingredienteAssetName))
                {
                    stockIngredientesTienda[entry.ingredienteAssetName] = entry.cantidad;
                }
            }
        }

        nombrePuntoSpawnSiguiente = "SpawnInicialCama";
    }

    private void InicializarValoresPorDefecto(bool esCargaDeDatos)
    {
        if (!esCargaDeDatos)
        {
            diaActual = 1;
            dineroActual = 50;
            horaActual = HoraDelDia.Manana;
            nombrePuntoSpawnSiguiente = "SpawnInicialCama";

            // Limpieza de PlayerPrefs para Nueva Partida
            PlayerPrefs.DeleteKey("ExisteGuardado");
            PlayerPrefs.DeleteKey("DiaActual");
            PlayerPrefs.DeleteKey("DineroActual");
            PlayerPrefs.DeleteKey("StockIngredientes");
            PlayerPrefs.DeleteKey("HoraActual");
            // ELIMINADA: La limpieza de NPCsGeneradosHoy
            PlayerPrefs.Save();
        }

        // (Re)Inicializar el Diccionario de Stock con la configuración del Inspector
        stockIngredientesTienda.Clear();
        if (configuracionStockInicial != null)
        {
            foreach (var config in configuracionStockInicial)
            {
                if (!string.IsNullOrEmpty(config.claveIngrediente) && !stockIngredientesTienda.ContainsKey(config.claveIngrediente))
                {
                    stockIngredientesTienda.Add(config.claveIngrediente, config.stockInicial);
                }
            }
        }
    }

    public void SetSiguientePuntoSpawn(string nombrePunto)
    {
        if (!string.IsNullOrEmpty(nombrePunto))
        {
            nombrePuntoSpawnSiguiente = nombrePunto;
        }
    }

    // -------------------------------------------------------------------------
    // MÉTODOS DE ECONOMÍA Y STOCK (dejados para referencia)
    // -------------------------------------------------------------------------

    public void AnadirDinero(int cantidad)
    {
        dineroActual += cantidad;
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
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RegistrarDormir();
        }
    }

    public GameObject ObtenerPrefabRecolectable(string claveIngrediente)
    {
        if (catalogoMaestro == null) return null;
        ItemCatalog.ItemData data = catalogoMaestro.GetItemData(claveIngrediente);
        if (data == null || data.tipoDeItem != ItemCatalog.TipoDeItem.INGREDIENTE) return null;
        return data.prefabRecolectable;
    }

    public int ObtenerStockTienda(string nombreIngrediente)
    {
        if (string.IsNullOrEmpty(nombreIngrediente)) return 0;
        stockIngredientesTienda.TryGetValue(nombreIngrediente, out int cantidad);
        return cantidad;
    }

    public bool ConsumirStockTienda(string nombreIngrediente)
    {
        if (stockIngredientesTienda.TryGetValue(nombreIngrediente, out int cantidadActual) && cantidadActual > 0)
        {
            stockIngredientesTienda[nombreIngrediente]--;
            return true;
        }
        return false;
    }

    public void AnadirStockTienda(string nombreIngrediente, int cantidadAAnadir)
    {
        if (string.IsNullOrEmpty(nombreIngrediente) || cantidadAAnadir <= 0) return;

        if (stockIngredientesTienda.ContainsKey(nombreIngrediente))
        {
            stockIngredientesTienda[nombreIngrediente] += cantidadAAnadir;
        }
        else
        {
            stockIngredientesTienda.Add(nombreIngrediente, cantidadAAnadir);
        }
    }

    void ActualizarAparienciaCiclo(bool instantaneo = false)
    {
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
                break;
            case HoraDelDia.Tarde:
                skyboxAplicar = skyboxTarde;
                luzAmbiente = new Color(0.7f, 0.6f, 0.55f);
                rotacionSol = Quaternion.Euler(20f, -150f, 0f);
                intensidadSol = 0.75f;
                break;
            case HoraDelDia.Noche:
                skyboxAplicar = skyboxNoche;
                luzAmbiente = new Color(0.1f, 0.1f, 0.18f);
                rotacionSol = Quaternion.Euler(-30f, -90f, 0f);
                intensidadSol = 0.08f;
                break;
        }

        if (skyboxAplicar != null) { RenderSettings.skybox = skyboxAplicar; DynamicGI.UpdateEnvironment(); }
        RenderSettings.ambientLight = luzAmbiente;

        if (luzDireccionalPrincipal != null)
        {
            luzDireccionalPrincipal.intensity = intensidadSol;
            luzDireccionalPrincipal.transform.rotation = rotacionSol;
        }
    }
}