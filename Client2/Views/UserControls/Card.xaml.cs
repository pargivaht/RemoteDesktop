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
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(Card), new PropertyMetadata("Device"));

        public static readonly DependencyProperty IPProperty =
            DependencyProperty.Register("IP", typeof(string), typeof(Card), new PropertyMetadata("0.0.0.0"));

        public static readonly DependencyProperty StatusColorProperty =
            DependencyProperty.Register("StatusColor", typeof(Color), typeof(Card), new PropertyMetadata(Color.FromRgb(48,48,48)));

        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register("StatusText", typeof(string), typeof(Card), new PropertyMetadata("Unknown"));

        public string Name_
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public string IP
        {
            get => (string)GetValue(IPProperty);
            set => SetValue(IPProperty, value);
        }

        public Color StatusColor
        {
            get => (Color)GetValue(StatusColorProperty);
            set => SetValue(StatusColorProperty, value);
        }

        public string StatusText
        {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        public Card()
        {
            InitializeComponent();
        }

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (StatusText == "Online")
            {
                StatusColor = Color.FromRgb(90, 0, 0); // Red
                StatusText = "Offline";
            }
            else
            {
                StatusColor = Color.FromRgb(0, 100, 0); // Green
                StatusText = "Online";
            }
        }
    }
}
