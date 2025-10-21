using UnityEngine;
using TMPro;

public class FuenteFrascos : MonoBehaviour
{
    // [ANTES] public DatosFrasco datosFrasco; // ELIMINAR
    // [AHORA] Usamos el nombre del ItemCatalog directamente
    [Tooltip("Nombre EXACTO del ítem en el ItemCatalog, ejemplo: 'Frasco Vacío'")]
    public string nombreItem = "Frasco Vacío";

    [Tooltip("Cuántos frascos hay disponibles inicialmente en esta fuente.")]
    public int cantidad = 10;

    // --- UI Información (Opcional) ---
    [Header("UI Información (Opcional)")]
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;
    // ----------------------------------

    public void MostrarInformacion()
    {
        if (string.IsNullOrEmpty(nombreItem)) return; // Salir si no hay nombre de ítem

        // Si no tenemos canvas, créalo y configúralo
        if (canvasInfoActual == null && prefabCanvasInfo != null)
        {
            canvasInfoActual = Instantiate(prefabCanvasInfo, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            canvasInfoActual.transform.LookAt(Camera.main.transform);
            canvasInfoActual.transform.forward *= -1;

            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                // Usamos el nuevo nombreItem string
                if (uiScript.textoNombre != null)
                    uiScript.textoNombre.text = nombreItem;
                if (uiScript.textoCantidad != null)
                    uiScript.textoCantidad.text = $"Quedan: {cantidad}";
            }
        }

        if (canvasInfoActual != null)
        {
            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null && uiScript.textoCantidad != null)
            {
                uiScript.textoCantidad.text = $"Quedan: {cantidad}";
            }
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

    void OnDestroy()
    {
        if (canvasInfoActual != null)
        {
            Destroy(canvasInfoActual);
        }
    }

    // El método IntentarRecoger ahora devuelve el string, no el ScriptableObject
    public string IntentarRecoger()
    {
        if (string.IsNullOrEmpty(nombreItem))
        {
            Debug.LogError("¡FuenteFrascos no tiene asignado un nombre de ítem!", this.gameObject);
            return null;
        }

        if (cantidad > 0)
        {
            cantidad--;

            // Actualizar UI flotante si existe
            if (canvasInfoActual != null && canvasInfoActual.activeSelf)
            {
                InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
                if (uiScript != null && uiScript.textoCantidad != null)
                {
                    uiScript.textoCantidad.text = $"Quedan: {cantidad}";
                }
            }

            // AÑADIMOS EL ÍTEM AL INVENTARIO USANDO EL STRING
            // Aquí se generaba el warning si el string no coincidía con el ItemCatalog
            InventoryManager.Instance?.AddItem(nombreItem);

            Debug.Log($"Recogido {nombreItem}. Quedan: {cantidad}");
            return nombreItem;
        }
        else
        {
            Debug.Log($"¡No quedan más {nombreItem}!");
            return null;
        }
    }
}