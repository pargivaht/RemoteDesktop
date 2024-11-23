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
    /// <summary>
    /// Interaction logic for ConnectionPage.xaml
    /// </summary>
    public partial class ConnectionPage : Page
    {
        private const double HeaderHeight = 55;
        private const double FooterHeight = 40;

        public ConnectionPage()
        {
            InitializeComponent();

            // Ensure size calculations on load and when the page resizes
            Loaded += ConnectionPage_Loaded;
            SizeChanged += ConnectionPage_SizeChanged;
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
            // Calculate the available height for the middle row
            double availableHeight = ActualHeight - HeaderHeight - FooterHeight;

            if (availableHeight > 0)
            {
                // Dynamically set the middle row's height
                MainGrid.RowDefinitions[1].Height = new GridLength(availableHeight, GridUnitType.Pixel);
            }
        }
    }


}
