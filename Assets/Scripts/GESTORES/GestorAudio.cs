using UnityEngine;

// Asegura que este GameObject siempre tenga un componente AudioSource.
[RequireComponent(typeof(AudioSource))]
public class GestorAudio : MonoBehaviour
{
    // Variable privada para guardar la referencia al componente AudioSource.
    private AudioSource fuenteEfectos;

    [Header("M�sica/Ambiente")] // Nueva secci�n
    [Tooltip("Arrastra aqu� un SEGUNDO componente AudioSource para la m�sica/ambiente.")]
    public AudioSource fuenteMusicaFondo;

    [Header("Efectos de Sonido")] // Secci�n agregada para clips de SFX espec�ficos
    [Tooltip("Clip de audio predeterminado para las puertas. Asigna un AudioClip en el Inspector.")]
    [SerializeField] private AudioClip clipSonidoPuertaDefault;

    // Propiedad est�tica para implementar el patr�n Singleton.
    public static GestorAudio Instancia { get; private set; }

    void Awake()
    {
        // L�gica del Singleton:
        if (Instancia == null)
        {
            Instancia = this;
            // Opcional: Evita que este objeto se destruya al cargar una nueva escena.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Obtenemos el componente AudioSource adjunto a este GameObject.
        fuenteEfectos = GetComponent<AudioSource>();

        // Configurar fuente para m�sica/ambiente
        if (fuenteMusicaFondo != null)
        {
            fuenteMusicaFondo.loop = true;      // La m�sica se repite
            fuenteMusicaFondo.playOnAwake = false; // No empieza sola
        }
        else
        {
            Debug.LogError("�FuenteMusicaFondo no asignada en GestorAudio! No habr� m�sica/ambiente.");
        }
    }

    // M�todo p�blico para reproducir un sonido espec�fico una vez.
    public void ReproducirSonido(AudioClip clip)
    {
        if (clip != null && fuenteEfectos != null)
        {
            fuenteEfectos.PlayOneShot(clip);
        }
        else if (clip == null)
        {
            Debug.LogWarning("Se intent� reproducir un AudioClip nulo.");
        }
        else
        {
            Debug.LogWarning("El GestorAudio no tiene un componente AudioSource asignado o inicializado.");
        }
    }

    /// <summary>
    /// Reproduce el sonido de una puerta. Acepta un clip opcional para usar un sonido diferente al predeterminado.
    /// Este m�todo es compatible con llamadas que pasan 0 o 1 argumento.
    /// </summary>
    /// <param name="clip">El clip espec�fico de la puerta a reproducir. Si es nulo, usa el clip predeterminado (clipSonidoPuertaDefault).</param>
    public void ReproducirSonidoPuerta(AudioClip clip = null)
    {
        // Si se proporciona un clip, �salo. Si no, usa el clip predeterminado.
        AudioClip clipAUsar = clip != null ? clip : clipSonidoPuertaDefault;

        if (clipAUsar != null && fuenteEfectos != null)
        {
            fuenteEfectos.PlayOneShot(clipAUsar);
        }
        else
        {
            Debug.LogWarning("No se pudo reproducir el sonido de la puerta. Falta el clip ('clipSonidoPuertaDefault') o la fuente de efectos.");
        }
    }


    // Cambia la pista de fondo si es diferente a la actual
    public void CambiarMusicaFondo(AudioClip nuevoClip)
    {
        if (fuenteMusicaFondo == null) return;

        // Si el nuevo clip es nulo, detener m�sica
        if (nuevoClip == null)
        {
            if (fuenteMusicaFondo.isPlaying) fuenteMusicaFondo.Stop();
            fuenteMusicaFondo.clip = null;
            Debug.Log("M�sica de fondo detenida (clip nulo).");
            return;
        }

        // Solo cambiar y reproducir si el clip es diferente al actual o si no est� sonando
        if (fuenteMusicaFondo.clip != nuevoClip || !fuenteMusicaFondo.isPlaying)
        {
            Debug.Log($"Cambiando m�sica/ambiente a: {nuevoClip.name}");
            fuenteMusicaFondo.clip = nuevoClip;
            fuenteMusicaFondo.Play();
        }
    }
}
