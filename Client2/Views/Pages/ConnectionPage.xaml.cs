﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using Image = System.Drawing.Image;

namespace Client2.Views.Pages
{
    public partial class ConnectionPage : Page
    {
        public readonly Connection _connection;

        private const double HeaderHeight = 55;
        private const double FooterHeight = 40;

        public ConnectionPage()
        {
            InitializeComponent();
            Loaded += ConnectionPage_Loaded;
            SizeChanged += ConnectionPage_SizeChanged;
            

            string ip = NavigationParameters.Ip;
            int port = NavigationParameters.Port;
            string password = NavigationParameters.Password;

            MainWindow window = NavigationParameters.Window;

            _connection = new Connection(ip, port, password, this, window);

            _connection.ScreenUpdated += OnScreenUpdated;
            _connection.CameraUpdated += OnCameraUpdated;
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

        private BitmapImage ConvertToBitmapImage(Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
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

            NavigationService?.Navigate(new MainPage());
            _connection.Disconnect();
        }

        private void fps_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_connection != null)
            {
                _connection.ChangeFps((int)fps_slider.Value);
            }
        }

        private void compression_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_connection != null)
            {
                _connection.Compression((int)compression_slider.Value);
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

       
    }


    public static class NavigationParameters
    {
        public static string Ip { get; set; }
        public static int Port { get; set; }
        public static string Password { get; set; }
        public static MainWindow Window { get; set; }

    }
}
