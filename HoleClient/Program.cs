//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;

//class StunClient
//{
//    private const int STUN_PORT = 3478; // Default STUN port
//    private static readonly IPEndPoint stunServerEndPoint = new IPEndPoint(IPAddress.Parse("stun.l.google.com"), STUN_PORT); // Public STUN server

//    public static void Main(string[] args)
//    {
//        using (UdpClient udpClient = new UdpClient())
//        {
//            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0)); // Bind to any available port

//            // Create a STUN binding request
//            byte[] bindingRequest = BuildBindingRequest();

//            // Send the request to the STUN server
//            udpClient.Send(bindingRequest, bindingRequest.Length, stunServerEndPoint);
//            Console.WriteLine("Sent STUN Binding Request to STUN server.");

//            // Receive the response
//            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
//            byte[] responseBytes = udpClient.Receive(ref remoteEndPoint);
//            Console.WriteLine($"Received STUN response from {remoteEndPoint}.");

//            // Parse the response
//            ParseStunResponse(responseBytes);
//        }
//    }

//    private static byte[] BuildBindingRequest()
//    {
//        // Create a basic STUN binding request (format specified in RFC 5389)
//        byte[] request = new byte[20];
//        request[0] = 0x00; // Message Type: Binding Request
//        request[1] = 0x01; // Message Type: Binding Request
//        request[2] = 0x00; // Message Length: 0
//        request[3] = 0x00; // Message Length: 0
//        // Transaction ID (random 12 bytes)
//        var random = new Random();
//        byte[] transactionId = new byte[12];
//        random.NextBytes(transactionId);
//        Buffer.BlockCopy(transactionId, 0, request, 4, transactionId.Length);
//        return request;
//    }

//    private static void ParseStunResponse(byte[] responseBytes)
//    {
//        // Check the message type
//        if (responseBytes[0] == 0x01 && responseBytes[1] == 0x01)
//        {
//            // The response is a Binding Response
//            // The mapped address is found in the response starting at byte 28 (RFC 5389)
//            if (responseBytes.Length >= 32) // Ensure enough data for the address
//            {
//                byte[] ipBytes = new byte[4];
//                Buffer.BlockCopy(responseBytes, 28, ipBytes, 0, 4);
//                string publicIp = string.Join(".", ipBytes);
//                ushort port = (ushort)((responseBytes[32] << 8) + responseBytes[33]); // Port is 2 bytes

//                Console.WriteLine($"Public IP: {publicIp}");
//                Console.WriteLine($"Public Port: {port}");
//            }
//            else
//            {
//                Console.WriteLine("Invalid STUN response length.");
//            }
//        }
//        else
//        {
//            Console.WriteLine("Received non-Binding Response.");
//        }
//    }
//}



//using System;
//using System.IO;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using NAudio.Wave;

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        await PlayTTS("Kell oli juba hiline, kui Kärt jõudis koju. ", Speakers.meelis);
//    }
//public static async Task PlayTTS(string text, Speakers speaker, float speed = 1)
//    {
//        if (speed > 2)
//        {
//            speed = 2;
//        }
//        else if(speed<0.5)
//        {
//            speed = 0.5f;
//        }
//        HttpClient client = new HttpClient();

//        var payload = new
//        {
//            text,
//            speaker = speaker.ToString().ToLowerInvariant(),
//            speed
//        };

//        var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

//        try
//        {
//            // Send POST request to TTS API
//            HttpResponseMessage response = await client.PostAsync("https://api.tartunlp.ai/text-to-speech/v2/", content);
//            response.EnsureSuccessStatusCode();

//            // Read the response stream
//            using (Stream audioStream = await response.Content.ReadAsStreamAsync())
//            {
//                // Play the audio stream using NAudio
//                using (var waveOut = new WaveOutEvent())
//                using (var waveReader = new WaveFileReader(audioStream))
//                {
//                    waveOut.Init(waveReader);
//                    waveOut.Play();

//                    // Wait for playback to finish
//                    while (waveOut.PlaybackState == PlaybackState.Playing)
//                    {
//                        await Task.Delay(500);
//                    }
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"An error occurred: {ex.Message}");
//        }
//    }

//}

//enum Speakers
//{
//    albert,
//    indrek,
//    kalev,
//    kylli,
//    lee,
//    liivika,
//    luukas,
//    mari,
//    meelis,
//    peeter,
//    tambet,
//    vesta
//}




using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main()
    {
        // Set DPI awareness for correct screen scaling
        SetProcessDPIAware();

        try
        {
            Bitmap screenshot = CaptureScreen();
            string downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string filePath = Path.Combine(downloadsPath, "screenshot.png");

            screenshot.Save(filePath, ImageFormat.Png);
            screenshot.Dispose();

            Console.WriteLine($"Screenshot saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static Bitmap CaptureScreen()
    {
        Rectangle bounds = GetPhysicalScreenBounds();

        Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height);
        using (Graphics g = Graphics.FromImage(screenshot))
        {
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

            Cursor currentCursor = Cursors.Default;
            if (currentCursor != null)
            {
                Point cursorPos = Cursor.Position;
                Rectangle cursorBounds = new Rectangle(cursorPos, currentCursor.Size);
                currentCursor.Draw(g, cursorBounds);
            }
        }

        return screenshot;
    }

    static Rectangle GetPhysicalScreenBounds()
    {
        // This gets the actual pixel bounds regardless of scaling
        return new Rectangle(
            0, 0,
            Screen.PrimaryScreen.Bounds.Width,
            Screen.PrimaryScreen.Bounds.Height
        );
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();
}
