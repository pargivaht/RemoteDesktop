using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

class ScreenReceiver
{
    private const int Port = 8888;
    private const string ServerIp = "127.0.0.1";
    private UdpClient udpClient;
    private Form form;
    private PictureBox pictureBox;
    private IPEndPoint serverEndPoint;

    public void Start()
    {
        // Initialize the form and picture box
        form = new Form
        {
            Text = "Screen Receiver",
            ClientSize = new Size(800, 450),
            FormBorderStyle = FormBorderStyle.Sizable,
            MaximizeBox = false
        };

        pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.StretchImage
        };

        form.Controls.Add(pictureBox);
        form.Show();

        serverEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), Port);

        // Start the UDP receiving in a background task
        Task.Run(() => ReceiveScreenshots());
    }

    private void ReceiveScreenshots()
    {
        udpClient = new UdpClient(); // Initialize without binding to a specific port
        SendConnectionRequest();     // Send a connection request to the server

        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0); // EndPoint to receive from any server

        while (true)
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
                        DisplayScreenshot(receivedData.ToArray());
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

 
    private void SendConnectionRequest()
    {
        byte[] connectionRequest = BitConverter.GetBytes(1); // Send a simple integer as a connection request
        udpClient.Send(connectionRequest, connectionRequest.Length, serverEndPoint);
    }

    private void DisplayScreenshot(byte[] data)
    {
        if (data.Length == 0) return;

        try
        {
            using (var ms = new MemoryStream(data))
            {
                Bitmap image = new Bitmap(ms);
                form.Invoke((MethodInvoker)(() =>
                {
                    if (pictureBox.Image != null)
                    {
                        pictureBox.Image.Dispose(); // Dispose of the old image to avoid memory leaks
                    }
                    pictureBox.Image = image; // Set the new image
                }));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error processing image: " + ex.Message);
        }
    }

    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ScreenReceiver receiver = new ScreenReceiver();
        receiver.Start();
        Application.Run(receiver.form);
    }
}
