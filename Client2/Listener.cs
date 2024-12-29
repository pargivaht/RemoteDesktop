using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using Newtonsoft.Json;
using System.IO;
using Client2.Views.Pages;
using System.Windows.Media;
using Client2.Views.UserControls;
using System.Net.NetworkInformation;
using System.Threading;
using Client2.ViewModel;
using System.Net;

namespace Client2
{
    
    public class Listener
    {

        class Connection { public string Name; public string Ip; public string Port; public string Password; public string UUID; }

        static MainPage page;
        static MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        static List<CardViewModel> cardViewModels = new List<CardViewModel>(); // List to store the view models
        static List<Connection> connections = new List<Connection>(); // To store the connection data
        static Timer timer;

        public Listener(MainPage mainPage) 
        {
            page = mainPage;
        }

        public void Start()
        {
            ReadCardSaves();
            timer = new Timer(PingConnections, null, 0, 3000);
        }

        public void AddNewCard(string Name, string ip, string port, string password)
        {

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".RemoteDesktop");
            string file = Path.Combine(path, "saves.json");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            List<Connection> connections = File.Exists(file)
                ? JsonConvert.DeserializeObject<List<Connection>>(File.ReadAllText(file)) ?? new List<Connection>()
                : new List<Connection>();

            connections.Add(new Connection
            {
                Name = Name,
                Ip = ip,
                Port = port,
                Password = password,
                UUID = Guid.NewGuid().ToString()
            });

            File.WriteAllText(file, JsonConvert.SerializeObject(connections, Formatting.Indented));

            ClearCards();
            ReadCardSaves();
            timer = new Timer(PingConnections, null, 0, 3000);
        }

        private void ReadCardSaves()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".RemoteDesktop");
            string file = Path.Combine(path, "saves.json");

            if (File.Exists(file))
            {
                var connections = JsonConvert.DeserializeObject<List<Connection>>(File.ReadAllText(file));

                foreach (var conn in connections)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {

                        var cardViewModel = new CardViewModel
                        {
                            Name = conn.Name,
                            Ip = conn.Ip,
                            Port = conn.Port,
                            UUID = conn.UUID
                        };

                        cardViewModels.Add(cardViewModel);

                        var card = new Card
                        {
                            Name = conn.Name,
                            Ip = conn.Ip,
                            Port = conn.Port,
                            UUID = conn.UUID,
                            Margin = new Thickness(50, 0, 0, 0),
                            Password = conn.Password,
                            DataContext = cardViewModel
                        };

                        card.OnCardClicked += HandleCardClick;
                        card.OnDeleteRequested += HandleCardDelete;
                        //card.OnDeleteRequested += HandleCardEdit;

                        page.ConnectionPanel.Children.Add(card);
                    });
                }
            }
        }

        static void ClearCards()
        {
            Application.Current.Dispatcher.Invoke(page.ConnectionPanel.Children.Clear);
        }

        private async void HandleCardClick(string ip, string port, string password)
        {


            await mainWindow.DialogWait(CancellationToken.None);

            

            mainWindow.CreateNewConnection(ip, int.Parse(port), password);
        }

        private async void HandleCardDelete(string uuid)
        {

            if (!await mainWindow.DialogDelete())
            {
                return;
            }

            // Remove the card from the UI
            var cardToRemove = page.ConnectionPanel.Children
                .OfType<Card>()
                .FirstOrDefault(c => c.UUID == uuid);

            if (cardToRemove != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    page.ConnectionPanel.Children.Remove(cardToRemove);
                });
            }

            // Update the JSON file to remove the connection
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".RemoteDesktop");
            string file = Path.Combine(path, "saves.json");

            if (File.Exists(file))
            {
                var connections = JsonConvert.DeserializeObject<List<Connection>>(File.ReadAllText(file));
                connections = connections.Where(c => c.UUID != uuid).ToList();

                File.WriteAllText(file, JsonConvert.SerializeObject(connections, Formatting.Indented));
            }
        }

        static void PingConnections(object state)
        {
            foreach (var cardViewModel in cardViewModels)
            {
                Task.Run(() => PingConnection(cardViewModel));
            }
        }

        static void PingConnection(CardViewModel cardViewModel)
        {
            Ping pingSender = new Ping();
            try
            {
                PingReply reply = pingSender.Send(cardViewModel.Ip, 1000);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (reply.Status == IPStatus.Success)
                    {

                        cardViewModel.Background = new SolidColorBrush(Color.FromRgb(0, 200, 0));

                    }
                    else
                    {

                        cardViewModel.Background = new SolidColorBrush(Color.FromRgb(193, 60, 0));
                    }
                });
            }
            catch (Exception) // In case of exception (e.g., timeout)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    cardViewModel.Background = new SolidColorBrush(Color.FromRgb(193, 0, 0));
                });
            }
        }
    }

}


