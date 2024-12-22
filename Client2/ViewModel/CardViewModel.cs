using System.ComponentModel;
using System.Windows.Media;

namespace Client2.ViewModel
{
    public class CardViewModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }

        public string UUID { get; set; }

        private Brush _background = new SolidColorBrush(Color.FromRgb(48, 48, 48));
        public Brush Background
        {
            get { return _background; }
            set
            {
                if (_background != value)
                {
                    _background = value;
                    OnPropertyChanged(nameof(Background));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
