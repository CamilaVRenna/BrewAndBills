using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ControladorPantallaCarga : MonoBehaviour
{
    [Header("UI Elementos")]
    public Image barraProgresoImagen;
    public TextMeshProUGUI textoProgreso;

    // ❌ ELIMINADA: La variable estática 'escenaACargar' ya no es necesaria.
    // public static string escenaACargar = "";

    void Start()
    {
        if (barraProgresoImagen != null) barraProgresoImagen.fillAmount = 0;

        // 🔑 CRÍTICO: Obtener el destino del Gestor de Carga persistente (Singleton).
        if (GestorCarga.Instancia == null)
        {
            Debug.LogError("🚨 ERROR CRÍTICO: GestorCarga no encontrado. La carga no puede continuar.");
            return;
        }

        // Obtener el destino real (que será "MenuPrincipal" o "EscenarioPrueba")
        string escenaDestino = GestorCarga.Instancia.ObtenerDestino();

        Debug.Log($"[ControladorPantallaCarga] Destino de carga obtenido de Singleton: {escenaDestino}.");

        StartCoroutine(CargarEscenaAsincrono(escenaDestino));
    }

    IEnumerator CargarEscenaAsincrono(string escenaDestino) // Modificado para aceptar el destino
    {
        yield return null; // Esperar un frame para que la UI inicial se dibuje

        AsyncOperation operacion = SceneManager.LoadSceneAsync(escenaDestino);

        operacion.allowSceneActivation = false;

        Debug.Log($"Empezando carga asíncrona de: {escenaDestino}");

        // Bucle de progreso
        while (operacion.progress < 0.9f)
        {
            float progreso = Mathf.Clamp01(operacion.progress / 0.9f);

            if (barraProgresoImagen != null) barraProgresoImagen.fillAmount = progreso;
            if (textoProgreso != null) textoProgreso.text = $"Cargando... {progreso * 100f:F0}%";

            yield return null;
        }

        Debug.Log($"Carga asíncrona completada para: {escenaDestino}. FORZANDO ACTIVACIÓN...");

        // Mostrar progreso completo en la UI
        if (barraProgresoImagen != null) barraProgresoImagen.fillAmount = 1f;
        if (textoProgreso != null) textoProgreso.text = "¡Listo!";

        yield return null;

        // 🔑 Activación de la escena
        operacion.allowSceneActivation = true;

        // Esperar explícitamente a que la operación termine por completo
        while (!operacion.isDone)
        {
            yield return null;
        }

        // ❌ ELIMINADA: La limpieza de la variable estática ya no es necesaria.
        Debug.Log("[ControladorPantallaCarga] Carga finalizada.");

        // El LoadingScreen se destruirá automáticamente al cargar la nueva escena
    }
}