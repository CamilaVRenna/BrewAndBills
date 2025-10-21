using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class CambioDeEscena : MonoBehaviour
{
    // [SerializeField] private GameObject panel; // Ya no se necesita si no hay transición
    [SerializeField] private GameObject finalPanel; // Panel para mostrar el "Fin del Juego"

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Solo se activa si el jugador colisiona con el trigger

            Debug.Log("Condición de fin de juego activada. Mostrando panel final.");

            // 1. Mostrar el panel de fin de juego
            if (finalPanel != null)
            {
                finalPanel.SetActive(true);
            }

            // 2. Iniciar la secuencia para volver al menú
            StartCoroutine(FinalizarJuego());
        }
    }

    private IEnumerator FinalizarJuego()
    {
        // Espera un momento para que el jugador vea el panel final
        yield return new WaitForSeconds(5f);

        Debug.Log("Volviendo al Menú Principal y borrando datos guardados.");

        // Usa el método de GestorJuego para cargar la escena del menú principal.
        // Asumiendo que el menú principal se llama "MenuPrincipal".
        GestorJuego.CargarEscenaConPantallaDeCarga("MenuPrincipal");

        // Limpiar todos los datos guardados para empezar de cero la próxima vez
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    // El IEnumerator ChangeScene() se elimina ya que no hay cambio de escena a "EscenarioPrueba"
    // ni interacción con una cueva.
}