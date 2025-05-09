using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // <<--- A�ADE ESTA L�NEA
using System.Collections.Generic;
using System.Linq;

public enum HoraDelDia { Manana, Tarde, Noche }

[System.Serializable] // Necesario para que aparezca en el Inspector
public class StockInicialIngrediente
{
    public DatosIngrediente ingrediente;
    public int stockInicial = 5; // Stock por defecto
}

[System.Serializable] // Necesario para que JsonUtility funcione
public class StockEntry
{
    public string ingredienteAssetName; // Guardamos el NOMBRE del asset ScriptableObject
    public int cantidad;
}

[System.Serializable]
public class StockDataWrapper
{
    // JsonUtility no funciona bien con Diccionarios, usamos una Lista de nuestra clase auxiliar
    public List<StockEntry> stockList = new List<StockEntry>();
}
// ------------------------------------------------------

// Dentro de GestorJuego, junto a las otras variables p�blicas/privadas

// Otras variables (reputaci�n, etc., si las tienes)
// public int reputacion = 50;

// Nombre de la clase en espa�ol
public class GestorJuego : MonoBehaviour
{

    [Header("L�mites Diarios")] // Nuevo Header
    public int limiteNPCsPorDia = 5; // L�mite de NPCs a generar por d�a
    private int npcsGeneradosHoy = 0; // Contador interno

    [Header("Configuraci�n Guardado y Spawn")]
    [Tooltip("Punto donde aparece el jugador al INICIO DEL D�A (Empty GO cerca de la cama)")]
    //public Transform puntoSpawnJugadorTienda; // <<--- Asignar en Inspector
    private string nombrePuntoSpawnSiguiente = "SpawnInicialCama"; // Nombre por DEFECTO o al cargar

    [Header("Inventario/Stock Ingredientes")]
    public List<StockInicialIngrediente> configuracionStockInicial; // Configurable en Inspector
    public Dictionary<DatosIngrediente, int> stockIngredientesTienda = new Dictionary<DatosIngrediente, int>();

    private bool durmiendo = false; // <<--- NUEVA VARIABLE FLAG

    // --- Patr�n Singleton ---
    public static GestorJuego Instance { get; private set; }

    public static void CargarEscenaConPantallaDeCarga(string nombreEscenaACargar)
    {
        if (string.IsNullOrEmpty(nombreEscenaACargar))
        {
            Debug.LogError("Se intent� cargar una escena con nombre vac�o.");
            return;
        }

        // 1. Guardamos est�ticamente el nombre de la escena que queremos cargar al final
        ControladorPantallaCarga.escenaACargar = nombreEscenaACargar;

        // 2. Cargamos la escena intermedia "LoadingScreen"
        // Aseg�rate de que tu escena se llame exactamente "LoadingScreen"
        SceneManager.LoadScene("PantallaCarga");
    }
    // --- FIN NUEVO M�TODO ---

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CargarDatos(); // Carga datos existentes o inicializa por defecto
        }
        else { Destroy(gameObject); }
    }
    // --- Fin Singleton ---

    // --- Suscripci�n a Eventos de Escena ---
    void OnEnable()
    {
        // Cada vez que este objeto se active, suscribirse al evento sceneLoaded
        Debug.Log(">>> GESTOR JUEGO ON ENABLE - Suscribiendo a sceneLoaded <<<");
        SceneManager.sceneLoaded += EscenaCargada;
        Debug.Log("GestorJuego suscrito a sceneLoaded."); // Log para confirmar
    }

    void OnDisable()
    {
        // Cada vez que este objeto se desactive o destruya, desuscribirse
        SceneManager.sceneLoaded -= EscenaCargada;
        Debug.Log("GestorJuego desuscrito de sceneLoaded."); // Log para confirmar
    }
    // --- Fin Suscripci�n ---

    [Header("Estado del Juego")]
    public int diaActual = 1;
    public int dineroActual = 50;

    [Header("Ciclo D�a/Noche")]
    public HoraDelDia horaActual = HoraDelDia.Manana; // El d�a empieza por la ma�ana
    [Tooltip("Material Skybox para la ma�ana")]
    public Material skyboxManana; // <<--- Asigna tu Skybox de Ma�ana aqu�
    [Tooltip("Material Skybox para la tarde")]
    public Material skyboxTarde;   // <<--- Asigna tu Skybox de Tarde aqu�
    [Tooltip("Material Skybox para la noche")]
    public Material skyboxNoche;  // <<--- Asigna tu Skybox de Noche aqu�

    [Header("Econom�a")]
    public int valorPocionCorrecta = 5;
    public int costoRentaDiaria = 10;

    [Header("Referencias UI y Efectos")]
    // Referencia al GestorUI (antes UIManager) <<--- TIPO CAMBIADO
    public GestorUI gestorUI; // <<--- Usa el nombre de clase traducido
    public AudioClip sonidoGanarDinero;
    public AudioClip sonidoPerderDinero;
    public GestorCompradores gestorNPCs; // <<--- NUEVA REFERENCIA: Asigna el GestorNPCs aqu�
    //private InteraccionJugador interaccionJugador; // Para mostrar notificaciones

    [Header("Audio Ambiente")] // Puedes a�adir este encabezado para organizar
    [Tooltip("M�sica o sonido para el MenuPrincipal")]
    public AudioClip musicaMenu; // <<--- NUEVO
    [Tooltip("M�sica o sonido ambiente para el d�a (Ma�ana/Tarde)")]
    public AudioClip audioDia;      // <<--- A�ADE ESTA L�NEA
    [Tooltip("M�sica o sonido ambiente para la noche (grillos?)")]
    public AudioClip audioNoche;     // <<--- A�ADE ESTA L�NEA

    // Referencia privada para la luz (se buscar� en Start)
    private Light luzDireccionalPrincipal = null; // <<--- A�ADE ESTA L�NEA

    void Start()
    {
        
        // Aplicar Skybox inicial (Ma�ana) al arrancar el juego.
        // RenderSettings es global, as� que esto est� bien aqu�.
        ActualizarAparienciaCiclo(true);
        Debug.Log("GestorJuego iniciado, Skybox inicial aplicado.");

        // YA NO buscamos GestorUI ni actualizamos UI aqu�. Eso se har� en EscenaCargada.
    }

    // --- NUEVO: Se ejecuta cuando una escena termina de cargar ---
    void EscenaCargada(Scene escena, LoadSceneMode modo)
    {
        // Ignorar escenas de utilidad (Aseg�rate que los nombres sean EXACTOS)
        if (escena.name == "Arranque" || escena.name == "LoadingScreen" || escena.name == "PantallaCarga")
        {
            Debug.Log($"EscenaCargada: Ignorando escena de utilidad '{escena.name}'.");
            return; // Salir si es una de estas escenas
        }

        // Si no es de utilidad, continuar...
        Debug.Log($"---[EscenaCargada] Escena: '{escena.name}', Hora al entrar: {horaActual} ---");

        // Reaplica el estado visual (Skybox, Luz, Audio) para la hora actual
        ActualizarAparienciaCiclo(true);

        // Buscar la luz direccional principal (si la usas para ambiente)
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
        if (luzDireccionalPrincipal == null) Debug.LogWarning("No se encontr� luz direccional principal.");

        // --- BUSCAR GESTORES DE ESCENA Y ACTUALIZAR UI ---
        gestorUI = FindObjectOfType<GestorUI>(); // Busca el GestorUI en la escena reci�n cargada
        gestorNPCs = FindObjectOfType<GestorCompradores>(); // Busca el GestorNPCs

        // --- POSICIONAR JUGADOR USANDO PUNTO DE SPAWN --- <<<--- L�GICA ACTUALIZADA
        ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
        if (jugador != null)
        {
            PuntoSpawn[] puntos = FindObjectsOfType<PuntoSpawn>();

            Debug.Log($"EscenaCargada: Encontrados {puntos.Length} PuntoSpawn.");
            Debug.Log($"EscenaCargada: Buscando punto con nombre: '{nombrePuntoSpawnSiguiente}'");

            // Buscar el punto con el nombre guardado en nombrePuntoSpawnSiguiente
            PuntoSpawn puntoDestino = puntos.FirstOrDefault(p => p != null && p.nombreIdentificador == nombrePuntoSpawnSiguiente); // A�adido p != null

            // Si no se encontr� el punto esperado Y no era el inicial, buscar el inicial como fallback
            if (puntoDestino == null && nombrePuntoSpawnSiguiente != "SpawnInicialCama")
            {
                Debug.LogWarning($"No se encontr� '{nombrePuntoSpawnSiguiente}'. Buscando fallback 'SpawnInicialCama'...");
                puntoDestino = puntos.FirstOrDefault(p => p != null && p.nombreIdentificador == "SpawnInicialCama");
            }

            // Si finalmente encontramos un punto v�lido...
            if (puntoDestino != null)
            {
                Debug.Log($"Punto destino encontrado: '{puntoDestino.name}' en {puntoDestino.transform.position}. Intentando mover jugador...");
                CharacterController cc = jugador.GetComponent<CharacterController>();
                Vector3 posAntes = jugador.transform.position; // <<--- A�ADE ESTA L�NEA AQU�
                if (cc != null) cc.enabled = false; // Desactivar para teletransportar
                jugador.transform.position = puntoDestino.transform.position;
                jugador.transform.rotation = puntoDestino.transform.rotation; // Usar rotaci�n del punto
                if (cc != null) cc.enabled = true; // Reactivar

                Debug.Log($"Posici�n JUGADOR ANTES: {posAntes}, DESPU�S: {jugador.transform.position}"); // Verificar cambio

                // Resetear vista vertical de la c�mara (�Recuerda a�adir este m�todo a ControladorJugador!)
                jugador.ResetearVistaVertical();
            }
            else
            {
                // Error si no encontramos ning�n punto v�lido
                Debug.LogError("�No se encontr� NING�N PuntoSpawn ('" + nombrePuntoSpawnSiguiente + "' o 'SpawnInicialCama') para posicionar al jugador!");
            }
        }
        else if (escena.name != "MenuPrincipal" && escena.name != "MainMenu") 
        {
            Debug.LogWarning("No se encontr� jugador en EscenaCargada.");
        } // No advertir en Men�
        // Resetear el nombre del siguiente spawn para la pr�xima vez (opcional)
        // nombrePuntoSpawnSiguiente = "SpawnInicialCama";
        // --- FIN POSICIONAR JUGADOR ---


        // --- NUEVO: L�GICA AUDIO POR ESCENA --- <<<--- A�ADIR ESTE BLOQUE AQU�
        AudioClip clipPoner = null; // Clip a reproducir por defecto (silencio)

        if (escena.name == "MenuPrincipal")
        { // Si estamos en el Men� Principal
            clipPoner = musicaMenu;
            Debug.Log("EscenaCargada: Seleccionando m�sica del men�.");
        }
        else if (escena.name == "TiendaDeMagia" || escena.name == "Bosque")
        { // Si estamos en escenas de juego
          // Usar audio seg�n la hora actual del d�a
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
        // Puedes a�adir 'else if' para otras escenas si tienen m�sica propia

        // Llamar al GestorAudio para que ponga el clip seleccionado (o null si no aplica)
        if (GestorAudio.Instancia != null)
        {
            GestorAudio.Instancia.CambiarMusicaFondo(clipPoner);
        }
        else { Debug.LogWarning("GestorAudio no encontrado para cambiar m�sica."); }
        // --- FIN BLOQUE AUDIO ---

        if (gestorUI != null)
        {
            Debug.Log("GestorUI encontrado en EscenaCargada. Actualizando UI.");
            gestorUI.ActualizarUIDinero(dineroActual);

            // --- MOSTRAR D�A SOLO SI NO ES DE NOCHE --- <<<--- CAMBIO AQU�
            if (horaActual != HoraDelDia.Noche)
            {
                Debug.Log($"Mostrando UI del D�a {diaActual} porque es {horaActual}"); // Log opcional
                gestorUI.MostrarInicioDia(diaActual);
            }
            else
            {
                Debug.Log($"No se muestra UI del D�a porque es {horaActual}"); // Log opcional
            }
            // ------------------------------------------
        }
        // Solo mostrar advertencia si falta GestorUI en escenas donde deber�a estar
        else if (escena.name == "TiendaDeMagia" || escena.name == "Bosque" || escena.name == "MainMenu")
        {
            Debug.LogWarning($"GestorUI no encontrado en la escena {escena.name}. �Falta el objeto UIManager o el script?");
        }

        // Solo advertir si falta GestorNPCs en la tienda
        if (gestorNPCs == null && escena.name == "TiendaDeMagia")
        {
            Debug.LogWarning($"GestorNPCs no encontrado en la escena {escena.name}.");
        }
        // ---------------------------------------------------
    }
    // --- Fin EscenaCargada ---

    // --- Funciones de Dinero ---

    // M�todo traducido
    public void AnadirDinero(int cantidad)
    {
        dineroActual += cantidad;
        Debug.Log($"Dinero a�adido: +{cantidad}. Total: {dineroActual}");
        if (gestorUI != null)
        {
            // Llamadas traducidas
            gestorUI.ActualizarUIDinero(dineroActual);
            gestorUI.MostrarCambioDinero(cantidad);
        }
        // Llamada a GestorAudio traducido (asumiendo que existe y tiene m�todo traducido)
        if (GestorAudio.Instancia != null && sonidoGanarDinero != null)
        {
            GestorAudio.Instancia.ReproducirSonido(sonidoGanarDinero);
        }
        //GuardarDatos();
    }

    // M�todo traducido
    private void DeducirRenta()
    {
        dineroActual -= costoRentaDiaria;
        Debug.Log($"Renta diaria deducida: -{costoRentaDiaria}. Total: {dineroActual}");
        if (gestorUI != null)
        {
            // Llamadas traducidas
            gestorUI.ActualizarUIDinero(dineroActual);
            gestorUI.MostrarCambioDinero(-costoRentaDiaria);
        }
        // Llamada a GestorAudio traducido
        if (GestorAudio.Instancia != null && sonidoPerderDinero != null)
        {
            GestorAudio.Instancia.ReproducirSonido(sonidoPerderDinero);
        }
    }

    

    // --- Funci�n de Dormir ---

    // M�todo traducido
    public void IrADormir()
    {

        // --- A�ADIR ESTA COMPROBACI�N AL INICIO ---
        if (durmiendo)
        {
            Debug.Log("Ya est� en proceso de dormir, ignorando petici�n.");
            return; // Salir si ya estamos durmiendo
        }
        
        Debug.Log("Intentando ir a dormir (llamado desde interacci�n)..."); // Mensaje actualizado
        if (gestorUI != null)
        {
            StartCoroutine(SecuenciaDormir()); // Llama a la corutina directamente
        }
        else
        {
            Debug.LogError("No se puede dormir, falta GestorUI.");
        }
    }

    // Corutina traducida
    private IEnumerator SecuenciaDormir()
    {
        // Prevenir doble ejecuci�n
        if (durmiendo) yield break; // Salir si ya est� corriendo
        durmiendo = true; // Marcar que empezamos a dormir

        // Usar try-finally para GARANTIZAR que durmiendo se ponga a false al final
        try
        {
            Debug.Log("Iniciando secuencia de sue�o...");

            // Deshabilitar Movimiento Jugador
            ControladorJugador jugador = FindObjectOfType<ControladorJugador>();
            if (jugador != null) { jugador.HabilitarMovimiento(false); }
            else { Debug.LogWarning("SecuenciaDormir: No se encontr� ControladorJugador para deshabilitar."); }

            // 1. Fundido a Negro
            if (gestorUI != null) yield return StartCoroutine(gestorUI.FundidoANegro());
            else { Debug.LogWarning("SecuenciaDormir: GestorUI null, no se har� fundido a negro."); }

            // 2. L�gica del cambio de d�a
            diaActual++;
            horaActual = HoraDelDia.Manana;
            GuardarDatos();
            npcsGeneradosHoy = 0;
            Debug.Log($"Comenzando D�A {diaActual} - Ma�ana");
            DeducirRenta();
            if (gestorNPCs != null) { gestorNPCs.ReiniciarParaNuevoDia(); }
            else { Debug.LogWarning("GestorNPCs no encontrado para reiniciar d�a."); } // Cambiado a Warning
            ActualizarAparienciaCiclo(true);
            if (GestorAudio.Instancia != null) { GestorAudio.Instancia.CambiarMusicaFondo(audioDia); } // Poner m�sica d�a
                                                                                                       // GuardarDatos(); // Guardar estado

            // 3. Reposicionar Jugador (MIENTRAS EST� NEGRO)
            Debug.Log("Forzando posici�n del jugador junto a la cama...");
            jugador = FindObjectOfType<ControladorJugador>(); // Buscar de nuevo por si acaso
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
                else { Debug.LogError("�No se encontr� 'SpawnInicialCama'!"); }
            }
            else { Debug.LogError("No se encontr� jugador para reposicionar."); }

            // 4. Fundido desde Negro
            if (gestorUI != null) yield return StartCoroutine(gestorUI.FundidoDesdeNegro());
            else { Debug.LogWarning("SecuenciaDormir: GestorUI null, no se har� fundido desde negro."); }

            // 5. Habilitar Movimiento Jugador
            jugador = FindObjectOfType<ControladorJugador>(); // Buscar de nuevo
            if (jugador != null) { jugador.HabilitarMovimiento(true); }
            else { Debug.LogWarning("SecuenciaDormir: No se encontr� ControladorJugador para habilitar."); }

            // 6. Mostrar UI del nuevo d�a
            if (gestorUI != null) gestorUI.MostrarInicioDia(diaActual);

            Debug.Log("Secuencia de sue�o completada.");

        } // Fin del try
        finally
        {
            // Este bloque SIEMPRE se ejecuta al salir de la corutina (normal o por error)
            durmiendo = false; // Permitir dormir de nuevo
            Debug.Log("Flag 'durmiendo' puesto a false.");
        }
    } // Fin de SecuenciaDormir 


    // --- Guardado y Carga Simple (PlayerPrefs) ---

    // M�todo traducido
    // Guarda el estado actual del juego en PlayerPrefs
    private void GuardarDatos()
    {
        Debug.LogWarning($"--- GUARDANDO DATOS --- D�a: {diaActual}, Hora: {horaActual}, Dinero: {dineroActual}");
        PlayerPrefs.SetInt("ExisteGuardado", 1);
        PlayerPrefs.SetInt("DiaActual", diaActual);
        PlayerPrefs.SetInt("DineroActual", dineroActual);
        PlayerPrefs.SetInt("HoraActual", (int)horaActual); // Debe ser Manana (0) aqu�

        Debug.LogError($"GUARDANDO HoraActual como INT: {(int)horaActual} (Enum: {horaActual})");

        StockDataWrapper stockWrapper = new StockDataWrapper();
        foreach (var kvp in stockIngredientesTienda)
        {
            if (kvp.Key != null) stockWrapper.stockList.Add(new StockEntry { ingredienteAssetName = kvp.Key.name, cantidad = kvp.Value });
        }
        string stockJson = JsonUtility.ToJson(stockWrapper);
        PlayerPrefs.SetString("StockIngredientes", stockJson);
        Debug.Log($"Stock Guardado JSON: {stockJson.Substring(0, Mathf.Min(stockJson.Length, 100))}..."); // Mostrar solo inicio

        // Guardar otros datos...
        // PlayerPrefs.SetInt("Reputacion", reputacion);

        // No guardamos la posici�n aqu�, siempre cargamos en puntoSpawnJugadorTienda
        PlayerPrefs.Save();
        Debug.LogWarning("--- DATOS GUARDADOS ---");
    }

    // Carga el estado del juego desde PlayerPrefs
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
        horaActual = (HoraDelDia)PlayerPrefs.GetInt("HoraActual", (int)HoraDelDia.Manana); // Carga la hora guardada (deber�a ser Manana)
        Debug.LogError($"CARGANDO HoraActual como INT: {PlayerPrefs.GetInt("HoraActual", -1)}, Convertido a Enum: {horaActual}"); // -1 si no existe
        Debug.LogError($"--- HORA CARGADA DE PLAYERPREFS: {horaActual} ---");

        stockIngredientesTienda = new Dictionary<DatosIngrediente, int>();
        string stockJson = PlayerPrefs.GetString("StockIngredientes", "{}");
        StockDataWrapper stockWrapper = JsonUtility.FromJson<StockDataWrapper>(stockJson);
        if (stockWrapper?.stockList != null)
        {
            foreach (var entry in stockWrapper.stockList)
            {
                string resourcePath = $"Data/Ingredientes/{entry.ingredienteAssetName}"; // AJUSTA SI TU RUTA DENTRO DE RESOURCES ES DIFERENTE
                DatosIngrediente ingredienteAsset = Resources.Load<DatosIngrediente>(resourcePath);
                if (ingredienteAsset != null) stockIngredientesTienda[ingredienteAsset] = entry.cantidad;
                else Debug.LogWarning($"No se encontr� DatosIngrediente '{entry.ingredienteAssetName}' en 'Resources/{resourcePath}'.");
            }
            Debug.Log($"Stock cargado con {stockIngredientesTienda.Count} tipos.");
        }
        else { Debug.LogWarning("No se pudo deserializar stock."); }

        // Cargar otros datos...
        // reputacion = PlayerPrefs.GetInt("Reputacion", 50);

        Debug.Log($"Datos Cargados - D�a: {diaActual}, Dinero: {dineroActual}, Hora: {horaActual}"); // Verifica la hora cargada
        
        // Resetear el punto de spawn al cargar para empezar en la cama
        nombrePuntoSpawnSiguiente = "SpawnInicialCama"; // Usa el nombre de tu punto de spawn inicial
        Debug.LogWarning("--- DATOS CARGADOS ---");
    }

    // Establece los valores iniciales para una nueva partida
    private void InicializarValoresPorDefecto()
    {
        Debug.Log("Inicializando valores por defecto para Nueva Partida...");
        diaActual = 1;
        dineroActual = 50;
        horaActual = HoraDelDia.Manana;
        nombrePuntoSpawnSiguiente = "SpawnInicialCama"; // <<--- A�ADE ESTA L�NEA
        npcsGeneradosHoy = 0;
        // reputacion = 50;

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
        // Limpiar flag de guardado por si acaso
        PlayerPrefs.DeleteKey("ExisteGuardado");
        PlayerPrefs.Save(); // Guardar el borrado del flag
    }

    // Opcional: Resetear datos

    /*[ContextMenu("Resetear Datos Guardados")]
    public void ResetearDatosGuardados() // M�todo traducido
    {
        PlayerPrefs.DeleteKey("DiaActual");
        PlayerPrefs.DeleteKey("DineroActual");
        diaActual = 1;
        dineroActual = 50;
        if(gestorUI != null) gestorUI.ActualizarUIDinero(dineroActual); // Llamada traducida
        Debug.LogWarning("�Datos guardados reseteados!");
    }*/

    // --- NUEVOS M�TODOS para Contador Diario ---
    public int ObtenerNPCsGeneradosHoy()
    {
        return npcsGeneradosHoy;
    }

    public void RegistrarNPCGeneradoHoy()
    {
        if (npcsGeneradosHoy < limiteNPCsPorDia) // Seguridad extra
        {
            npcsGeneradosHoy++;
            Debug.Log($"NPC Registrado hoy. Total: {npcsGeneradosHoy}/{limiteNPCsPorDia}");
        }
        else
        {
            Debug.LogWarning("Se intent� registrar NPC pero ya se alcanz� el l�mite diario.");
        }
    }
    // ------------------------------------------

    // --- NUEVO: Devuelve true si es de noche ---
    public bool PuedeDormir()
    {
        return horaActual == HoraDelDia.Noche;
    }

    // --- NUEVO: Actualiza el Skybox y opcionalmente luces ---
    void ActualizarAparienciaCiclo(bool instantaneo = false)
    {
        // --- Log al inicio del m�todo ---
        Debug.Log($"[ActualizarAparienciaCiclo] Ejecutando para Hora: {horaActual}");
        // --------------------------------

        Material skyboxAplicar = null;
        //AudioClip audioAplicar = null; // Para la m�sica/ambiente
        Color luzAmbiente = new Color(0.5f, 0.5f, 0.5f, 1f); // Gris por defecto
        float intensidadSol = 1.0f; // Intensidad por defecto
        Quaternion rotacionSol = Quaternion.Euler(50f, -30f, 0f); // Ma�ana por defecto

        switch (horaActual)
        {
            case HoraDelDia.Manana:
                skyboxAplicar = skyboxManana;
                //audioAplicar = audioDia; // Audio de d�a
                luzAmbiente = new Color(0.8f, 0.8f, 0.8f); // Claro
                rotacionSol = Quaternion.Euler(50f, -30f, 0f); // Sol alto
                intensidadSol = 1.0f;
                Debug.Log("[ActualizarAparienciaCiclo] Config Ma�ana.");
                break;
            case HoraDelDia.Tarde:
                skyboxAplicar = skyboxTarde;
                //audioAplicar = audioDia; // Mismo audio (o puedes crear audioTarde)
                luzAmbiente = new Color(0.7f, 0.6f, 0.55f); // C�lido
                rotacionSol = Quaternion.Euler(20f, -150f, 0f); // Sol bajo
                intensidadSol = 0.75f; // Menos intenso
                Debug.Log("[ActualizarAparienciaCiclo] Config Tarde.");
                break;
            case HoraDelDia.Noche:
                skyboxAplicar = skyboxNoche;
                //audioAplicar = audioNoche; // Audio de noche
                luzAmbiente = new Color(0.1f, 0.1f, 0.18f); // Oscuro azulado
                rotacionSol = Quaternion.Euler(-30f, -90f, 0f); // Posici�n de luna/bajo horizonte
                intensidadSol = 0.08f; // Muy tenue
                Debug.Log("[ActualizarAparienciaCiclo] Config Noche.");
                break;
        }

        // Log ANTES de aplicar
        Debug.Log($"[ActualizarAparienciaCiclo] Intentando aplicar Skybox: {(skyboxAplicar != null ? skyboxAplicar.name : "NINGUNO")}");

        // Aplicar Skybox
        if (skyboxAplicar != null) { RenderSettings.skybox = skyboxAplicar; DynamicGI.UpdateEnvironment(); }
        else { Debug.LogWarning($"Skybox NULO para {horaActual}."); }

        // Aplicar Luz Ambiental
        RenderSettings.ambientLight = luzAmbiente;
        Debug.Log($"[ActualizarAparienciaCiclo] Luz Ambiental aplicada: {luzAmbiente}");

        // Aplicar Luz Direccional
        if (luzDireccionalPrincipal != null)
        {
            luzDireccionalPrincipal.intensity = intensidadSol;
            luzDireccionalPrincipal.transform.rotation = rotacionSol;
            // Opcional: Cambiar color de la luz por la noche a azulado?
            // luzDireccionalPrincipal.color = (horaActual == HoraDelDia.Noche) ? new Color(0.6f, 0.7f, 1.0f) : Color.white;
            Debug.Log($"[ActualizarAparienciaCiclo] Luz Direccional - Intensidad: {intensidadSol}, Rot: {rotacionSol.eulerAngles}");
        }

        // Cambiar M�sica/Ambiente <<<--- A�ADIDO
        /*if (GestorAudio.Instancia != null)
        {
            GestorAudio.Instancia.CambiarMusicaFondo(audioAplicar);
        }
        else { Debug.LogWarning("GestorAudio no disponible para cambiar m�sica."); }*/
        // -------------------------
    }

    // --- NUEVO: Registra el viaje y cambia la hora si aplica ---
    public void RegistrarViaje(string escenaDestino)
    {
        HoraDelDia horaPrevia = horaActual;
        HoraDelDia nuevaHora = horaActual; // Por defecto no cambia

        // --- Log ANTES del cambio ---
        Debug.Log($"[RegistrarViaje] Hora ANTES: {horaPrevia}, Viajando a: {escenaDestino}");
        // ---------------------------

        // Reglas de cambio de hora (AJUSTA LOS NOMBRES DE ESCENA SI ES NECESARIO)
        if (horaActual == HoraDelDia.Manana && escenaDestino == "Bosque")
        {
            nuevaHora = HoraDelDia.Tarde;
        }
        else if (horaActual == HoraDelDia.Tarde && escenaDestino == "TiendaDeMagia")
        { // <-- Aseg�rate que este sea el nombre correcto
            nuevaHora = HoraDelDia.Noche;
        }
        // Puedes a�adir m�s reglas aqu�

        // Si la hora cambi�...
        if (nuevaHora != horaPrevia)
        {
            horaActual = nuevaHora;
            // --- Log DESPU�S del cambio ---
            Debug.Log($"[RegistrarViaje] Hora CAMBIADA a: {horaActual}");
            // ----------------------------

            // --- NUEVO: Si se hizo de noche, echar a los NPCs ---
            if (horaActual == HoraDelDia.Noche)
            {
                Debug.Log("[RegistrarViaje] Se hizo de noche, despawneando NPCs...");
                // Llama al m�todo en GestorNPCs (si existe en la escena actual)
                // El '?' evita un error si gestorNPCs es null (ej: si viajamos desde el bosque)
                gestorNPCs?.DespawnTodosNPCsPorNoche();
            }
            // --------------------------------------------------

            Debug.Log($"Viaje cambi� la hora de {horaPrevia} a {horaActual}");
            //ActualizarAparienciaCiclo(true); // Actualizar visuales INSTANT�NEAMENTE
            // SaveData(); // Guarda el nuevo estado de hora (descomentar si quitaste el save/load)
        }
        else
        {
            Debug.Log($"Viaje a {escenaDestino} no cambi� la hora ({horaActual})");
        }
    }

    // Dentro de la clase GestorJuego

    // --- NUEVO: Funciones para Stock de Tienda ---
    // Devuelve la cantidad actual de un ingrediente en el stock de la tienda
    public int ObtenerStockTienda(DatosIngrediente tipo)
    {
        // Comprueba si el tipo de ingrediente es v�lido y si existe en el diccionario
        if (tipo != null && stockIngredientesTienda.TryGetValue(tipo, out int cantidad))
        {
            // Si existe, devuelve la cantidad encontrada
            return cantidad;
        }
        // Si el ingrediente no es v�lido o no est� en el diccionario, devuelve 0
        // Debug.LogWarning($"Intento de obtener stock para '{tipo?.name ?? "NULL"}' no encontrado."); // Log opcional
        return 0;
    }

    // Intenta consumir una unidad del stock. Devuelve true si tuvo �xito, false si no hab�a.
    // Se llama desde FuenteIngredientes al intentar recoger en tienda
    public bool ConsumirStockTienda(DatosIngrediente tipo)
    {
        // Comprueba si el tipo es v�lido y si existe en el diccionario
        if (tipo != null && stockIngredientesTienda.TryGetValue(tipo, out int cantidadActual))
        {
            // Comprueba si queda stock
            if (cantidadActual > 0)
            {
                stockIngredientesTienda[tipo]--; // Restar uno del stock
                Debug.Log($"Consumido 1 de {tipo.nombreIngrediente} del stock. Quedan: {stockIngredientesTienda[tipo]}");
                // Opcional: Actualizar UI del inventario si tienes una
                // if(gestorUI != null) gestorUI.ActualizarInventarioUI();
                return true; // ��xito! Se pudo consumir
            }
            else
            {
                // No quedaba stock
                Debug.Log($"Intento de consumir {tipo.nombreIngrediente}, pero no hay stock (0).");
                return false; // No se pudo consumir
            }
        }
        // El ingrediente no se encontr� en el diccionario
        Debug.LogWarning($"Intento de consumir ingrediente '{tipo?.name ?? "NULL"}' no encontrado en stock.");
        return false; // No se pudo consumir
    }
    // --- FIN FUNCIONES STOCK ---

    // --- NUEVO: Se llama desde IngredienteRecolectable al recoger en bosque ---
    public void AnadirStockTienda(DatosIngrediente tipo, int cantidadAAnadir)
    {
        if (tipo == null)
        {
            Debug.LogWarning("Se intent� a�adir stock para un tipo de ingrediente NULL.");
            return;
        }
        if (cantidadAAnadir <= 0)
        {
            Debug.LogWarning($"Se intent� a�adir una cantidad no positiva ({cantidadAAnadir}) de {tipo.nombreIngrediente}.");
            return;
        }

        // Comprobar si el ingrediente ya existe en el diccionario
        if (stockIngredientesTienda.ContainsKey(tipo))
        {
            // Si existe, simplemente suma la cantidad
            stockIngredientesTienda[tipo] += cantidadAAnadir;
        }
        else // Si no existe (porque quiz�s no estaba en ninguna receta inicial)...
        {
            // ... lo a�adimos al diccionario con la cantidad recolectada.
            stockIngredientesTienda.Add(tipo, cantidadAAnadir);
            Debug.LogWarning($"Ingrediente '{tipo.nombreIngrediente}' no estaba en el stock inicial, a�adido ahora.");
        }

        Debug.Log($"A�adido +{cantidadAAnadir} de {tipo.nombreIngrediente} al stock. Nuevo total: {stockIngredientesTienda[tipo]}");

        // Opcional: Actualizar alguna UI de inventario si la tienes
        // if(gestorUI != null) gestorUI.ActualizarInventarioUI();

        // Opcional: Guardar datos si quieres que el stock persista (actualmente desactivado)
        // GuardarDatos();
    }

    // ... Aqu� van los otros m�todos de tu clase (PuedeDormir, ActualizarAparienciaCiclo, etc.) ...

    // Llamado por la puerta ANTES de cargar la nueva escena
    public void SetSiguientePuntoSpawn(string nombrePunto)
    {
        if (!string.IsNullOrEmpty(nombrePunto))
        {
            nombrePuntoSpawnSiguiente = nombrePunto;
            Debug.Log($"Siguiente punto de spawn fijado a: {nombrePuntoSpawnSiguiente}");
        }
        else
        {
            Debug.LogWarning("Se intent� fijar un nombre de punto de spawn vac�o. Se usar� el anterior o por defecto.");
        }
    }

} // Fin de la clase GestorJuego