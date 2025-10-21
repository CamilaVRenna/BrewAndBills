using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // --- NUEVO: Secuencia de saludo y salida ---
    public void IniciarSecuenciaSaludoYSalida(Vector3 destinoSalida, System.Action onTerminar)
    {
        StartCoroutine(SaludoYSalidaCoroutine(destinoSalida, onTerminar));
    }

    private IEnumerator SaludoYSalidaCoroutine(Vector3 destino, System.Action onTerminar)
    {
        // Mostrar saludo (puedes personalizar el mensaje)
        Debug.Log("NPC de misión: ¡Hola! Soy el mensajero de la cueva.");
        UIMessageManager.Instance?.MostrarMensaje("¡Hola! Soy el mensajero de la cueva.");
        yield return new WaitForSeconds(2.5f);

        // Opcional: puedes agregar más diálogos aquí

        // Moverse hacia el destino de salida
        float velocidad = 3.5f;
        while (Vector3.Distance(transform.position, destino) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
            yield return null;
        }

        // Desaparecer y notificar
        UIMessageManager.Instance?.MostrarMensaje("");
        onTerminar?.Invoke();
        Destroy(gameObject);
    }
}
