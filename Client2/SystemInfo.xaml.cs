using Newtonsoft.Json;
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
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using TreeViewItem = System.Windows.Controls.TreeViewItem;

namespace Client2
{
    /// <summary>
    /// Interaction logic for SystemInfo.xaml
    /// </summary>
    public partial class SystemInfo : FluentWindow
    {
        SystemInfoList list;
        public SystemInfo(SystemInfoList list)
        {
            this.list = list;
            InitializeComponent();
        }

        private void FluentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var categoryMap = new Dictionary<string, TreeViewItem>
            {
                { "General Info", (TreeViewItem)BasicInfo },
                { "CPU", (TreeViewItem)CPU },
                { "RAM", (TreeViewItem)RAM },
                { "GPU", (TreeViewItem)GPU },
                { "Storage", (TreeViewItem)storage },
                { "Motherboard", (TreeViewItem)motherboard },
                { "Network", (TreeViewItem)network },
                { "Battery", (TreeViewItem)battery },
                { "Display", (TreeViewItem)display },
                { "Security", (TreeViewItem)security },
                { "Virtualization", (TreeViewItem)virtualization }
            };

            // Loop through system info properties
            foreach (var entry in list.GetType().GetProperties())
            {
                string key = entry.Name;

                string category = GetCategoryForProperty(key);

                if (categoryMap.TryGetValue(category, out TreeViewItem categoryItem))
                {
                    categoryItem.Items.Add(new TreeViewItem() { Header = $"{key}: {entry.GetValue(list)}" });
                }
                else
                {
                    // Default to General Info if no category is found
                    BasicInfo.Items.Add(new TreeViewItem() { Header = $"{key}: {entry.GetValue(list)}" });
                }
            }
        }

        private string GetCategoryForProperty(string propertyName)
        {
            if (propertyName is "Title" or "OS" or "UserName" or "MachineName" or "BootTime" or "Uptime" or "SerialNumber")
                return "General Info";
            if (propertyName.StartsWith("CPU", StringComparison.OrdinalIgnoreCase) || propertyName == "Virtualization")
                return "CPU";
            if (propertyName.StartsWith("RAM", StringComparison.OrdinalIgnoreCase))
                return "RAM";
            if (propertyName.StartsWith("GPU", StringComparison.OrdinalIgnoreCase))
                return "GPU";
            if (propertyName.Contains("Drive") || propertyName.Contains("Storage"))
                return "Storage";
            if (propertyName.StartsWith("Motherboard", StringComparison.OrdinalIgnoreCase) || propertyName.Contains("Bios"))
                return "Motherboard";
            if (propertyName.Contains("Network") || propertyName.Contains("IP") || propertyName.Contains("MAC"))
                return "Network";
            if (propertyName.StartsWith("Battery", StringComparison.OrdinalIgnoreCase))
                return "Battery";
            if (propertyName.Contains("Display"))
                return "Display";
            if (propertyName.Contains("Antivirus"))
                return "Security";
            if (propertyName.Contains("IsVirtualizationEnabled"))
                return "Virtualization";

            return "General Info"; // Default
        }
    }
}
