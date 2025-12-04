using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace BrickOps.Networking
{
    /// <summary>
    /// Encapsula la gestión básica de sockets UDP para cliente y servidor.
    /// </summary>
    public class UdpTransport : IDisposable
    {
        private Socket socket;
        private readonly byte[] receiveBuffer = new byte[2048];

        public Socket Socket => socket;
        public IPEndPoint RemoteEndPoint { get; private set; }

        public bool IsReady => socket != null;

        public bool InitializeClient(IPAddress serverIp, int port)
        {
            Close();

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Blocking = false;
                RemoteEndPoint = new IPEndPoint(serverIp, port);
                return true;
            }
            catch (Exception)
            {
                Close();
                return false;
            }
        }

        public bool InitializeServer(int port)
        {
            Close();

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Blocking = false;
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                return true;
            }
            catch (Exception)
            {
                Close();
                return false;
            }
        }

        public bool TryReceive(out string message, out EndPoint sender)
        {
            message = null;
            sender = new IPEndPoint(IPAddress.Any, 0);

            if (socket == null)
                return false;

            try
            {
                int bytes = socket.ReceiveFrom(receiveBuffer, ref sender);
                if (bytes <= 0)
                    return false;

                message = NetworkProtocol.BytesToMessage(receiveBuffer, bytes);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public void Send(string message, EndPoint target)
        {
            if (socket == null || target == null || string.IsNullOrEmpty(message))
                return;

            byte[] data = NetworkProtocol.MessageToBytes(message);

            try
            {
                socket.SendTo(data, target);
            }
            catch (SocketException)
            {
                // Silenciar errores no críticos (ej: non-blocking)
            }
        }

        public void Broadcast(string message, IEnumerable<IPEndPoint> recipients, IPEndPoint exclude = null)
        {
            if (socket == null || recipients == null)
                return;

            byte[] data = NetworkProtocol.MessageToBytes(message);

            foreach (var client in recipients)
            {
                if (client == null || (exclude != null && client.Equals(exclude)))
                    continue;

                try
                {
                    socket.SendTo(data, client);
                }
                catch (SocketException)
                {
                    // Ignorar errores individuales
                }
            }
        }

        public void Close()
        {
            if (socket == null)
                return;

            try
            {
                socket.Close();
            }
            catch (Exception)
            {}
            finally
            {
                socket = null;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
