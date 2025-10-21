using System.Collections;
using UnityEngine;
using TMPro;

public class CartelAyuda : MonoBehaviour
{
    public string mensaje = "Presiona H para ayuda";
    public TextMeshProUGUI textoMensaje; 
    public GameObject helpPanel; 
    private bool isPanelOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            textoMensaje.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && textoMensaje != null)
        {
            textoMensaje.gameObject.SetActive(false);
            if (isPanelOpen)
            {
                helpPanel.SetActive(false);
                isPanelOpen = false;
            }
        }
    }

    private void Update()
    {
        if (textoMensaje != null && textoMensaje.gameObject.activeSelf && Input.GetKeyDown(KeyCode.H))
        {
            if (!isPanelOpen)
            {
                helpPanel.SetActive(true);
                isPanelOpen = true;
            }
            else
            {
                helpPanel.SetActive(false);
                isPanelOpen = false;
            }
        }
    }
}
