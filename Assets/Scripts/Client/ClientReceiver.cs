using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections; // <--- Esto arregla el error de IEnumerator
using UnityEngine.SceneManagement;

public class ClientReceiver : MonoBehaviour
{
    UdpClient udpClient;
    IPEndPoint anyIP;

    void Start()
    {
        udpClient = new UdpClient(0);
        anyIP = new IPEndPoint(IPAddress.Any, 0);
        udpClient.BeginReceive(OnReceive, null);
    }

    void OnReceive(IAsyncResult ar)
    {
        byte[] data = udpClient.EndReceive(ar, ref anyIP);
        udpClient.BeginReceive(OnReceive, null);

        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            byte packetID = reader.ReadByte();

            if (packetID == 1) // StartGame
            {
                string sceneName = reader.ReadString();
                StartCoroutine(LoadSceneCoroutine(sceneName));
            }
        }
    }

    IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return null; // esperar un frame para asegurarse de que estamos en el hilo principal
        SceneManager.LoadScene(sceneName);
    }
}
