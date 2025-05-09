using UnityEngine;
using UnityEngine.SceneManagement;

public class Arranque : MonoBehaviour
{
    // Nombre de la PRIMERA escena real a cargar (tu men� principal)
    public string primeraEscena = "MenuPrincipal";

    void Start()
    {
        // Llama inmediatamente al m�todo est�tico para cargar el men� a trav�s de la pantalla de carga
        GestorJuego.CargarEscenaConPantallaDeCarga(primeraEscena);
    }
}