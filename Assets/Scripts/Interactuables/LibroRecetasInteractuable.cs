using UnityEngine;
using TMPro; 

public class LibroRecetasInteractuable : MonoBehaviour
{
    [Header("Indicador Visual (Al Mirar)")]
    [Tooltip("Texto que se mostrar� al mirar el libro.")]
    public string textoIndicador = "Leer Libro [E]"; // Puedes cambiar este texto
    [Tooltip("Arrastra aqu� el prefab de Canvas flotante (Prefab_InfoCanvas).")]
    public GameObject prefabCanvasInfo; // <<--- ASIGNAR EN INSPECTOR
    private GameObject canvasInfoActual = null;

    public void MostrarInformacion()
    {
        if (prefabCanvasInfo == null)
        {
            Debug.LogWarning($"Libro {gameObject.name} no tiene prefabCanvasInfo asignado.");
            return;
        }

        if (canvasInfoActual == null)
        {
            Vector3 offset = Vector3.up * 0.75f; // Ajusta offset Y para el libro
            Collider col = GetComponent<Collider>();
            Vector3 basePos = (col != null) ? col.bounds.center : transform.position;
            canvasInfoActual = Instantiate(prefabCanvasInfo, basePos + offset, Quaternion.identity);

            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null)
                {
                    uiScript.textoNombre.text = textoIndicador;
                    uiScript.textoNombre.gameObject.SetActive(true); // Asegurar visibilidad
                }
                else { Debug.LogWarning("InfoCanvasUI en prefab no tiene 'textoNombre' asignado."); }

                if (uiScript.textoCantidad != null)
                {
                    uiScript.textoCantidad.gameObject.SetActive(false); // Ocultar cantidad
                }
            }
            else
            {
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) { tmp.text = textoIndicador; }
                else { Debug.LogWarning("No se encontr� TextMeshProUGUI en prefabCanvasInfo para el Libro."); }
            }
        }
        if (canvasInfoActual != null)
        {
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoNombre != null) uiScript.textoNombre.text = textoIndicador;
            else { TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>(); if (tmp != null) tmp.text = textoIndicador; }
            canvasInfoActual.SetActive(true);
        }
    }

    public void OcultarInformacion()
    {
        if (canvasInfoActual != null)
        {
            canvasInfoActual.SetActive(false);
        }
    }

    void OnDestroy() { if (canvasInfoActual != null) { Destroy(canvasInfoActual); } }
}