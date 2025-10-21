using UnityEngine;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

public class ServerController : MonoBehaviour
{
    public UdpClient udpServer;
    public List<IPEndPoint> connectedClients = new List<IPEndPoint>();

    void Start()
    {
        udpServer = new UdpClient(7777);
    }

    public void OnStartGameButton()
    {
        Debug.Log("Starting game... Sending load scene command to clients.");

        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write((byte)1);
            writer.Write("GameScene");

            byte[] data = ms.ToArray();

            foreach (var client in connectedClients)
                udpServer.Send(data, data.Length, client);
        }
    }
}
