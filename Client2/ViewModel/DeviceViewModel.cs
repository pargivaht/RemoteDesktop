using System.ComponentModel;
using System.Windows.Media;

namespace Client2.ViewModel
{
    public class DeviceViewModel : INotifyPropertyChanged
    {
        private Brush _statusColor = new SolidColorBrush(Color.FromRgb(48,48,48)); // Default to Offline color

        public Brush StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public void UpdateStatus(bool isOnline)
        {
            Color Online = Color.FromRgb(0, 200, 0);
            Color Offline = Color.FromRgb(193, 0, 0);

            StatusColor = isOnline ? new SolidColorBrush(Online) : new SolidColorBrush(Offline);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
