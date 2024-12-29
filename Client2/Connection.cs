using Client2.Views.Pages;
using System.Windows;

public class Connection
{
    public string Ip { get; }
    public int Port { get; }
    public string Password { get; }

    public Connection(string ip, int port, string password)
    {
        Ip = ip;
        Port = port;
        Password = password;

        //MessageBox.Show(password, ip);
    }

    public void Connect()
    {
        // Handle the connection logic here
    }
}
