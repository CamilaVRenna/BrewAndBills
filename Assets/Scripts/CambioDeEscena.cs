using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class CambioDeEscena : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject finalPanel;

private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        // Solo permitir entrar si la misión está completa
        if (GestorJuego.Instance != null && GestorJuego.Instance.misionCompleta)
        {
            Debug.Log("fin");
            finalPanel.SetActive(true);
            StartCoroutine(FinalizarJuego());
            // Aquí puedes poner la lógica para cambiar de escena si quieres
        }
        else
        {
            StartCoroutine(ChangeScene());
            Debug.Log("¡Debes recolectar 3 mieles antes de entrar a la cueva!");
        }
    }
}

    private IEnumerator FinalizarJuego()
    {
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("MenuPrincipal");
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

     IEnumerator ChangeScene()
    {
            panel.gameObject.SetActive(true); 
            if (GestorJuego.Instance.interactuoConCueva == false) 
            {
                GestorJuego.Instance.interactuoConCueva = true;
            }
            if (GestorJuego.Instance != null)
                GestorJuego.Instance.GuardarDatos();

            yield return new WaitForSeconds(8f); 
            SceneManager.LoadScene("EscenarioPrueba");
    }
}