using UnityEngine;

public class BocadilloUI : MonoBehaviour
{
    private Transform camTransform = null; // Inicializar a null
    //private bool busquedaInicialHecha = false; // Para evitar logs repetitivos

    // Awake o Start pueden usarse para configuraciones iniciales que NO dependen de la c�mara
    void Awake()
    {
        // Podr�as configurar otras cosas aqu� si fuera necesario
    }

    // Usamos LateUpdate para asegurarnos de que la c�mara ya se haya movido/actualizado
    void LateUpdate()
    {
        // --- B�SQUEDA DIN�MICA DE C�MARA ---
        // Comprobar si no tenemos c�mara O si la que ten�amos est� inactiva
        if (camTransform == null || !camTransform.gameObject.activeInHierarchy)
        {
            // Si no tenemos c�mara v�lida, intentar encontrar una CADA frame
            // Debug.LogWarning($"BocadilloUI ({gameObject.name}): Buscando c�mara..."); // Log Opcional (puede ser ruidoso)

            // Intento 1: Camera.main (la forma preferida y m�s eficiente)
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }
            else
            {
                // Fallback: Buscar CUALQUIER c�mara activa si MainCamera falla
                // Esto podr�a coger la c�mara del minijuego temporalmente
                Camera cualquierCamaraActiva = FindObjectOfType<Camera>();
                if (cualquierCamaraActiva != null)
                {
                    camTransform = cualquierCamaraActiva.transform;
                    // Ya no necesitamos mostrar el warning aqu�, es esperado si la MainCamera est� inactiva
                }
                else
                {
                    // Si no hay NINGUNA c�mara activa, poner camTransform a null
                    camTransform = null;
                }
            }
            // Log opcional para saber qu� encontr�
            // if(camTransform != null) Debug.Log($"BocadilloUI ({gameObject.name}) encontr�/usa c�mara: {camTransform.name}");
            // else Debug.LogWarning($"BocadilloUI ({gameObject.name}) no encontr� c�mara activa este frame.");
        }
        // --- FIN B�SQUEDA ---


        // --- L�GICA DE ORIENTACI�N (Billboard) ---
        // Si DESPU�S de la b�squeda tenemos una c�mara v�lida, orientar hacia ella
        if (camTransform != null)
        {
            transform.LookAt(transform.position + camTransform.rotation * Vector3.forward,
                             camTransform.rotation * Vector3.up);
        }
        // --- FIN ORIENTACI�N ---
    }// --- FIN ORIENTACI�N ---
}