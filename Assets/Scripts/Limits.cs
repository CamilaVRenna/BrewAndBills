using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Limits : MonoBehaviour
{
    public string message;
    public TextMeshProUGUI mensajeUI; // Asigna el objeto de texto desde el inspector

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && mensajeUI != null)
        {
            mensajeUI.text = message;
            mensajeUI.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && mensajeUI != null)
        {
            mensajeUI.text = "";
            mensajeUI.gameObject.SetActive(false);
        }
    }
}
