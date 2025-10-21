using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Para Slider/Image
using TMPro;         // Para TextMeshProUGUI

public class ControladorPantallaCarga : MonoBehaviour
{
    [Header("UI Elementos")]
    // Elige una de las siguientes dos (barra o slider) y comenta/borra la otra
    // public Slider barraProgresoSlider;
    public Image barraProgresoImagen; // Asigna la imagen con Fill Method = Horizontal
    public TextMeshProUGUI textoProgreso;
    // public TextMeshProUGUI textoTips; // Opcional

    // Variable estática para saber qué cargar
    // Otros scripts pondrán el nombre de la escena aquí ANTES de cargar LoadingScreen
    public static string escenaACargar = "";

    // Lista opcional de tips
    // public string[] tips = { "Consejo 1...", "Consejo 2...", "Recuerda..." };

    void Start()
    {
        // Asegurarse de que la barra empieza vacía
        if (barraProgresoImagen != null) barraProgresoImagen.fillAmount = 0;
        // if (barraProgresoSlider != null) barraProgresoSlider.value = 0;

        // Mostrar un tip inicial (opcional)
        // if (textoTips != null && tips.Length > 0) textoTips.text = tips[Random.Range(0, tips.Length)];

        // Iniciar la carga asíncrona si se especificó una escena
        if (!string.IsNullOrEmpty(escenaACargar))
        {
            StartCoroutine(CargarEscenaAsincrono());
        }
        else
        {
            Debug.LogError("LoadingScreenController: No se especificó ninguna escena para cargar (escenaACargar está vacía).");
            // Quizás cargar menú principal por defecto?
            // StartCoroutine(CargarEscenaAsincrono("MainMenu"));
        }
    }

    IEnumerator CargarEscenaAsincrono()
    {
        yield return null; // Esperar un frame para que la UI inicial se dibuje

        AsyncOperation operacion = SceneManager.LoadSceneAsync(escenaACargar);

        // Evitar que la escena se active automáticamente al llegar al 90%
        operacion.allowSceneActivation = false;

        Debug.Log($"Empezando carga asíncrona de: {escenaACargar}");

        // Mientras la escena se carga en segundo plano (hasta 0.9)
        while (operacion.progress < 0.9f)
        {
            // El progreso va de 0.0 a 0.9, lo escalamos a 0.0 - 1.0
            float progreso = Mathf.Clamp01(operacion.progress / 0.9f);

            // Actualizar UI
            if (barraProgresoImagen != null) barraProgresoImagen.fillAmount = progreso;
            // if (barraProgresoSlider != null) barraProgresoSlider.value = progreso;
            if (textoProgreso != null) textoProgreso.text = $"Cargando... {progreso * 100f:F0}%";

            // Opcional: Cambiar tips de vez en cuando
            // if (textoTips != null && Random.value < 0.01f) // Pequeña probabilidad cada frame
            //     textoTips.text = tips[Random.Range(0, tips.Length)];

            yield return null; // Esperar al siguiente frame
        }

        Debug.Log($"Carga asíncrona completada para: {escenaACargar}. Esperando activación...");

        // Actualizar UI al 100% (o mostrar mensaje)
        if (barraProgresoImagen != null) barraProgresoImagen.fillAmount = 1f;
        // if (barraProgresoSlider != null) barraProgresoSlider.value = 1f;
        if (textoProgreso != null) textoProgreso.text = "¡Listo!"; // O "Presiona una tecla..."

        // OPCIONAL: Esperar un poco o a que el jugador presione una tecla
        // yield return new WaitForSeconds(0.5f);
        // while (!Input.anyKeyDown) { yield return null; }

        // Permitir que la escena cargada se active y se muestre
        operacion.allowSceneActivation = true;

        // El LoadingScreen se destruirá al cargar la nueva escena
    }
}