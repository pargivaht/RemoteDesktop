using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NAudio.Wave;

class Program
{
    static void Main()
    {
        // Hardcoded server IP and port
        string serverIP = "172.22.28.145"; // Change to your server's IP address
        int port = 8888; // Change to your desired port

        try
        {
            // Bind the UdpClient to the local port it will listen on
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            var udpClient = new UdpClient(localEndPoint);

            // Define the server endpoint from which to receive data
            var serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);

            Console.WriteLine("Waiting for audio format from the server...");
            byte[] formatData = udpClient.Receive(ref serverEndPoint); // Receive format info specifically from server
            string formatDetails = Encoding.ASCII.GetString(formatData);
            var formatParts = formatDetails.Split(',');
            int sampleRate = int.Parse(formatParts[0]);
            int channels = int.Parse(formatParts[1]);
            int bitDepth = int.Parse(formatParts[2]);

            var waveFormat = new WaveFormat(sampleRate, bitDepth, channels);
            var bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
            var waveOut = new WaveOutEvent();
            waveOut.Init(bufferedWaveProvider);
            waveOut.Play();

            Console.WriteLine("Receiving and playing audio...");
            while (true)
            {
                byte[] buffer = udpClient.Receive(ref serverEndPoint);
                bufferedWaveProvider.AddSamples(buffer, 0, buffer.Length);
            }
        }
        catch (FormatException fe)
        {
            Console.WriteLine($"Format error: {fe.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Ensure proper disposal of resources in the finally block
            // udpClient?.Close();
            // waveOut?.Stop();
            // waveOut?.Dispose();
        }
    }
}
