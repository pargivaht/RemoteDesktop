
using Client2.ViewModel;
using Client2.Views.Pages;
using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;

namespace Client2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private object? _currentPage;

        public MainWindow()
        {

            InitializeComponent();

            StateChanged += MainWindow_StateChanged;
            SystemThemeWatcher.Watch(this);
            Loaded += (_, _) => RootNavigation.Navigate(typeof(MainPage));
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {

            ConnectionPage connectionPage = new ConnectionPage();
            connectionPage.AdjustContentSize();
        }

    }
}
