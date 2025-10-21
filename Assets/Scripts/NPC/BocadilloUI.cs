using UnityEngine;

public class BocadilloUI : MonoBehaviour
{
    private Transform camTransform = null; 

    void LateUpdate()
    {
        if (camTransform == null || !camTransform.gameObject.activeInHierarchy)
        {
            if (Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }
            else
            {
                Camera cualquierCamaraActiva = FindObjectOfType<Camera>();
                if (cualquierCamaraActiva != null)
                {
                    camTransform = cualquierCamaraActiva.transform;
                }
                else
                {
                    camTransform = null;
                }
            }
        }

        if (camTransform != null)
        {
            transform.LookAt(transform.position + camTransform.rotation * Vector3.forward,
                             camTransform.rotation * Vector3.up);
        }
    }
}