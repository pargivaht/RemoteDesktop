using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class StunClient
{
    private const int STUN_PORT = 3478; // Default STUN port
    private static readonly IPEndPoint stunServerEndPoint = new IPEndPoint(IPAddress.Parse("stun.l.google.com"), STUN_PORT); // Public STUN server

    public static void Main(string[] args)
    {
        using (UdpClient udpClient = new UdpClient())
        {
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0)); // Bind to any available port

            // Create a STUN binding request
            byte[] bindingRequest = BuildBindingRequest();

            // Send the request to the STUN server
            udpClient.Send(bindingRequest, bindingRequest.Length, stunServerEndPoint);
            Console.WriteLine("Sent STUN Binding Request to STUN server.");

            // Receive the response
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] responseBytes = udpClient.Receive(ref remoteEndPoint);
            Console.WriteLine($"Received STUN response from {remoteEndPoint}.");

            // Parse the response
            ParseStunResponse(responseBytes);
        }
    }

    private static byte[] BuildBindingRequest()
    {
        // Create a basic STUN binding request (format specified in RFC 5389)
        byte[] request = new byte[20];
        request[0] = 0x00; // Message Type: Binding Request
        request[1] = 0x01; // Message Type: Binding Request
        request[2] = 0x00; // Message Length: 0
        request[3] = 0x00; // Message Length: 0
        // Transaction ID (random 12 bytes)
        var random = new Random();
        byte[] transactionId = new byte[12];
        random.NextBytes(transactionId);
        Buffer.BlockCopy(transactionId, 0, request, 4, transactionId.Length);
        return request;
    }

    private static void ParseStunResponse(byte[] responseBytes)
    {
        // Check the message type
        if (responseBytes[0] == 0x01 && responseBytes[1] == 0x01)
        {
            // The response is a Binding Response
            // The mapped address is found in the response starting at byte 28 (RFC 5389)
            if (responseBytes.Length >= 32) // Ensure enough data for the address
            {
                byte[] ipBytes = new byte[4];
                Buffer.BlockCopy(responseBytes, 28, ipBytes, 0, 4);
                string publicIp = string.Join(".", ipBytes);
                ushort port = (ushort)((responseBytes[32] << 8) + responseBytes[33]); // Port is 2 bytes

                Console.WriteLine($"Public IP: {publicIp}");
                Console.WriteLine($"Public Port: {port}");
            }
            else
            {
                Console.WriteLine("Invalid STUN response length.");
            }
        }
        else
        {
            Console.WriteLine("Received non-Binding Response.");
        }
    }
}
