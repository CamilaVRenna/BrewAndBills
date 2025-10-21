using UnityEngine;
using UnityEngine.AI; // Necesario para usar la clase NavMeshAgent

// Este script debe adjuntarse al mismo GameObject que tiene el NavMeshAgent y el Animator.
public class ControladorNPC : MonoBehaviour
{
    // Referencias a los componentes (se asignarán automáticamente en Start)
    private NavMeshAgent agenteNavMesh;
    private Animator animador;

    // Nombre del parámetro Float en tu Animator Controller
    // Asegúrate de que este nombre sea EXACTAMENTE el mismo que usaste en el Animator.
    [Tooltip("El nombre del parámetro FLOAT en el Animator (Ej: 'Velocidad' o 'Speed').")]
    public string parametroVelocidad = "Velocidad";

    void Start()
    {
        // Obtener los componentes al inicio para mayor eficiencia
        agenteNavMesh = GetComponent<NavMeshAgent>();
        animador = GetComponent<Animator>();

        if (agenteNavMesh == null)
        {
            Debug.LogError("Error: NavMeshAgent no encontrado. ¿El NPC lo tiene?");
        }
        if (animador == null)
        {
            Debug.LogError("Error: Animator no encontrado. ¿El NPC lo tiene?");
        }
    }

    // Update se ejecuta en cada frame y es la forma correcta de sincronizar
    // la animación con el movimiento, sin necesidad de timers.
    void Update()
    {
        // Solo proceder si ambos componentes están presentes
        if (agenteNavMesh == null || animador == null)
        {
            return;
        }

        // 1. Obtener la velocidad actual del NavMeshAgent.
        // 'velocity.magnitude' nos da la magnitud (valor escalar) de la velocidad vectorial.
        // Si el NPC está quieto, será 0. Si se mueve, será > 0.
        float velocidadActual = agenteNavMesh.velocity.magnitude;

        // 2. Establecer el parámetro Float en el Animator.
        // Esto automáticamente cambia la animación del Blend Tree.
        // Por ejemplo, si velocidadActual es 0, el Blend Tree reproduce Idle.
        // Si es 2.5, reproduce Walk o Run.
        animador.SetFloat(parametroVelocidad, velocidadActual);
    }
}
