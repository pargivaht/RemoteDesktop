
using Client2.ViewModel;
using Client2.Views.Pages;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Policy;
using System.Speech.Synthesis;
using System.Text;
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

            Console.SetWindowSize(110, 25);

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
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


        public async Task DialogError(string title, string error, CancellationToken c)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);


            ContentDialogResult a = await contentDialogService.ShowAsync(
            new ContentDialog()
            {
                Title = title,
                Content = error,
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false,
                PrimaryButtonAppearance = ControlAppearance.Secondary,
                CloseButtonText = "Ok"
            }, c);
        }


        public async Task<string> DialogCreateMessageBox(CancellationToken c)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            // Create UI elements
            Wpf.Ui.Controls.TextBox messageBox = new Wpf.Ui.Controls.TextBox() { PlaceholderText = "Enter message (required)" };
            Wpf.Ui.Controls.TextBox titleBox = new Wpf.Ui.Controls.TextBox() { PlaceholderText = "Enter title (required)" };

            ComboBox buttonsDropdown = new ComboBox()
            {
                ItemsSource = new List<string> { "OK", "OKCancel", "YesNo", "YesNoCancel" },
                SelectedIndex = 0 // Default selection
            };

            ComboBox iconDropdown = new ComboBox()
            {
                ItemsSource = new List<string> { "None", "Info", "Warning", "Error", "Question" },
                SelectedIndex = 0
            };

            StackPanel panel = new StackPanel();
            panel.Children.Add(new Wpf.Ui.Controls.TextBlock() { Text = "Message:" });
            panel.Children.Add(messageBox);
            panel.Children.Add(new Wpf.Ui.Controls.TextBlock() { Text = "Title:" });
            panel.Children.Add(titleBox);
            panel.Children.Add(new Wpf.Ui.Controls.TextBlock() { Text = "Buttons:" });
            panel.Children.Add(buttonsDropdown);
            panel.Children.Add(new Wpf.Ui.Controls.TextBlock() { Text = "Icon:" });
            panel.Children.Add(iconDropdown);
            

            var dialog = new ContentDialog()
            {
                Title = "Send Message Box",
                Content = panel,
                PrimaryButtonText = "Send",
                CloseButtonText = "Cancel"
            };

            // Show the dialog
            var result = await contentDialogService.ShowAsync(dialog, c);


            // Return formatted string if user clicked "Send"
            if (result == ContentDialogResult.Primary)
            {
                return $"{messageBox.Text}|{titleBox.Text}|{buttonsDropdown.SelectedItem}|{iconDropdown.SelectedItem}";
            }

            return null; // User canceled the dialog
        }

        public async Task<string> DialogCreateTTSMsg(CancellationToken c)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            // Create UI elements
            Wpf.Ui.Controls.TextBox messageBox = new Wpf.Ui.Controls.TextBox() { PlaceholderText = "Enter message (required)" };

            ComboBox speakerDropdown = new ComboBox()
            {
                ItemsSource = new List<string> { "albert", "indrek", "kalev", "kylli", "kylli", "lee", "liivika", "luukas", "mari", "meelis", "peeter", "tambet", "vesta" },
                SelectedIndex = 0 // Default selection
            };

            ComboBox langDropdown = new ComboBox()
            {
                ItemsSource = new List<string> { "est", "eng" },
                SelectedIndex = 0
            };

            Slider speedSlider = new Slider()
            {
                Minimum = 0.5,
                Maximum = 2,
                Value = 1,
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true
            };

            StackPanel panel = new StackPanel();

            panel.Children.Add(new Wpf.Ui.Controls.TextBlock() { Text = "Speaker:" });
            panel.Children.Add(speakerDropdown);
            panel.Children.Add(new Wpf.Ui.Controls.TextBlock() { Text = "Language:" });
            panel.Children.Add(langDropdown);
            panel.Children.Add(new Wpf.Ui.Controls.TextBlock() { Text = "Speed:" });
            panel.Children.Add(speedSlider);
            panel.Children.Add(new Wpf.Ui.Controls.TextBlock() { Text = "Message:" });
            panel.Children.Add(messageBox);


            var dialog = new ContentDialog()
            {
                Title = "Send TTS Message",
                Content = panel,
                PrimaryButtonText = "Send",
                SecondaryButtonText = "Preview",
                CloseButtonText = "Cancel"
            };

            // Show the dialog
            var result = await contentDialogService.ShowAsync(dialog, c);

            // Return formatted string if user clicked "Send"
            if (result == ContentDialogResult.Primary)
            {
                if (langDropdown.SelectedItem == "est")
                {
                    return $"tts{messageBox.Text}|{speakerDropdown.Text}|{speedSlider.Value}";
                }
                else
                {
                    return $"engtts{messageBox.Text}|{speedSlider.Value}";
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                if (langDropdown.SelectedIndex == 0)
                {
                    await PlayTTS(messageBox.Text, speakerDropdown.Text, (int)speedSlider.Value);
                }
                else
                {
                    PlayTTSeng(messageBox.Text, (int)speedSlider.Value);
                }
            }

            return null; // User canceled the dialog
            
        }


        public async Task<string> DialogCreateUrl(CancellationToken c)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            // Create UI elements
            Wpf.Ui.Controls.TextBox url = new Wpf.Ui.Controls.TextBox() { PlaceholderText = "Enter url (required)" };


            StackPanel panel = new StackPanel();
            panel.Children.Add(new Wpf.Ui.Controls.TextBlock() { Text = "Url:" });
            panel.Children.Add(url);

            var dialog = new ContentDialog()
            {
                Title = "Send Open Website",
                Content = panel,
                PrimaryButtonText = "Send",
                CloseButtonText = "Cancel"
            };

            // Show the dialog
            var result = await contentDialogService.ShowAsync(dialog, c);

            if (result == ContentDialogResult.Primary)
            {
                return $"{url.Text}";
            }

            return null; // User canceled the dialog
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

        public async Task<bool> DialogBSOD(CancellationToken c)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            var dialog = new ContentDialog()
            {
                Title = "Are you sure?",
                Content = "If you continue, the remote machine will crash.\n Do you want to continue?",
                PrimaryButtonAppearance = ControlAppearance.Danger,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await contentDialogService.ShowAsync(dialog, c);

            if (result == ContentDialogResult.Primary)
            {
                return true;
            }

            return false;

        }

        public async Task<bool> DialogShutdown(CancellationToken c)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            var dialog = new ContentDialog()
            {
                Title = "Are you sure?",
                Content = "If you continue, the remote machine will shutdown.\n Do you want to continue?",
                PrimaryButtonAppearance = ControlAppearance.Danger,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await contentDialogService.ShowAsync(dialog, c);

            if (result == ContentDialogResult.Primary)
            {
                return true;
            }

            return false;

        }

        public async Task<bool> DialogRestart(CancellationToken c)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            var dialog = new ContentDialog()
            {
                Title = "Are you sure?",
                Content = "If you continue, the remote machine will restart.\n Do you want to continue?",
                PrimaryButtonAppearance = ControlAppearance.Danger,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await contentDialogService.ShowAsync(dialog, c);

            if (result == ContentDialogResult.Primary)
            {
                return true;
            }

            return false;

        }

        public static async Task PlayTTS(string text, string speaker, float speed = 1)
        {
            if (speed > 2)
            {
                speed = 2;
            }
            else if (speed < 0.5)
            {
                speed = 0.5f;
            }
            HttpClient client = new HttpClient();

            var payload = new
            {
                text,
                speaker = speaker.ToString().ToLowerInvariant(),
                speed
            };

            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                // Send POST request to TTS API
                HttpResponseMessage response = await client.PostAsync("https://api.tartunlp.ai/text-to-speech/v2/", content);
                response.EnsureSuccessStatusCode();

                // Read the response stream
                using (Stream audioStream = await response.Content.ReadAsStreamAsync())
                {
                    // Play the audio stream using NAudio
                    using (var waveOut = new WaveOutEvent())
                    using (var waveReader = new WaveFileReader(audioStream))
                    {
                        waveOut.Init(waveReader);
                        waveOut.Play();

                        // Wait for playback to finish
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            await Task.Delay(500);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public static void PlayTTSeng(string text, int speed = 1)
        {
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                synth.Rate = speed;
                synth.SpeakAsync(text);
            }
        }
    }
}