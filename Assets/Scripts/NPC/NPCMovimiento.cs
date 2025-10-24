using UnityEngine;
using UnityEngine.AI;

public class NPCMovimiento : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private NPCComprador npcComprador; // Referencia al controlador principal

    [HideInInspector] public Vector3 destinoSalida; // Posición de salida

    // Esta referencia DEBE ser un Transform en la escena (la ventana) que el NPC mira al detenerse.
    public Transform puntoMiradaVentana;

    private bool mirandoVentana = false;

    // Se declara como 'int' regular. Se inicializará en Awake().
    private int speedHash;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        npcComprador = GetComponent<NPCComprador>();

        if (navMeshAgent == null)
        {
            Debug.LogError($"¡NavMeshAgent no encontrado en el NPC {gameObject.name}!");
            enabled = false;
        }
        else
        {
            // Deshabilita el agente al inicio, se habilita solo cuando debe moverse.
            navMeshAgent.enabled = false;
        }

        if (animator == null)
        {
            Debug.LogWarning($"¡Animator no encontrado en el NPC {gameObject.name}! Las animaciones no funcionarán.");
        }
        else
        {
            // SOLUCIÓN: Inicializa el hash en Awake() para evitar errores de parámetro no encontrado.
            speedHash = Animator.StringToHash("Speed");
        }
    }

    void Update()
    {
        ActualizarAnimacion();
    }

    // ------------------------------------------------------------------
    // LÓGICA DE MOVIMIENTO
    // ------------------------------------------------------------------

    /// <summary>
    /// Inicia el movimiento del NPC hacia el destino especificado.
    /// </summary>
    public void IniciarMovimiento(Vector3 destino)
    {
        if (navMeshAgent == null) return;

        // Habilita el agente y el movimiento
        navMeshAgent.enabled = true;
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(destino);
        mirandoVentana = false; // Resetear para el giro al llegar
    }

    /// <summary>
    /// Mueve el NPC hacia su posición de salida.
    /// </summary>
    public void IrseHaciaSalida()
    {
        IniciarMovimiento(destinoSalida);
    }

    /// <summary>
    /// Comprueba si el NPC ha llegado a su destino.
    /// </summary>
    public bool CheckearLlegadaDestino()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled || navMeshAgent.pathPending) return false;

        // Comprobación de llegada: remainingDistance debe ser menor o igual al stoppingDistance
        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.1f)
        {
            // Verificación final para asegurar que no hay camino activo o la velocidad es nula.
            // Uso de 'velocity.sqrMagnitude' es más eficiente que 'velocity.magnitude == 0'.
            if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f) // <--- Optimización
            {
                // Detener y deshabilitar el agente para ahorrar recursos y forzar la animación a 'Idle'
                navMeshAgent.isStopped = true;
                navMeshAgent.enabled = false;
                mirandoVentana = false;
                return true; // ¡Ha llegado!
            }
        }
        return false;
    }

    // ------------------------------------------------------------------
    // LÓGICA DE ROTACIÓN Y ANIMACIÓN
    // ------------------------------------------------------------------

    /// <summary>
    /// Rota el NPC hacia el punto de la ventana si está detenido.
    /// </summary>
    public void IntentarGirarHaciaVentana()
    {
        if (mirandoVentana || puntoMiradaVentana == null) return;

        // Cálculo de la dirección ignorando la altura (eje Y).
        Vector3 dir = puntoMiradaVentana.position - transform.position;
        Vector3 dirHoriz = new Vector3(dir.x, 0, dir.z);

        if (dirHoriz.sqrMagnitude > 0.001f)
        {
            Quaternion rotObj = Quaternion.LookRotation(dirHoriz);
            float rotSpeed = 360f; // Velocidad de giro manual

            // Giro suave
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObj, rotSpeed * Time.deltaTime);

            // Comprobación de finalización del giro
            if (Quaternion.Angle(transform.rotation, rotObj) < 1.0f)
            {
                transform.rotation = rotObj;
                mirandoVentana = true;
            }
        }
        else
        {
            mirandoVentana = true;
        }
    }

    /// <summary>
    /// Envía la velocidad del NavMeshAgent al Animator para controlar las animaciones de movimiento/idle.
    /// </summary>
    private void ActualizarAnimacion()
    {
        // 1. Verificación de componentes.
        if (animator == null || navMeshAgent == null || speedHash == 0) return;

        float currentSpeed = 0f;

        // 2. Solo calcular la velocidad si el NavMeshAgent está habilitado y en movimiento.
        if (navMeshAgent.enabled && !navMeshAgent.isStopped)
        {
            // Usa desiredVelocity para una lectura más estable y predicha de la velocidad.
            currentSpeed = navMeshAgent.desiredVelocity.magnitude;
        }
        // Si el agente no está habilitado (navMeshAgent.enabled = false), currentSpeed = 0, forzando 'Idle'.

        // Enviamos la velocidad al Animator.
        animator.SetFloat(speedHash, currentSpeed);
    }
}