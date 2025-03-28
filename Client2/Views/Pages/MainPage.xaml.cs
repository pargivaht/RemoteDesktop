using Client2.ViewModel;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Client2.Views.Pages
{
    public partial class MainPage
    {
        public static Listener listener;

        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        public MainPage()
        {
            InitializeComponent();
            AddBtn.Click += AddBtn_Click;
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Port.Text = "8888";

            listener = new(this);

            new Thread(listener.Start).Start();
        }

        private async void AddBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {


            if (string.IsNullOrEmpty(Ip.Text) || string.IsNullOrEmpty(Port.Text))
            {
                await mainWindow.DialogFillAll();
            }
            else
            {
                listener.AddNewCard(Name.Text, Ip.Text, Port.Text, Password.Password);

                Name.Clear();
                Ip.Clear();
                Password.Clear();
            }
        }
    }
}
