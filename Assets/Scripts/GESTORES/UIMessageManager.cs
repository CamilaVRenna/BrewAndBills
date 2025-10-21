using UnityEngine;
using TMPro;
using System.Collections;

public class UIMessageManager : MonoBehaviour
{
    public static UIMessageManager Instance { get; private set; }
    public TextMeshProUGUI textoMensajesUI;

    private Coroutine mensajeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void MostrarMensaje(string mensaje)
    {
        if (textoMensajesUI != null)
        {
            textoMensajesUI.text = mensaje;
            textoMensajesUI.gameObject.SetActive(true);

            // Si ya hay una corrutina mostrando un mensaje, la detenemos
            if (mensajeCoroutine != null)
                StopCoroutine(mensajeCoroutine);

            // Iniciamos la corrutina para ocultar el mensaje después de 4 segundos
            mensajeCoroutine = StartCoroutine(DesaparecerMensajeDespuesDeTiempo(4f));
        }
    }

    private IEnumerator DesaparecerMensajeDespuesDeTiempo(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        if (textoMensajesUI != null)
            textoMensajesUI.gameObject.SetActive(false);
    }
}