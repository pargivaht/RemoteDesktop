using System;
using System.Collections.Generic;
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
using System.Windows.Forms;
using NAudio.Wave;

namespace Client
{
    public partial class Form1 : Form
    {
        static TcpClient cameraClient;
        static NetworkStream cameraStream;

        private UdpClient udpClient; 
        private IPEndPoint screenEndpoint;
        private CancellationTokenSource cancellationTokenSource;

        static WaveOutEvent waveOut;
        static TcpClient audioClient;
        static BufferedWaveProvider bufferedWaveProvider;
        static TcpClient controlClient;
        static NetworkStream controlStream;

        static string info;

        private static string ip = "127.0.0.1";
        //private static string ip = "192.168.2.93";

        private bool MicImageSwitch = false;
        private bool PauseImageSwitch = false;
        private bool PauseCamSwitch = false;
        static int CurremtCameraIndex = 1;

        static NetworkStream stream;

        public Form1()
        {
            InitializeComponent();
            ConnectToServer();
        }

        public async void ConnectToServer()
        {
            try
            {
                udpClient = new UdpClient();
                screenEndpoint = new IPEndPoint(IPAddress.Parse(ip), 8888);
                //udpClient.Connect(screenEndpoint);

                byte[] connectionRequest = BitConverter.GetBytes(1); // Send a simple integer as a connection request
                udpClient.Send(connectionRequest, connectionRequest.Length, screenEndpoint);


                cameraClient = new TcpClient(ip, 8890);  // for camera
                cancellationTokenSource = new CancellationTokenSource();
                cameraStream = cameraClient.GetStream();

                controlClient = new TcpClient(ip, 8891); // Control commands
                controlStream = controlClient.GetStream();

                SendControlCommand("info");

                byte[] buffer = new byte[1024];
                int bytesRead = await controlStream?.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                for (int i = 0; i < 100;)
                {
                    if (response.StartsWith("info"))
                    {
                        info = response.Substring(4);
                        this.Text = info;
                        break;
                    }
                }

                Task.Run(() => ReceiveScreen(cancellationTokenSource.Token));

                Task.Run(() => ReceiveCameraStream(cancellationTokenSource.Token), cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error connecting to server: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }


        public static async Task PlayAudio(bool toggle)
        {
            try
            {
                if (toggle)
                {
                    audioClient = new TcpClient(ip, 8889);
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
                    audioClient?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing audio: {ex.Message}");
            }
        }

        public void SendData(string stringData, IPEndPoint endpoint)
        {
            byte[] data = Encoding.UTF8.GetBytes(stringData);
            Task.Run(() => SendDataToServer(data, endpoint));
        }

        public void SendControlCommand(string command)
        {
            byte[] data = Encoding.UTF8.GetBytes(command);
            Task.Run(() => SendControlCommandToServer(data));
        }

        public async void SendDataToServer(byte[] data, IPEndPoint endpoint)
        {
            try
            {
                if (udpClient != null)
                {
                    await udpClient.SendAsync(data, data.Length, endpoint);  // Send data via UDP
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending data to server: " + ex.Message);
            }
        }

        public async void SendControlCommandToServer(byte[] data)
        {
            try
            {
                if (controlClient == null || !controlClient.Connected)
                {
                    ConnectToServer();  // Reconnect if necessary
                }

                if (controlClient != null && controlClient.Connected)
                {
                    await controlStream.WriteAsync(data, 0, data.Length);
                    controlStream.Flush();  // Ensure immediate sending
                }
                else
                {
                    Console.WriteLine("Control client is not connected to the server.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending control command to server: " + ex.Message);
            }
        }

        private async Task ReceiveScreen(CancellationToken cancellationToken)
        {
            //while (!cancellationToken.IsCancellationRequested)
            //{
            //    try
            //    {
            //        // Step 1: Receive the number of chunks
            //        UdpReceiveResult totalChunksResult = await udpClient.ReceiveAsync();
            //        int totalChunks = BitConverter.ToInt32(totalChunksResult.Buffer, 0);
            //        Console.WriteLine($"Expected total chunks: {totalChunks}");

            //        // Step 2: Create a buffer to hold the entire image data
            //        List<byte> imageData = new List<byte>();

            //        // Step 3: Receive all chunks
            //        for (int i = 0; i < totalChunks; i++)
            //        {
            //            UdpReceiveResult chunkResult = await udpClient.ReceiveAsync();
            //            if (chunkResult.Buffer.Length <= 4)
            //            {
            //                Console.WriteLine("Unexpected small chunk, skipping...");
            //                continue;
            //            }

            //            imageData.AddRange(chunkResult.Buffer);
            //            Console.WriteLine($"Received chunk {i + 1}/{totalChunks} - {chunkResult.Buffer.Length} bytes");
            //        }

            //        // Step 4: Reconstruct the image from the received data
            //        using (MemoryStream ms = new MemoryStream(imageData.ToArray()))
            //        {
            //            Image screenshot = Image.FromStream(ms);
            //            UpdateImage(screenshot);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("Error receiving screen: " + ex.Message);
            //    }
            //}


            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0); // EndPoint to receive from any server

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Receive the expected length first
                    byte[] lengthBytes = udpClient.Receive(ref remoteEndPoint);
                    if (lengthBytes.Length < 4)
                    {
                        Console.WriteLine("Received length data is invalid.");
                        continue; // Skip this iteration
                    }
                    int length = BitConverter.ToInt32(lengthBytes, 0);
                    if (length <= 0 || length > 10 * 1024 * 1024) // Validate length
                    {
                        Console.WriteLine("Received invalid frame length, discarding frame.");
                        continue; // Skip this iteration
                    }

                    // Now receive the actual image data
                    List<byte> receivedData = new List<byte>();
                    int totalBytesReceived = 0;

                    while (totalBytesReceived < length)
                    {
                        byte[] data = udpClient.Receive(ref remoteEndPoint);
                        receivedData.AddRange(data);
                        totalBytesReceived += data.Length;

                        if (totalBytesReceived > length)
                        {
                            Console.WriteLine("Received more data than expected, discarding excess.");
                            break; // Discard excess data
                        }
                    }

                    if (totalBytesReceived == length)
                    {
                        try
                        {
                            //DisplayScreenshot(receivedData.ToArray());

                            using (MemoryStream ms = new MemoryStream(receivedData.ToArray()))
                            {
                                Image screenshot = Image.FromStream(ms);
                                UpdateImage(screenshot);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error displaying image: " + ex.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Received data size mismatch, discarding frame.");
                    }
                }
                catch (SocketException socketEx)
                {
                    Console.WriteLine("Socket error: " + socketEx.Message);
                }
                catch (ObjectDisposedException disposedEx)
                {
                    Console.WriteLine("Socket was disposed: " + disposedEx.Message);
                    udpClient = new UdpClient(); // Re-establish connection
                }
                catch (Exception ex)
                {
                    Console.WriteLine("General error receiving data: " + ex.Message);
                }

                //Thread.Sleep(10); // Optional delay
            }

        }



        private void UpdateImage(Image image)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Image>(UpdateImage), image);
                return;
            }

            if (image != null)
            {
                pictureBox.Image?.Dispose(); // Dispose the previous image
                pictureBox.Image = image; // Update the PictureBox with the new image
                pictureBox.Refresh(); // Force the PictureBox to redraw
            }
            else
            {
                Console.WriteLine("Received null image."); // Debugging line
            }
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
                        UpdateCameraImage(cameraImage);
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

        private void UpdateCameraImage(Image image)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Image>(UpdateCameraImage), image);
                return;
            }

            pictureBox1.Image = image;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            cancellationTokenSource?.Cancel();
            udpClient?.Close();
            controlClient?.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                MicBtn.BackgroundImage = Client.Properties.Resources.mic_off;
                MicImageSwitch = false;
                Task.Run(() => PlayAudio(false));

            
                udpClient?.Close();
                audioClient?.Close();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private async void MicBtn_Click(object sender, EventArgs e)
        {
            if (!MicImageSwitch)
            {
                MicBtn.BackgroundImage = Client.Properties.Resources.mic_on;
                MicImageSwitch = true;
                Task.Run(() => PlayAudio(true));

            }
            else
            {
                MicBtn.BackgroundImage = Client.Properties.Resources.mic_off;
                MicImageSwitch = false;
                Task.Run(() => PlayAudio(false));

            }
        }

        private void ReconnectBtn_Click(object sender, EventArgs e)
        {

            MicBtn.BackgroundImage = Client.Properties.Resources.mic_off;
            MicImageSwitch = false;
            Task.Run(() => PlayAudio(false));
            ConnectToServer();
        }

        private void pauseScreenBtn_Click(object sender, EventArgs e)
        {
            if (!PauseImageSwitch)
            {
                pauseScreenBtn.BackgroundImage = Client.Properties.Resources.play;
                PauseImageSwitch = true;
                SendControlCommand("pauseScreen");
            }
            else
            {
                pauseScreenBtn.BackgroundImage = Client.Properties.Resources.pause;
                PauseImageSwitch = false;
                SendControlCommand("resumeScreen");
            }
        }

        private void fpsTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (fpsTrackBar.Value == 0)
            {
                SendControlCommand("fps1");
                fpsLabel.Text = "Fps: 1";
            }
            else
            {
                SendControlCommand("fps" + fpsTrackBar.Value.ToString());
                fpsLabel.Text = "Fps:" + fpsTrackBar.Value.ToString();
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void SwitchCamera(int cameraIndex)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes("switch" + cameraIndex);
            cameraStream.Write(data, 0, data.Length);
        }

        private void SwitchBtn_Click(object sender, EventArgs e)
        {
            SwitchCamera(CurremtCameraIndex);
            CurremtCameraIndex++;
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (trackBar1.Value <= 0)
            {
                SendControlCommand("camfps1");
                label1.Text = "Fps: 1";
            }
            else
            {
                SendControlCommand("camfps" + trackBar1.Value.ToString());
                label1.Text = "Fps:" + trackBar1.Value.ToString();
                Thread.Sleep(100);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!PauseCamSwitch)
            {
                button4.BackgroundImage = Client.Properties.Resources.play;
                PauseCamSwitch = true;
                SendControlCommand("pauseCam");
            }
            else
            {
                button4.BackgroundImage = Client.Properties.Resources.pause;
                PauseCamSwitch = false;
                SendControlCommand("resumeCam");
            }
        }
    }
}
