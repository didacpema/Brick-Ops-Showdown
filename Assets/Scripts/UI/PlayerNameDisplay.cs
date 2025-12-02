using UnityEngine;
using TMPro; // Necesitas TextMeshPro

public class PlayerNameDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText; // Arrastra aquí el componente de texto
    public Canvas nameCanvas;

    void Start()
    {
        // Configurar el canvas para que use la cámara principal
        if (nameCanvas != null && nameCanvas.worldCamera == null)
        {
            nameCanvas.worldCamera = Camera.main;
        }
    }

    void Update()
    {
        // Billboarding: Que el texto mire siempre a la cámara
        if (nameCanvas != null && Camera.main != null)
        {
            nameCanvas.transform.LookAt(Camera.main.transform);
            // Corregir la rotación porque LookAt a veces lo deja invertido en UI
            nameCanvas.transform.Rotate(0, 180, 0); 
        }
    }

    public void SetName(string playerName)
    {
        if (nameText != null)
        {
            nameText.text = playerName;
        }
    }
}