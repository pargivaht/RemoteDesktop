using Client2.ViewModel;
using System.Windows.Input;

namespace Client2.Views.Pages
{
    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
            AddBtn.Click += AddBtn_Click;
        }

        private void AddBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MainWindow.listener.AddNew(Name.Text, Ip.Text, Port.Text, Password.Password);
        }
    }
}
