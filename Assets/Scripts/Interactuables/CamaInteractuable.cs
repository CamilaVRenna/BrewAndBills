using UnityEngine;
using TMPro;

public class CamaInteractuable : MonoBehaviour
{
    [Header("Indicador Visual (Al Mirar)")]
    public string textoIndicador = "Dormir [E]";
    public GameObject prefabCanvasInfo;
    private GameObject canvasInfoActual = null;

    private GestorJuego gestorJuego;

    void Start()
    {
        gestorJuego = GestorJuego.Instance;
        if (gestorJuego == null)
        {
            Debug.LogError("CamaInteractuable no encontró la instancia de GestorJuego.");
        }
    }

    public void MostrarInformacion()
    {
        if (prefabCanvasInfo == null) return;
        if (canvasInfoActual == null)
        {
            Vector3 posicionUI = transform.position + Vector3.up * 1.0f;
            canvasInfoActual = Instantiate(prefabCanvasInfo, posicionUI, Quaternion.identity);

            InfoCanvasUI uiScript = canvasInfoActual.GetComponent<InfoCanvasUI>();
            if (uiScript != null)
            {
                if (uiScript.textoNombre != null) uiScript.textoNombre.text = textoIndicador;
                if (uiScript.textoCantidad != null) uiScript.textoCantidad.gameObject.SetActive(false);
            }
            else
            {
                TextMeshProUGUI tmp = canvasInfoActual.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = textoIndicador;
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

    public void InteractuarConCama()
    {
        if (gestorJuego != null)
        {
            Debug.Log("CamaInteractuable: Llamando a IrADormir() en GestorJuego.");
            gestorJuego.IrADormir();
        }
    }
}