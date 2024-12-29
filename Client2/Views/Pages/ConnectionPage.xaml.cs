using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client2.Views.Pages
{
    public partial class ConnectionPage : Page
    {
        private readonly Connection _connection;



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

            _connection = new Connection(ip, port, password);

            //do something initially. eg. connect
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


    }


    public static class NavigationParameters
    {
        public static string Ip { get; set; }
        public static int Port { get; set; }
        public static string Password { get; set; }
    }


}
