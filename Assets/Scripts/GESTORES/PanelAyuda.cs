using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelAyuda : MonoBehaviour
{ 
    public GameObject helpPanel; 
    private bool isPanelOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && helpPanel != null)
        {
            helpPanel.SetActive(true);
            isPanelOpen = true;
        }
    }

    private void Update()
    {
        if (isPanelOpen && Input.GetKeyDown(KeyCode.E))
        {
            helpPanel.SetActive(false);
            isPanelOpen = false;
            Destroy(gameObject);

        }
    }
}
