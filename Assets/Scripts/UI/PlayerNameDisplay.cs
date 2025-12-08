using UnityEngine;
using TMPro; // Necesitas TextMeshPro

public class PlayerNameDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public Canvas nameCanvas;

    void Start()
    {
        if (nameCanvas != null && nameCanvas.worldCamera == null)
        {
            nameCanvas.worldCamera = Camera.main;
        }
    }

    void Update()
    {
        if (nameCanvas != null && Camera.main != null)
        {
            nameCanvas.transform.LookAt(Camera.main.transform);
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