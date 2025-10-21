using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BrickOps.Networking;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button createServerButton;
    [SerializeField] private Button joinClientButton;

    void Start()
    {
        createServerButton.onClick.AddListener(OnCreateServer);
        joinClientButton.onClick.AddListener(OnJoinClient);
    }

    void OnCreateServer()
    {
        // Crear NetworkManager como servidor
        if (NetworkManager.Instance == null)
        {
            GameObject nmObj = new GameObject("NetworkManager");
            NetworkManager nm = nmObj.AddComponent<NetworkManager>();
            nm.isServer = true;
            nm.port = 6000;
        }
        
        SceneManager.LoadScene("ServerScene");
    }

    void OnJoinClient()
    {
        // Crear NetworkManager como cliente
        if (NetworkManager.Instance == null)
        {
            GameObject nmObj = new GameObject("NetworkManager");
            nmObj.AddComponent<NetworkManager>();
        }
        
        SceneManager.LoadScene("WaitingRoom");
    }
}