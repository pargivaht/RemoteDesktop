
using Client2.ViewModel;
using Client2.Views.Pages;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
namespace Client2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        public MainWindow()
        {

            InitializeComponent();

            StateChanged += MainWindow_StateChanged;
            Loaded += Window_Loaded;
            SystemThemeWatcher.Watch(this);
            Loaded += (_, _) => 
            {
                RootNavigation.Navigate(typeof(MainPage));
                RootNavigation.IsPaneOpen = false;
            };

        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            //ConnectionPage connectionPage = new ConnectionPage();
            //connectionPage.AdjustContentSize();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public void CreateNewConnection(string ip, int port, string password)
        {
            // Set the values in the static class before navigating
            NavigationParameters.Ip = ip;
            NavigationParameters.Port = port;
            NavigationParameters.Password = password;
            NavigationParameters.Window = this;

            // Navigate to the ConnectionPage
            RootNavigation.Navigate(typeof(ConnectionPage));
        }

        public void SetTitle(string title)
        {
            Title = title;
            Titlebar.Title = title;
        }

        public async Task<bool> DialogDelete()
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            ContentDialogResult a = await contentDialogService.ShowAsync(
                new ContentDialog()
                {
                    Title = "Delete Item?",
                    Content = "Are you sure that you want to delete this?",
                    PrimaryButtonText = "Delete",
                    IsSecondaryButtonEnabled = false,
                    PrimaryButtonAppearance = ControlAppearance.Danger,
                    CloseButtonText = "Cancel"
                }, CancellationToken.None);

            
            if(a.Equals(ContentDialogResult.Primary))
            {
                return true;
            }
            else
                { return false; }

        }

        public async Task DialogWait(CancellationToken c)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            ContentDialogResult a = await contentDialogService.ShowAsync(
                new ContentDialog()
                {
                    Title = "Connecting...",
                    Content = "Please wait a moment.",
                    IsPrimaryButtonEnabled = false,
                    IsSecondaryButtonEnabled = false,
                    PrimaryButtonAppearance = ControlAppearance.Secondary,
                    CloseButtonText = "Cancel"
                }, c);

        }

        public async Task DialogFillAll()
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            ContentDialogResult a = await contentDialogService.ShowAsync(
                new ContentDialog()
                {
                    Title = "Missing Info",
                    Content = "Please fill all required fields.",
                    IsPrimaryButtonEnabled = false,
                    IsSecondaryButtonEnabled = false,
                    PrimaryButtonAppearance = ControlAppearance.Secondary,
                    CloseButtonText = "Ok"
                }, CancellationToken.None);
            
        }

    }
}
