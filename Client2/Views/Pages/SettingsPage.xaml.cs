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
using Wpf.Ui.Appearance;

namespace Client2.Views.Pages
{
    public partial class SettingsPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            AppVersionTextBlock.Text = $"Remote Desktop - {GetAssemblyVersion()}";

            ApplicationThemeManager.Apply(ApplicationTheme.Dark);

        }


        private static string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? string.Empty;
        }
    }
}
