
using Client2.ViewModel;
using Client2.Views.Pages;
using System;
using System.Collections.Generic;
using System.IO;
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


        public static Listener listener = new();

        public MainWindow()
        {

            InitializeComponent();

            StateChanged += MainWindow_StateChanged;
            Loaded += Window_Loaded;
            SystemThemeWatcher.Watch(this);
            Loaded += (_, _) => RootNavigation.Navigate(typeof(MainPage));

            

        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {

            ConnectionPage connectionPage = new ConnectionPage();
            connectionPage.AdjustContentSize();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            listener.Start();
        }

    }
}
