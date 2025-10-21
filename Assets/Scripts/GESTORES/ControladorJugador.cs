using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ControladorJugador : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidadMovimiento = 5.0f;
    public float velocidadCorrer = 8.0f;
    public float velocidadAgachado = 2.0f;
    public float gravedad = -9.81f;

    [Header("Ajustes de Salto")]
    public float fuerzaSalto = 5.0f;
    private float velocidadVertical;

    [Header("Ajustes de Agacharse")]
    public float alturaNormal = 2.0f;
    public float alturaAgachado = 1.0f;
    public float centroNormalY = 0f;
    public float centroAgachadoY = -0.5f;
    public float posCamaraNormalY = 0.8f;
    public float posCamaraAgachadoY = 0.4f;
    private bool estaAgachado = false;

    [Header("Ajustes de Vista (Cámara)")]
    public Camera camaraJugador;
    public float velocidadMirar = 2.0f;
    public float limiteMirarX = 80.0f;

    [Header("Campo de Visión (FOV)")]
    [Range(40f, 100f)]
    public float campoDeVision = 60f;

    // Referencias y estado interno
    private CharacterController controladorPersonaje;
    private float rotacionX = 0; // Acumulador para rotación vertical de la cámara
    private bool puedeMoverse = true;

    // --- Variables para Guardar/Restaurar Rotación ---
    private Quaternion rotacionCuerpoAlmacenada;
    private Quaternion rotacionCamaraAlmacenada;
    // -------------------------------------------------

    void Start()
    {
        controladorPersonaje = GetComponent<CharacterController>();
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        AplicarCampoVision();

        velocidadVertical = 0f;
        estaAgachado = false;
        AplicarEstadoAgachado(false);

        // Inicializar rotaciones almacenadas por si acaso
        rotacionCuerpoAlmacenada = transform.rotation;
        if (camaraJugador != null) rotacionCamaraAlmacenada = camaraJugador.transform.localRotation;
    }

    void Update()
    {
        ManejarAgacharse();
        ManejarSalto();

        if (puedeMoverse)
        {
            // Bloquear y ocultar cursor (si el juego no lo maneja centralmente)
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;

            // Llamar a los métodos de movimiento y cámara
            ManejarMovimiento();
            ManejarVistaCamara();
        }

        if (camaraJugador != null && camaraJugador.fieldOfView != campoDeVision)
        {
            AplicarCampoVision();
        }
    }

    void ManejarAgacharse()
    {
        if (!puedeMoverse) return;
        if (Input.GetKeyDown(KeyCode.LeftControl)) { estaAgachado = true; AplicarEstadoAgachado(true); }
        if (Input.GetKeyUp(KeyCode.LeftControl)) { estaAgachado = false; AplicarEstadoAgachado(false); }
    }

    void AplicarEstadoAgachado(bool agachado)
    {
        controladorPersonaje.height = agachado ? alturaAgachado : alturaNormal;
        controladorPersonaje.center = new Vector3(0, agachado ? centroAgachadoY : centroNormalY, 0);
        if (camaraJugador != null)
        {
            camaraJugador.transform.localPosition = new Vector3(
                 camaraJugador.transform.localPosition.x,
                 agachado ? posCamaraAgachadoY : posCamaraNormalY,
                 camaraJugador.transform.localPosition.z);
        }
    }

    void ManejarSalto()
    {
        if (!puedeMoverse) return;
        if (controladorPersonaje.isGrounded)
        {
            if (Input.GetButtonDown("Jump"))
            {
                velocidadVertical = fuerzaSalto;
                if (estaAgachado) { estaAgachado = false; AplicarEstadoAgachado(false); }
            }
        }
    }

    void ManejarMovimiento()
    {
        Vector3 adelante = transform.TransformDirection(Vector3.forward);
        Vector3 derecha = transform.TransformDirection(Vector3.right);

        // CORRECCIÓN: Usar GetAxisRaw para eliminar el deslizamiento/inercia.
        float inputZ = puedeMoverse ? Input.GetAxisRaw("Vertical") : 0;
        float inputX = puedeMoverse ? Input.GetAxisRaw("Horizontal") : 0;

        bool intentaCorrer = Input.GetKey(KeyCode.LeftShift) && !estaAgachado && puedeMoverse;
        float velocidadAUsar = estaAgachado ? velocidadAgachado : (intentaCorrer ? velocidadCorrer : velocidadMovimiento);
        Vector3 movimientoXZ = ((adelante * inputZ) + (derecha * inputX)).normalized * velocidadAUsar; // Normalizado para consistencia

        if (controladorPersonaje.isGrounded) { if (velocidadVertical < 0) { velocidadVertical = -1f; } }
        velocidadVertical += gravedad * Time.deltaTime;

        Vector3 movimientoFinal = new Vector3(movimientoXZ.x, velocidadVertical, movimientoXZ.z);
        controladorPersonaje.Move(movimientoFinal * Time.deltaTime);
    }

    void ManejarVistaCamara()
    {
        if (puedeMoverse)
        {
            rotacionX += -Input.GetAxis("Mouse Y") * velocidadMirar;
            rotacionX = Mathf.Clamp(rotacionX, -limiteMirarX, limiteMirarX);
            camaraJugador.transform.localRotation = Quaternion.Euler(rotacionX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * velocidadMirar, 0);
        }
    }

    void AplicarCampoVision()
    {
        if (camaraJugador != null) { camaraJugador.fieldOfView = campoDeVision; }
    }

    public void HabilitarMovimiento(bool habilitar)
    {
        puedeMoverse = habilitar;
    }

    public void AlmacenarRotacionActual()
    {
        rotacionCuerpoAlmacenada = transform.rotation; // Guarda rotación global del jugador
        if (camaraJugador != null)
        {
            rotacionCamaraAlmacenada = camaraJugador.transform.localRotation; // Guarda rotación local de la cámara
            // Guarda también el ángulo X acumulado actual para la vista vertical
            rotacionX = camaraJugador.transform.localEulerAngles.x;
            // Ajustar si el ángulo Euler es > 180 (Unity a veces devuelve esto para negativos)
            if (rotacionX > 180f) rotacionX -= 360f;
        }
        Debug.Log("Rotación Almacenada"); // Para confirmar que se llama
    }

    public void RestaurarRotacionAlmacenada()
    {
        transform.rotation = rotacionCuerpoAlmacenada; // Restaura rotación global del jugador
        if (camaraJugador != null)
        {
            camaraJugador.transform.localRotation = rotacionCamaraAlmacenada; // Restaura rotación local de la cámara
            // Re-asignar rotacionX asegura que la próxima entrada del ratón continúe desde aquí
        }
        Debug.Log("Rotación Restaurada"); // Para confirmar que se llama
    }

    public void ResetearVistaVertical()
    {
        rotacionX = 0f; // Poner ángulo vertical a 0
        if (camaraJugador != null)
        {
            camaraJugador.transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f); // Aplicar rotación X=0
        }
    }
}