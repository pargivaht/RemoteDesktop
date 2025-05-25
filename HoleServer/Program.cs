//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;

//class UdpServer
//{
//    private UdpClient udpServer;

//    public UdpServer(int port)
//    {
//        udpServer = new UdpClient(port);
//    }

//    public void Start()
//    {
//        Console.WriteLine("UDP Server started...");
//        while (true)
//        {
//            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
//            byte[] receivedData = udpServer.Receive(ref clientEndPoint);
//            string message = Encoding.UTF8.GetString(receivedData);
//            Console.WriteLine($"Received message: {message} from {clientEndPoint}");

//            // Send back the client's endpoint to itself and others
//            SendClientAddress(clientEndPoint);
//        }
//    }

//    private void SendClientAddress(IPEndPoint clientEndPoint)
//    {
//        string responseMessage = $"{clientEndPoint.Address}:{clientEndPoint.Port}";
//        byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);

//        // Send back to the client
//        udpServer.Send(responseBytes, responseBytes.Length, clientEndPoint);
//    }

//    static void Main(string[] args)
//    {
//        UdpServer server = new UdpServer(12345);
//        server.Start();
//    }
//}


using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class SecureServer
{

    static async Task Main()
    {
        Console.WriteLine("skibidi123");
        byte[] a = Encrypt("skibidi123", "password");
        foreach (byte b in a) { Console.Write(b); }
        string message = Decrypt(a, "password");
        Console.WriteLine(message);
    }

    public static byte[] Encrypt(string plainText, string password)
    {
        // Generate random salt
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(salt);

        // Derive key
        var key = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);

        using (Aes aes = Aes.Create())
        {
            aes.Key = key.GetBytes(32); // 256-bit key
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var ms = new MemoryStream())
            {
                // Write salt + IV to the beginning
                ms.Write(salt, 0, salt.Length);
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cryptoStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(plainText);
                }

                return ms.ToArray();
            }
        }
    }



    public static string Decrypt(byte[] encryptedData, string password)
    {
        byte[] salt = encryptedData.Take(16).ToArray();
        byte[] iv = encryptedData.Skip(16).Take(16).ToArray();
        byte[] ciphertext = encryptedData.Skip(32).ToArray();

        var key = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);

        using (Aes aes = Aes.Create())
        {
            aes.Key = key.GetBytes(32);
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var ms = new MemoryStream(ciphertext))
            using (var cryptoStream = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (var reader = new StreamReader(cryptoStream))
            {
                return reader.ReadToEnd();
            }
        }
    }


}
