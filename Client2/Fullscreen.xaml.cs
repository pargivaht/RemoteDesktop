using Client2.Views.Pages;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Client2
{
    /// <summary>
    /// Interaction logic for Fullscreen.xaml
    /// </summary>
    public partial class Fullscreen : FluentWindow
    {
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private bool isMenuVisible = false;
        private bool isAnimating = false;

        public Fullscreen()
        {
            InitializeComponent();

            this.WindowStyle = WindowStyle.None;      // Removes the title bar
            this.ResizeMode = ResizeMode.NoResize;    // Prevents resizing
            this.WindowState = WindowState.Maximized; // Maximizes the window
            this.Topmost = true;

            Connection.ScreenUpdated += OnScreenUpdated;

            ConnectionPage.isFullscreen = true;
            this.Closing += (sender, e) =>
            {
                ConnectionPage.isFullscreen = false;
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnScreenUpdated(System.Drawing.Image image)
        {
            Dispatcher.Invoke(() =>
            {
                pictureBoxScreen.Source = ConvertToBitmapImage(image);
            });
        }

        private void Fullscreen_Loaded(object sender, RoutedEventArgs e)
        {
            double left = (this.ActualWidth - FloatingMenu.ActualWidth) / 2;
            Canvas.SetLeft(FloatingMenu, left);
            Canvas.SetTop(FloatingMenu, 15);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isAnimating) return;

            var position = e.GetPosition(this);

            if (position.Y < 100 && !isMenuVisible)
                SlideMenuDown();
            else if (position.Y >= 150 && isMenuVisible)
                SlideMenuUp();
        }

        private void SlideMenuDown()
        {
            isAnimating = true;
            var anim = new DoubleAnimation
            {
                From = -60,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            anim.Completed += (s, e) =>
            {
                isAnimating = false;
                isMenuVisible = true;
            };

            (FloatingMenuBorder.RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.YProperty, anim);
        }

        private void SlideMenuUp()
        {
            isAnimating = true;
            var anim = new DoubleAnimation
            {
                From = 0,
                To = -60,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            anim.Completed += (s, e) =>
            {
                isAnimating = false;
                isMenuVisible = false;
            };

            (FloatingMenuBorder.RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.YProperty, anim);
        }
    


        private static BitmapSource ConvertToBitmapImage(System.Drawing.Image image)
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
    }
}
