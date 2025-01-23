using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ChatServer
{
    class Program
    {
        class MessagePacket
        {
            public int type { get; set; }
            public string Name { get; set; }
            public string Message { get; set; }
            public DateTime date { get; set; }
        }

        class ReceivePacket
        {
            public int type { get; set; }
            public string Name { get; set; }
            public string Message { get; set; }
            public DateTime date { get; set; }
            public List<string> clients { get; set; }
        }

        //Chat Type
        enum C_TYPE
        {
            MESSAGE = 0,
            IMAGE,
            LIST
        }

        static List<Socket> connectedClients = new List<Socket>();
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server...");
            StartServer();
        }

        static void StartServer()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 12345));
            serverSocket.Listen(10);

            Console.WriteLine("Server is running. Waiting for clients...");

            serverSocket.BeginAccept(AcceptCallback, serverSocket);
            Console.ReadLine();
        }

        static void AcceptCallback(IAsyncResult ar)
        {
            Socket serverSocket = (Socket)ar.AsyncState;
            Socket clientSocket = serverSocket.EndAccept(ar);

            connectedClients.Add(clientSocket);
            Console.WriteLine($"Client connected: {clientSocket.RemoteEndPoint}");

            var asyncObj = new AsyncObject(5000);
            asyncObj.WorkingSocket = clientSocket;
            clientSocket.BeginReceive(asyncObj.Buffer, 0, asyncObj.BufferSize, 0, ReceiveCallback, asyncObj);

            serverSocket.BeginAccept(AcceptCallback, serverSocket);
        }

        static void ReceiveCallback(IAsyncResult ar)
        {
            var asyncObj = (AsyncObject)ar.AsyncState;
            int received = asyncObj.WorkingSocket.EndReceive(ar);

            if (received > 0)
            {
                string strClientMessage = Encoding.UTF8.GetString(asyncObj.Buffer, 0, received);
                MessagePacket temp = JsonSerializer.Deserialize<MessagePacket>(strClientMessage);
                ReceivePacket packet = new ReceivePacket();
                packet.date = temp.date;
                packet.Name = temp.Name;
                packet.type = temp.type;
                packet.Message = temp.Message;
                packet.clients = new List<string>();

                for(int i=0;i< connectedClients.Count; i++)
                {
                    packet.clients.Add(connectedClients[i].RemoteEndPoint.ToString());
                }

                string text = JsonSerializer.Serialize(packet);

                switch (packet.type)
                {
                    case (int)C_TYPE.MESSAGE:
                        Console.WriteLine($"Received: {text}");
                        BroadcastMessage(text);
                        break;

                    case (int)C_TYPE.IMAGE:
                        string base64Image = packet.Message;
                        Console.WriteLine("Received an image.");

                        // Decode and save the image
                        byte[] imageBytes = Convert.FromBase64String(base64Image);
                        System.IO.File.WriteAllBytes($"received_{DateTime.Now.Ticks}.jpg", imageBytes);
                        BroadcastMessage(text);
                        break;

                    case (int)C_TYPE.LIST:
                        break;

                }
            }

            asyncObj.WorkingSocket.BeginReceive(asyncObj.Buffer, 0, asyncObj.BufferSize, 0, ReceiveCallback, asyncObj);
        }

        static void BroadcastMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            foreach (var client in connectedClients)
            {
                if (client.Connected)
                {
                    try
                    {
                        client.Send(data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending to client: {ex.Message}");
                    }
                }
            }
        }
    }

    class AsyncObject
    {
        public byte[] Buffer { get; private set; }
        public Socket WorkingSocket { get; set; }
        public int BufferSize { get; private set; }

        public AsyncObject(int bufferSize)
        {
            Buffer = new byte[bufferSize];
            BufferSize = bufferSize;
        }
    }
}
