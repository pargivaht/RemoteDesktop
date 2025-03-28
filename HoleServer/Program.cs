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
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class SecureServer
{
    static string rsaPublicKey;
    static string rsaPrivateKey;
    static byte[] aesKey;
    static byte[] aesIV = new byte[16]; // IV should be random in real cases

    static async Task Main()
    {
        // Generate RSA key pair
        (rsaPublicKey, rsaPrivateKey) = RSAKeyPair.GenerateKeys();
        TcpListener listener = new TcpListener(IPAddress.Any, 8888);
        listener.Start();
        Console.WriteLine("Server started.");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    static async Task HandleClient(TcpClient client)
    {
        // Using curly braces for C# 7.3 compatibility
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        using (var writer = new StreamWriter(stream) { AutoFlush = true })
        {
            // Send RSA public key
            await writer.WriteLineAsync(rsaPublicKey);

            // Receive encrypted AES key from client
            byte[] encryptedKey = Convert.FromBase64String(await reader.ReadLineAsync());
            aesKey = Convert.FromBase64String(RSAEncryption.DecryptWithRSA(encryptedKey, rsaPrivateKey));

            Console.WriteLine("Secure AES key received.");

            // Receive encrypted message
            string encryptedMessage = await reader.ReadLineAsync();
            byte[] cipherText = Convert.FromBase64String(encryptedMessage);
            string decryptedMessage = AESEncryption.Decrypt(cipherText, aesKey, aesIV);

            Console.WriteLine($"Decrypted Message: {decryptedMessage}");
        }
    }
}

class RSAKeyPair
{
    public static (string publicKey, string privateKey) GenerateKeys()
    {
        using (var rsa = RSA.Create(2048)) // Generate 2048-bit RSA key pair
        {
            return (Convert.ToBase64String(rsa.ExportRSAPublicKey()), Convert.ToBase64String(rsa.ExportRSAPrivateKey()));
        }
    }
}

class RSAEncryption
{
    public static byte[] EncryptWithRSA(string plainText, string publicKey)
    {
        using (var rsa = RSA.Create())
        {
            // Import the public key using the ImportCspBlob method (CSP style)
            rsa.ImportCspBlob(Convert.FromBase64String(publicKey));
            return rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA256);
        }
    }

    public static string DecryptWithRSA(byte[] cipherText, string privateKey)
    {
        using (var rsa = RSA.Create())
        {
            // Import the private key using the ImportCspBlob method (CSP style)
            rs(Convert.FromBase64String(privateKey));
            return Encoding.UTF8.GetString(rsa.Decrypt(cipherText, RSAEncryptionPadding.OaepSHA256));
        }
    }
}


class AESEncryption
{
    public static byte[] Encrypt(string plainText, byte[] key, byte[] iv)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cs))
            {
                writer.Write(plainText);
                writer.Flush();
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }

    public static string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(cipherText))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var reader = new StreamReader(cs))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
