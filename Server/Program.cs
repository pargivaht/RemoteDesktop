﻿using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using System.Diagnostics;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Linq;
using System.Drawing.Imaging;

namespace Server
{
    class Program
    {
        static TcpListener screenListener;
        static TcpListener voiceListener;
        static TcpListener cameraListener;

        static FilterInfoCollection videoDevices;
        static int currentDeviceIndex = 1;
        static VideoCaptureDevice videoSource;

        static bool isScreenSharingPaused = false;

        static public int fps = 60;
        static public int compression = 100;

        static string info = "Remote Desktop: " + Environment.UserName + "@" + Environment.MachineName + ". Monitor Count: " + SystemInformation.MonitorCount;

        static async Task Main(string[] args)
        {
            screenListener = new TcpListener(IPAddress.Any, 8888);
            screenListener.Start();
            Console.WriteLine("Screen sharing server started.");

            voiceListener = new TcpListener(IPAddress.Any, 8889);
            voiceListener.Start();
            Console.WriteLine("Voice sharing server started.");

            cameraListener = new TcpListener(IPAddress.Any, 8890);
            cameraListener.Start();
            Console.WriteLine("Camera sharing server started.");

            Task.WaitAll(
                AcceptClientsAsync(screenListener, HandleScreenClientAsync),
                AcceptClientsAsync(voiceListener, HandleVoiceClientAsync),
                AcceptClientsAsync(cameraListener, HandleCameraClientAsync)
            );
        }

        private static async Task AcceptClientsAsync(TcpListener listener, Func<TcpClient, Task> handleClient)
        {
            try
            {
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");

                    _ = handleClient(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting clients: {ex.Message}");
            }
        }

        private static async Task HandleCameraClientAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            var videoSources = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            
            int selectedCameraIndex = 0;  // Start with the first camera

            if (videoSources.Count == 0)
            {
                Console.WriteLine("No video sources found.");
                return;
            }

            try
            {
                videoSource = new VideoCaptureDevice(videoSources[selectedCameraIndex].MonikerString);
                //StartCamera(videoSource, stream);

                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) continue;  // No data received

                    string command = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    if (command.StartsWith("switch"))
                    {
                        int newCameraIndex;
                        if (int.TryParse(command.Substring(6).Trim(), out newCameraIndex))
                        {
                            newCameraIndex = (newCameraIndex % videoSources.Count + videoSources.Count) % videoSources.Count;  // Wrap-around logic
                            if (newCameraIndex != selectedCameraIndex)
                            {
                                videoSource.SignalToStop();
                                videoSource.WaitForStop();
                                selectedCameraIndex = newCameraIndex;
                                videoSource = new VideoCaptureDevice(videoSources[selectedCameraIndex].MonikerString);
                                StartCamera(videoSource, stream);
                                Console.WriteLine($"Switched to camera {selectedCameraIndex}");
                            }
                            else
                            {
                                Console.WriteLine("Switching to the same camera, adjusting index...");
                                selectedCameraIndex = (selectedCameraIndex + 1) % videoSources.Count;  // Force switch to next camera
                                videoSource.SignalToStop();
                                videoSource.WaitForStop();
                                videoSource = new VideoCaptureDevice(videoSources[selectedCameraIndex].MonikerString);
                                StartCamera(videoSource, stream);
                                Console.WriteLine($"Automatically switched to camera {selectedCameraIndex}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling camera client: {ex.Message}");
            }
            finally
            {
                videoSource?.SignalToStop();
                videoSource?.WaitForStop();
                client.Close();
                stream.Close();
                Console.WriteLine("Camera server has cleaned up the client connection.");
            }
        }

        private static void StartCamera(VideoCaptureDevice videoSource, NetworkStream stream, int desiredWidth = 640, int desiredHeight = 480, int desiredFps = 15)
        {
            // Check for supported resolutions and select one
            var capabilities = videoSource.VideoCapabilities;
            VideoCapabilities selectedCapability = null;

            // Attempt to find the exact match for resolution and frame rate
            foreach (var cap in capabilities)
            {
                if (cap.FrameSize.Width == desiredWidth &&
                    cap.FrameSize.Height == desiredHeight &&
                    cap.MaximumFrameRate >= desiredFps)
                {
                    if (selectedCapability == null || cap.MaximumFrameRate < selectedCapability.MaximumFrameRate)
                    {
                        selectedCapability = cap;
                    }
                }
            }

            if (selectedCapability != null)
            {
                videoSource.VideoResolution = selectedCapability;
                Console.WriteLine($"Selected camera resolution: {selectedCapability.FrameSize.Width}x{selectedCapability.FrameSize.Height} at up to {selectedCapability.MaximumFrameRate} fps");
            }
            else
            {
                Console.WriteLine("Desired resolution and frame rate not supported. Using default settings.");
            }

            videoSource.NewFrame += (sender, eventArgs) =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    eventArgs.Frame.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] imageData = ms.ToArray();
                    SendImageSize(stream, imageData.Length);
                    SendImageData(stream, imageData);
                }
            };
            videoSource.Start();
        }



        private static async Task HandleScreenClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    Task receivingTask = ReceiveDataFromClientAsync(stream);
                    Task sendingTask = SendScreenSharingFramesAsync(stream);

                    await Task.WhenAll(receivingTask, sendingTask);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        private static async Task HandleVoiceClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    var waveIn = new WaveInEvent();
                    waveIn.WaveFormat = new WaveFormat(44100, 1);

                    waveIn.DataAvailable += (sender, e) =>
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            ms.Write(BitConverter.GetBytes(e.BytesRecorded), 0, 4);
                            ms.Write(e.Buffer, 0, e.BytesRecorded);
                            stream.Write(ms.ToArray(), 0, (int)ms.Length);
                        }
                    };

                    waveIn.StartRecording();
                    Console.WriteLine("Capturing audio...");

                    await Task.Delay(-1); // Keep the voice client running until manually stopped

                    waveIn.StopRecording();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling voice client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        private static async Task ReceiveDataFromClientAsync(NetworkStream stream)
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        
                        if (receivedData.StartsWith("fps"))
                        {
                            string numberString = receivedData.Substring(3);

                            if (int.TryParse(numberString, out int number))
                            {
                                fps = number;
                            }
                        }

                        if (receivedData.StartsWith("compression"))
                        {
                            string numberString = receivedData.Substring(11);

                            if (int.TryParse(numberString, out int number))
                            {
                                compression = number;
                            }
                        }


                        switch (receivedData)
                        {
                            case "pauseCam":
                                videoSource.Stop();
                                break;
                            case "resumeCam":
                                videoSource.Start();
                                break;

                            case "info":
                                byte[] responseBytesInfo = Encoding.UTF8.GetBytes("info" + info);
                                await stream.WriteAsync(responseBytesInfo, 0, responseBytesInfo.Length);
                                break;
                                

                            case "pauseScreen":
                                isScreenSharingPaused = true;
                                break;
                                

                            case "resumeScreen":
                                isScreenSharingPaused = false;
                                break;

                            default: break;
                        }

                        Console.WriteLine("Received data from client: " + receivedData);
                    }
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                Console.WriteLine("Client disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving data from client: {ex.Message}");
            }
        }

        private static async Task SendScreenSharingFramesAsync(NetworkStream stream)
        {
            try
            {
                while (true)
                {
                    if (!isScreenSharingPaused)
                    {
                        Bitmap screenshot = CaptureScreen();

                        byte[] imageData;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            if(compression == 100)
                            {
                                screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                            }
                            else
                            {
                                var qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, compression); // Adjust quality (1-100)
                                var jpegCodec = GetEncoder(ImageFormat.Jpeg);
                                var encoderParams = new EncoderParameters(1);
                                encoderParams.Param[0] = qualityParam;

                                screenshot.Save(ms, jpegCodec, encoderParams);
                            }

                            imageData = ms.ToArray();
                        }
                        SendImageSize(stream, imageData.Length);
                        SendImageData(stream, imageData);
                    }

                    await Task.Delay(1000 / fps);
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                // Client disconnected
                Console.WriteLine("Client disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending screen sharing frames: {ex.Message}");
            }
        }

        private static void SendImageSize(NetworkStream stream, int imageSize)
        {
            byte[] sizeBytes = BitConverter.GetBytes(imageSize);
            stream.Write(sizeBytes, 0, sizeBytes.Length);
        }

        private static void SendImageData(NetworkStream stream, byte[] imageData)
        {
            try
            {
                stream.Write(imageData, 0, imageData.Length);
            }
            catch (IOException e) when (e.InnerException is SocketException)
            {
                Console.WriteLine("Client disconnected.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error sending image data: " + e.Message);
            }

        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
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
    }
}
