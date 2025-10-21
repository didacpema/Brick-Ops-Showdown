using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BrickOps.Networking;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button createServerButton;
    [SerializeField] private Button joinServerButton;

    void Start()
    {
        createServerButton.onClick.AddListener(OnCreateServer);
        joinServerButton.onClick.AddListener(OnJoinServer);
    }

    void OnCreateServer()
    {
        // Crear NetworkManager como servidor
        if (NetworkManager.Instance == null)
        {
            GameObject nmObj = new GameObject("NetworkManager");
            NetworkManager nm = nmObj.AddComponent<NetworkManager>();
            nm.isServer = true;
        }
        
        SceneManager.LoadScene("ServerLobby");
    }

    void OnJoinServer()
    {
        // Crear NetworkManager como cliente
        if (NetworkManager.Instance == null)
        {
            GameObject nmObj = new GameObject("NetworkManager");
            nmObj.AddComponent<NetworkManager>();
        }
        
        SceneManager.LoadScene("ClientConnect");
    }
}