using UnityEngine;

// Asegura que este GameObject siempre tenga un componente AudioSource.
[RequireComponent(typeof(AudioSource))]
public class GestorAudio : MonoBehaviour
{
    // Variable privada para guardar la referencia al componente AudioSource.
    private AudioSource fuenteEfectos;

    [Header("M�sica/Ambiente")] // Nueva secci�n
    [Tooltip("Arrastra aqu� un SEGUNDO componente AudioSource para la m�sica/ambiente.")]
    public AudioSource fuenteMusicaFondo; // <<--- NUEVA VARIABLE

    // Propiedad est�tica para implementar el patr�n Singleton.
    // Permite acceder a la instancia �nica de GestorAudio desde cualquier script.
    public static GestorAudio Instancia { get; private set; } // "Instance" es com�n mantenerlo as� por el patr�n Singleton

    // Awake se ejecuta antes que Start, ideal para inicializar Singletons.
    void Awake()
    {
        // L�gica del Singleton:
        // Si no existe ya una instancia...
        if (Instancia == null)
        {
            // ...esta se convierte en la instancia �nica.
            Instancia = this;
            // Opcional: Evita que este objeto se destruya al cargar una nueva escena.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si ya existe una instancia, destruye este GameObject duplicado.
            Destroy(gameObject);
            // Salimos del m�todo para evitar inicializar la fuente de audio en el duplicado.
            return;
        }

        // Obtenemos el componente AudioSource adjunto a este GameObject.
        fuenteEfectos = GetComponent<AudioSource>();

        // Configurar fuente para m�sica/ambiente
        if (fuenteMusicaFondo != null)
        {
            fuenteMusicaFondo.loop = true;        // La m�sica se repite
            fuenteMusicaFondo.playOnAwake = false; // No empieza sola
        }
        else
        {
            // Advertencia si no se asigna en el Inspector
            Debug.LogError("�FuenteMusicaFondo no asignada en GestorAudio! No habr� m�sica/ambiente.");
        }

    }

    // M�todo p�blico para reproducir un sonido espec�fico una vez.
    public void ReproducirSonido(AudioClip clip)
    {
        // Comprobamos que tanto el clip de audio como la fuente de audio no sean nulos.
        if (clip != null && fuenteEfectos != null)
        {
            // Reproduce el clip de audio proporcionado.
            fuenteEfectos.PlayOneShot(clip);
        }
        // Si el clip es nulo, muestra una advertencia en la consola.
        else if (clip == null)
        {
            Debug.LogWarning("Se intent� reproducir un AudioClip nulo.");
        }
        // Si la fuente de audio es nula (no deber�a pasar por RequireComponent y Awake), muestra una advertencia.
        else
        {
            Debug.LogWarning("El GestorAudio no tiene un componente AudioSource asignado o inicializado.");
        }
    }

    // --- NUEVO M�TODO ---
    // Cambia la pista de fondo si es diferente a la actual
    public void CambiarMusicaFondo(AudioClip nuevoClip)
    {
        if (fuenteMusicaFondo == null) return; // Salir si no hay fuente asignada

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
            fuenteMusicaFondo.Play(); // Play() respeta el loop = true
        }
    }
    // --- FIN NUEVO M�TODO ---

}