using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Media;
using Client2.ViewModel;
using System.Windows;
using System.Windows.Input;



namespace Client2.Views.UserControls
{

    public partial class Card : UserControl
    {
        // Properties for Name, IP, Port, and StatusColor

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(Card), new PropertyMetadata(string.Empty));

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public static readonly DependencyProperty IpProperty =
            DependencyProperty.Register("Ip", typeof(string), typeof(Card), new PropertyMetadata(string.Empty));

        public string Ip
        {
            get { return (string)GetValue(IpProperty); }
            set { SetValue(IpProperty, value); }
        }

        public static readonly DependencyProperty PortProperty =
            DependencyProperty.Register("Port", typeof(string), typeof(Card), new PropertyMetadata(string.Empty));

        public string Port
        {
            get { return (string)GetValue(PortProperty); }
            set { SetValue(PortProperty, value); }
        }

        public static readonly DependencyProperty StatusColorProperty =
            DependencyProperty.Register("StatusColor", typeof(Brush), typeof(Card), new PropertyMetadata(Brushes.Red));

        public Brush StatusColor
        {
            get { return (Brush)GetValue(StatusColorProperty); }
            set { SetValue(StatusColorProperty, value); }
        }

        public Card()
        {
            InitializeComponent();
            this.DataContext = this;  // Set the DataContext to this card instance
        }

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
