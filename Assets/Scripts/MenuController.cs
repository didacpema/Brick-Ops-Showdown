using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private Button tcpClientButton;
    [SerializeField] private Button tcpServerButton;
    [SerializeField] private Button udpClientButton;
    [SerializeField] private Button udpServerButton;

    private void Start()
    {
        tcpClientButton.onClick.AddListener(() => LoadScene("TCP_Client"));
        tcpServerButton.onClick.AddListener(() => LoadScene("TCP_Server"));
        udpClientButton.onClick.AddListener(() => LoadScene("UDP_Client"));
        udpServerButton.onClick.AddListener(() => LoadScene("UDP_Server"));
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}