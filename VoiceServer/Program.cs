using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NAudio.Wave;

class Program
{
    static void Main(string[] args)
    {
        int port = 8888;
        var serverEndPoint = new IPEndPoint(IPAddress.Broadcast, port);  // Use Broadcast or change to specific client IP if known
        var udpClient = new UdpClient();

        using (var capture = new WasapiLoopbackCapture())
        {
            // Wait for the capture to initialize and start
            capture.StartRecording();

            // Send the format details as a simple string
            string formatDetails = $"{capture.WaveFormat.SampleRate},{capture.WaveFormat.Channels},{capture.WaveFormat.BitsPerSample}";
            byte[] formatBytes = Encoding.ASCII.GetBytes(formatDetails);
            udpClient.Send(formatBytes, formatBytes.Length, serverEndPoint);

            Console.WriteLine("Sent format details. Now broadcasting audio...");

            // Event handler for sending audio data
            capture.DataAvailable += (sender, e) =>
            {
                udpClient.Send(e.Buffer, e.BytesRecorded, serverEndPoint);
            };

            // Keep the server running until the user decides to stop
            Console.WriteLine("Broadcasting audio. Press any key to stop...");
            Console.ReadKey();

            capture.StopRecording();
        }

        udpClient.Close();
    }
}
