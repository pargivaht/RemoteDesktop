using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using MessageBox = System.Windows.MessageBox;
using Image = System.Drawing.Image;
using Client2;

public class Connection
{
    public static string Ip { get; set; }
    public static int Port { get; set; }
    public static string Password { get; set; }

    public Page Page { get; }

    public MainWindow Window { get; }

    public event Action<Image> ScreenUpdated;
    public event Action<Image> CameraUpdated;


    static TcpClient cameraClient;
    static NetworkStream cameraStream;

    public TcpClient client;
    private CancellationTokenSource cancellationTokenSource;

    static WaveOutEvent waveOut;
    static TcpClient client2;
    static BufferedWaveProvider bufferedWaveProvider;

    static string info;

    private bool MicImageSwitch = false;
    private bool PauseImageSwitch = false;
    private bool PauseCamSwitch = false;
    static int CurremtCameraIndex = 1;

    private static int fps = 60;
    private static int compression = 100;

    static NetworkStream stream;

    public Connection(string ip, int port, string password, Page page, MainWindow window)
    {
        Ip = ip;
        Port = port;
        Password = password;
        Page = page;
        Window = window;
        ConnectToServer();
    }

    public async void ConnectToServer()
    {
        try
        {
            client = new TcpClient(Ip, 8888);  // for screen
            cameraClient = new TcpClient(Ip, 8890);  // for camera
            cancellationTokenSource = new CancellationTokenSource();
            stream = client.GetStream();
            cameraStream = cameraClient.GetStream();

            SendData("info");

            byte[] buffer = new byte[1024];
            int bytesRead = await stream?.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            for (int i = 0; i < 100;)
            {
                if (response.StartsWith("info"))
                {
                    info = response.Substring(4);
                    Window.SetTitle(info);
                    break;
                }
            }

            Task.Run(() => ReceiveScreen(cancellationTokenSource.Token));

            //Task.Run(() => ReceiveCameraStream(cancellationTokenSource.Token), cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error connecting to server: " + ex.Message);
        }
    }


    public static async Task PlayAudio(bool toggle)
    {
        try
        {
            if (toggle)
            {
                client2 = new TcpClient(Ip, 8889);
                Console.WriteLine("Connected to audio server.");

                waveOut = new WaveOutEvent();

                bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1)); // 44.1 kHz, mono
                waveOut.Init(bufferedWaveProvider);

                waveOut.Play();
                Console.WriteLine("Playing audio...");

                using (BinaryReader reader = new BinaryReader(client2.GetStream()))
                {
                    while (client2.Connected)
                    {
                        // Read the length of the audio data
                        int length = reader.ReadInt32();

                        // Read the audio data
                        byte[] audioData = reader.ReadBytes(length);

                        // Write the audio data to the BufferedWaveProvider
                        bufferedWaveProvider.AddSamples(audioData, 0, length);
                    }
                }
            }
            else
            {
                waveOut?.Stop();
                client2?.Dispose();
            }
        }
        catch //(Exception ex)
        {
            //MessageBox.Show($"Error playing audio: {ex.Message}");
        }
    }

    public void SendData(string stringData)
    {
        byte[] data = Encoding.UTF8.GetBytes(stringData);
        Task.Run(() => SendDataToServer(data));
    }

    public async void SendDataToServer(byte[] data)
    {
        try
        {
            if (client == null || !client.Connected)
            {
                ConnectToServer();
            }

            if (client != null && client.Connected)
            {
                stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
                stream.Flush(); // Flush the stream to send the data immediately
            }
            else
            {
                Console.WriteLine("Client is not connected to the server.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending data to server: " + ex.Message);
        }
    }

    private async Task ReceiveScreen(CancellationToken cancellationToken)
    {

        SendData("resumeScreen");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Receive screenshot size
                byte[] sizeBytes = new byte[4];
                await stream.ReadAsync(sizeBytes, 0, sizeBytes.Length, cancellationToken);
                int imageSize = BitConverter.ToInt32(sizeBytes, 0);

                // Receive screenshot data
                byte[] imageData = new byte[imageSize];
                int totalBytesRead = 0;
                while (totalBytesRead < imageSize)
                {
                    int bytesRead = await stream.ReadAsync(imageData, totalBytesRead, imageSize - totalBytesRead, cancellationToken);
                    if (bytesRead == 0)
                    {
                        throw new IOException("End of stream reached while reading image data.");
                    }
                    totalBytesRead += bytesRead;
                }

                // Create image from received data
                using (MemoryStream ms = new MemoryStream(imageData))
                {
                    ms.Position = 0; // Set stream position to beginning
                    Image screenshot = Image.FromStream(ms);
                    ScreenUpdated.Invoke(screenshot); // Update the screen image (invoke on UI thread
                }

                await Task.Delay(1000 / fps, cancellationToken); // Adjust the interval as needed
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation and exit the loop
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error receiving screen: " + ex.Message);
                break;
            }
        }

        // Clean up resources
        stream.Close();
        client.Close();
    }


    private async Task ReceiveCameraStream(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Receive the size of the image first
                byte[] sizeBytes = new byte[4];
                await cameraStream.ReadAsync(sizeBytes, 0, sizeBytes.Length, cancellationToken);
                int imageSize = BitConverter.ToInt32(sizeBytes, 0);

                // Receive the image data
                byte[] imageData = new byte[imageSize];
                int totalBytesRead = 0;
                while (totalBytesRead < imageSize)
                {
                    int bytesRead = await cameraStream.ReadAsync(imageData, totalBytesRead, imageSize - totalBytesRead, cancellationToken);
                    if (bytesRead == 0)
                    {
                        throw new IOException("End of stream reached while reading camera data.");
                    }
                    totalBytesRead += bytesRead;
                }

                using (MemoryStream ms = new MemoryStream(imageData))
                {
                    ms.Position = 0;
                    Image cameraImage = Image.FromStream(ms);
                    CameraUpdated.Invoke(cameraImage);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation and exit the loop
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error receiving camera stream: " + ex.Message);
                break;
            }
        }

        cameraStream.Close();
        cameraClient.Close();
    }

    public void ChangeFps(int _fps)
    {
        if (_fps == 0)
        {
            fps = 1;
            SendData("fps1");
        }
        else
        {
            fps = _fps;
            SendData("fps" + _fps.ToString());
            Thread.Sleep(100);
        }
    }

    public void Compression(int _compression)
    {
        if (_compression == 0)
        {
            compression = 1;
            SendData("compression1");
        }
        else
        {
            compression = _compression;
            SendData("compression1" + _compression.ToString());
            Thread.Sleep(100);
        }
    }

    public void ChangeCamera()
    {
        CurremtCameraIndex++;
        if (CurremtCameraIndex > 2)
        {
            CurremtCameraIndex = 1;
        }
        SendData("cam" + CurremtCameraIndex.ToString());
    }

    public void PauseCamera()
    {
        if (PauseCamSwitch)
        {
            SendData("resumeCam");
            PauseCamSwitch = false;
        }
        else
        {
            SendData("pauseCam");
            PauseCamSwitch = true;
        }
    }

    public void Pause()
    {
        if (PauseImageSwitch)
        {
            SendData("resumeScreen");
            PauseImageSwitch = false;
        }
        else
        {
            SendData("pauseScreen");
            PauseImageSwitch = true;
        }
    }

    public void Mic(bool Switch)
    {
        if (Switch)
        {

            Task.Run(() => PlayAudio(true));
            MicImageSwitch = true;
        }
        else
        {
            Task.Run(() => PlayAudio(false));
            MicImageSwitch = false;
        }
    }

    public void Reconnect()
    {
        MicImageSwitch = false;
        Task.Run(() => PlayAudio(false));
        ConnectToServer();
    }

    public void Disconnect()
    {
        cancellationTokenSource.Cancel();
        MicImageSwitch = false;
        Task.Run(() => PlayAudio(false));
    }

}
