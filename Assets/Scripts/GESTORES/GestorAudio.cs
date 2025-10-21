using UnityEngine;

// Asegura que este GameObject siempre tenga un componente AudioSource.
[RequireComponent(typeof(AudioSource))]
public class GestorAudio : MonoBehaviour
{
    // Variable privada para guardar la referencia al componente AudioSource.
    private AudioSource fuenteEfectos;

    [Header("Música/Ambiente")] // Nueva sección
    [Tooltip("Arrastra aquí un SEGUNDO componente AudioSource para la música/ambiente.")]
    public AudioSource fuenteMusicaFondo;

    [Header("Efectos de Sonido")] // Sección agregada para clips de SFX específicos
    [Tooltip("Clip de audio predeterminado para las puertas. Asigna un AudioClip en el Inspector.")]
    [SerializeField] private AudioClip clipSonidoPuertaDefault;

    // Propiedad estática para implementar el patrón Singleton.
    public static GestorAudio Instancia { get; private set; }

    void Awake()
    {
        // Lógica del Singleton:
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

        // Configurar fuente para música/ambiente
        if (fuenteMusicaFondo != null)
        {
            fuenteMusicaFondo.loop = true;      // La música se repite
            fuenteMusicaFondo.playOnAwake = false; // No empieza sola
        }
        else
        {
            Debug.LogError("¡FuenteMusicaFondo no asignada en GestorAudio! No habrá música/ambiente.");
        }
    }

    // Método público para reproducir un sonido específico una vez.
    public void ReproducirSonido(AudioClip clip)
    {
        if (clip != null && fuenteEfectos != null)
        {
            fuenteEfectos.PlayOneShot(clip);
        }
        else if (clip == null)
        {
            Debug.LogWarning("Se intentó reproducir un AudioClip nulo.");
        }
        else
        {
            Debug.LogWarning("El GestorAudio no tiene un componente AudioSource asignado o inicializado.");
        }
    }

    /// <summary>
    /// Reproduce el sonido de una puerta. Acepta un clip opcional para usar un sonido diferente al predeterminado.
    /// Este método es compatible con llamadas que pasan 0 o 1 argumento.
    /// </summary>
    /// <param name="clip">El clip específico de la puerta a reproducir. Si es nulo, usa el clip predeterminado (clipSonidoPuertaDefault).</param>
    public void ReproducirSonidoPuerta(AudioClip clip = null)
    {
        // Si se proporciona un clip, úsalo. Si no, usa el clip predeterminado.
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

        // Si el nuevo clip es nulo, detener música
        if (nuevoClip == null)
        {
            if (fuenteMusicaFondo.isPlaying) fuenteMusicaFondo.Stop();
            fuenteMusicaFondo.clip = null;
            Debug.Log("Música de fondo detenida (clip nulo).");
            return;
        }

        // Solo cambiar y reproducir si el clip es diferente al actual o si no está sonando
        if (fuenteMusicaFondo.clip != nuevoClip || !fuenteMusicaFondo.isPlaying)
        {
            Debug.Log($"Cambiando música/ambiente a: {nuevoClip.name}");
            fuenteMusicaFondo.clip = nuevoClip;
            fuenteMusicaFondo.Play();
        }
    }
}
