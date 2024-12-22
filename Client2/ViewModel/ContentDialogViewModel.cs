using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Client2.ViewModel
{
    public class ContentDialogViewModel : INotifyPropertyChanged
    {
        private string _title;
        private string _content;
        private Visibility _visibility = Visibility.Hidden;
        private Action<string> _onButtonClicked;

        private string _primaryButtonText;
        private string _secondaryButtonText;
        private string _closeButtonText;

        private bool _isPrimaryButtonEnabled = true;
        private bool _isSecondaryButtonEnabled = true;
        private bool _isCloseButtonEnabled = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(nameof(Content)); }
        }

        public Visibility Visibility
        {
            get => _visibility;
            set { _visibility = value; OnPropertyChanged(nameof(Visibility)); }
        }

        public string PrimaryButtonText
        {
            get => _primaryButtonText;
            set { _primaryButtonText = value; OnPropertyChanged(nameof(PrimaryButtonText)); }
        }

        public string SecondaryButtonText
        {
            get => _secondaryButtonText;
            set { _secondaryButtonText = value; OnPropertyChanged(nameof(SecondaryButtonText)); }
        }

        public string CloseButtonText
        {
            get => _closeButtonText;
            set { _closeButtonText = value; OnPropertyChanged(nameof(CloseButtonText)); }
        }

        public bool IsPrimaryButtonEnabled
        {
            get => _isPrimaryButtonEnabled;
            set { _isPrimaryButtonEnabled = value; OnPropertyChanged(nameof(IsPrimaryButtonEnabled)); }
        }

        public bool IsSecondaryButtonEnabled
        {
            get => _isSecondaryButtonEnabled;
            set { _isSecondaryButtonEnabled = value; OnPropertyChanged(nameof(IsSecondaryButtonEnabled)); }
        }

        public bool IsCloseButtonEnabled
        {
            get => _isCloseButtonEnabled;
            set { _isCloseButtonEnabled = value; OnPropertyChanged(nameof(IsCloseButtonEnabled)); }
        }

        public ICommand ButtonCommand { get; }

        public ContentDialogViewModel()
        {
            ButtonCommand = new RelayCommand<string>(button =>
            {
                _onButtonClicked?.Invoke(button);
                Visibility = Visibility.Hidden;
            });
        }

        public void ShowDialog(string title, string content, string primaryButtonText, string secondaryButtonText, string closeButtonText, Action<string> onButtonClicked)
        {
            Title = title;
            Content = content;
            PrimaryButtonText = primaryButtonText;
            SecondaryButtonText = secondaryButtonText;
            CloseButtonText = closeButtonText;
            _onButtonClicked = onButtonClicked;
            Visibility = Visibility.Visible;
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
