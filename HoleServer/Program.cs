using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class UdpServer
{
    private UdpClient udpServer;

    public UdpServer(int port)
    {
        udpServer = new UdpClient(port);
    }

    public void Start()
    {
        Console.WriteLine("UDP Server started...");
        while (true)
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receivedData = udpServer.Receive(ref clientEndPoint);
            string message = Encoding.UTF8.GetString(receivedData);
            Console.WriteLine($"Received message: {message} from {clientEndPoint}");

            // Send back the client's endpoint to itself and others
            SendClientAddress(clientEndPoint);
        }
    }

    private void SendClientAddress(IPEndPoint clientEndPoint)
    {
        string responseMessage = $"{clientEndPoint.Address}:{clientEndPoint.Port}";
        byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);

        // Send back to the client
        udpServer.Send(responseBytes, responseBytes.Length, clientEndPoint);
    }

    static void Main(string[] args)
    {
        UdpServer server = new UdpServer(12345);
        server.Start();
    }
}
