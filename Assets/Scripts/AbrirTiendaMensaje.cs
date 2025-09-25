using System.Collections;
using UnityEngine;
using TMPro;

public class AbrirTiendaMensaje : MonoBehaviour
{
    // Asigna tu GestorCompradores aquí desde el Inspector
    public GestorCompradores gestorCompradores;

    [Header("Configuraci\u00f3n")]
    public string tagJugador = "Player";
    public string mensajeAbrir = "Presiona E para abrir la tienda";
    public string mensajeCerrar = "Presiona E para cerrar la tienda";

    [Header("UI")]
    public TextMeshProUGUI textoMensaje;

    private bool jugadorEnArea = false;

    void Awake()
    {
        if (gestorCompradores == null)
        {
            gestorCompradores = FindObjectOfType<GestorCompradores>();
        }
    }

    private void Update()
    {
        if (jugadorEnArea && Input.GetKeyDown(KeyCode.E))
        {
            if (gestorCompradores == null)
            {
                Debug.LogError("El GestorCompradores no est\u00e1 asignado ni se pudo encontrar.");
                return;
            }

            // Si la tienda est\u00e1 abierta, la cerramos. Si no, la abrimos.
            if (gestorCompradores.tiendaAbierta)
            {
                gestorCompradores.CerrarTienda();
            }
            else
            {
                gestorCompradores.AbrirTienda();
            }

            // Actualizamos el mensaje inmediatamente
            ActualizarMensajeUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador) && textoMensaje != null)
        {
            jugadorEnArea = true;
            ActualizarMensajeUI();
            textoMensaje.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(tagJugador) && textoMensaje != null)
        {
            jugadorEnArea = false;
            textoMensaje.gameObject.SetActive(false);
        }
    }

    private void ActualizarMensajeUI()
    {
        if (gestorCompradores == null || textoMensaje == null) return;

        // Comprobamos el estado de la tienda y cambiamos el mensaje
        if (gestorCompradores.tiendaAbierta)
        {
            textoMensaje.text = mensajeCerrar;
        }
        else
        {
            textoMensaje.text = mensajeAbrir;
        }
    }
}