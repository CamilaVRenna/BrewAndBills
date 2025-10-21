using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelojUI : MonoBehaviour
{
    // Asigna esta imagen en el Inspector para que actúe como tu reloj visual.
    public Image imagenReloj;

    // Asigna los cuatro sprites en el Inspector en el orden correcto.
    [Tooltip("Orden: 0-Amanecer, 1-Mediodía, 2-Atardecer, 3-Noche")]
    public Sprite[] spritesMomentosDelDia;

    // Asigna este TextMeshPro en el Inspector para mostrar la hora
    [Header("UI de Texto Opcional")]
    public TextMeshProUGUI textoHora;
    public TextMeshProUGUI textoDia;

    void Update()
    {
        if (TimeManager.Instance == null)
        {
            return;
        }

        // Obtén el progreso del día de 0 a 1 (donde 0 es 00:00 y 1 es 24:00)
        float progresoDiaOriginal = TimeManager.Instance.GetDayProgress();

        // Desplaza el progreso del día para que comience a las 18 hs.
        // Un 0.25f representa 6 horas (24 * 0.25 = 6).
        // Sumamos 0.25f para que el punto de las 18hs (0.75f) se convierta en 1.0f (0.0f).
        float progresoDesplazado = (progresoDiaOriginal + 0.25f) % 1.0f;

        // Calcula la hora de juego usando el progreso desplazado.
        int horaJuego = (int)(progresoDesplazado * 24);
        int minutosJuego = (int)((progresoDesplazado * 24 * 60) % 60);

        if (imagenReloj != null && spritesMomentosDelDia.Length == 4)
        {
            int indiceSprite = 0;
            // ✅ CORREGIDO: Lógica de las imágenes basada en los rangos de tiempo que pasaste.
            // Nota: Aquí se usa la horaJuego para una lógica más clara.
            if (horaJuego >= 6 && horaJuego < 9)
            {
                indiceSprite = 0; // Amanecer
            }
            else if (horaJuego >= 9 && horaJuego < 18)
            {
                indiceSprite = 1; // Mediodía
            }
            else if (horaJuego >= 18 && horaJuego < 21)
            {
                indiceSprite = 2; // Atardecer
            }
            else // De 21:00 a 5:59
            {
                indiceSprite = 3; // Noche
            }
            imagenReloj.sprite = spritesMomentosDelDia[indiceSprite];
        }

        if (textoHora != null)
        {
            // Muestra la hora y los minutos
            textoHora.text = $"{horaJuego:D2}:{minutosJuego:D2}";
        }

        if (textoDia != null)
        {
            string momentoDia;
            // ✅ CORREGIDO: Lógica de texto basada en los rangos de tiempo que pasaste.
            if (horaJuego >= 6 && horaJuego < 9)
            {
                momentoDia = "Amanecer";
            }
            else if (horaJuego >= 9 && horaJuego < 18)
            {
                momentoDia = "Mediodía";
            }
            else if (horaJuego >= 18 && horaJuego < 21)
            {
                momentoDia = "Atardecer";
            }
            else
            {
                momentoDia = "Noche";
            }
            textoDia.text = $"Día {TimeManager.Instance.currentDay} - {momentoDia}";
        }
    }
}