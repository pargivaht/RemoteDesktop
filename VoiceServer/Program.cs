using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Open.Nat;


class ScreenStreamer
{
    private const int Port = 8888;
    private UdpClient udpClient;
    private IPEndPoint clientEndPoint;

    public async void Start()
    {
        await UPnP(Port, Protocol.Udp, "ScreenShare", true);

        udpClient = new UdpClient(Port); // Listen on the specific port
        Console.WriteLine("Server is waiting for connection request...");

        // Wait for a connection request (from any client)
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, Port);
        byte[] connectionRequest = udpClient.Receive(ref remoteEndPoint);

        // Set the client endpoint to communicate with
        if (connectionRequest.Length > 0)
        {
            clientEndPoint = remoteEndPoint;
            Console.WriteLine("Connection established with client: " + clientEndPoint.ToString());
        }
        while (true)
        {
            Bitmap screenshot = CaptureScreen();
            SendScreenshot(screenshot);
            Thread.Sleep(1000 / 144); // Adjust the delay for your needs
        }
    }

    static Bitmap CaptureScreen()
    {
        Rectangle bounds = Screen.PrimaryScreen.Bounds;
        Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height);

        using (Graphics g = Graphics.FromImage(screenshot))
        {
            g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);

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

    private void SendScreenshot(Bitmap screenshot)
    {
        var qualityParam = new EncoderParameter(Encoder.Quality, 50L); // Adjust quality (1-100)
        var jpegCodec = GetEncoder(ImageFormat.Jpeg);
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = qualityParam;

        using (var ms = new MemoryStream())
        {
            // Save the bitmap to the memory stream in JPEG format with compression
            screenshot.Save(ms, jpegCodec, encoderParams);
            byte[] imageData = ms.ToArray();

            // Send the total length as an integer first
            byte[] lengthBytes = BitConverter.GetBytes(imageData.Length);
            udpClient.Send(lengthBytes, lengthBytes.Length, clientEndPoint);

            // Chunk and send image data in smaller packets
            int packetSize = 506;
            for (int i = 0; i < imageData.Length; i += packetSize)
            {
                int remainingBytes = imageData.Length - i;
                int currentPacketSize = Math.Min(packetSize, remainingBytes);
                byte[] packetData = new byte[currentPacketSize];
                Array.Copy(imageData, i, packetData, 0, currentPacketSize);
                udpClient.Send(packetData, currentPacketSize, clientEndPoint);
            }
        }
    }

    private ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return null;
    }

    public static async Task UPnP(int Port, Protocol protocol, string name, bool swich)
    {
        try
        {
            var nat = new NatDiscoverer();

            var cts = new CancellationTokenSource(10000);

            Console.WriteLine("Searching for nat device...");

            var device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            Console.WriteLine("Nat device found...");

            Console.WriteLine("Searching for mappings...");

            var existingMappings = await device.GetAllMappingsAsync();

            Console.WriteLine("Mappings found...");

            var existingMapping = existingMappings.FirstOrDefault(m => m.PrivatePort == Port && m.PublicPort == Port && m.Protocol == protocol);

            if (swich)
            {
                Console.WriteLine("Attempting to create new mapping...");

                if (existingMapping == null)
                {
                    await device.CreatePortMapAsync(new Mapping(protocol, Port, Port, name)); //create
                    Console.WriteLine("A new mapping created...");
                }
                else
                {
                    Console.WriteLine($"Port {Port}/{protocol} is already mapped.");
                }
            }
            else
            {
                Console.WriteLine("Deleting mapping...");

                await device.DeletePortMapAsync(new Mapping(protocol, Port, Port)); //delete

                Console.WriteLine("Mappig deleted...");
            }

        }
        catch (NatDeviceNotFoundException)
        {
            Console.WriteLine("No UPnP-compatible NAT device was found on the network.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UPnP: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("UPnP done...");
        }
    }

    public static void Main()
    {
        ScreenStreamer streamer = new ScreenStreamer();
        streamer.Start();

        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
    }
}
