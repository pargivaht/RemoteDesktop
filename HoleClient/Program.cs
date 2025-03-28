//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;

//class StunClient
//{
//    private const int STUN_PORT = 3478; // Default STUN port
//    private static readonly IPEndPoint stunServerEndPoint = new IPEndPoint(IPAddress.Parse("stun.l.google.com"), STUN_PORT); // Public STUN server

//    public static void Main(string[] args)
//    {
//        using (UdpClient udpClient = new UdpClient())
//        {
//            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0)); // Bind to any available port

//            // Create a STUN binding request
//            byte[] bindingRequest = BuildBindingRequest();

//            // Send the request to the STUN server
//            udpClient.Send(bindingRequest, bindingRequest.Length, stunServerEndPoint);
//            Console.WriteLine("Sent STUN Binding Request to STUN server.");

//            // Receive the response
//            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
//            byte[] responseBytes = udpClient.Receive(ref remoteEndPoint);
//            Console.WriteLine($"Received STUN response from {remoteEndPoint}.");

//            // Parse the response
//            ParseStunResponse(responseBytes);
//        }
//    }

//    private static byte[] BuildBindingRequest()
//    {
//        // Create a basic STUN binding request (format specified in RFC 5389)
//        byte[] request = new byte[20];
//        request[0] = 0x00; // Message Type: Binding Request
//        request[1] = 0x01; // Message Type: Binding Request
//        request[2] = 0x00; // Message Length: 0
//        request[3] = 0x00; // Message Length: 0
//        // Transaction ID (random 12 bytes)
//        var random = new Random();
//        byte[] transactionId = new byte[12];
//        random.NextBytes(transactionId);
//        Buffer.BlockCopy(transactionId, 0, request, 4, transactionId.Length);
//        return request;
//    }

//    private static void ParseStunResponse(byte[] responseBytes)
//    {
//        // Check the message type
//        if (responseBytes[0] == 0x01 && responseBytes[1] == 0x01)
//        {
//            // The response is a Binding Response
//            // The mapped address is found in the response starting at byte 28 (RFC 5389)
//            if (responseBytes.Length >= 32) // Ensure enough data for the address
//            {
//                byte[] ipBytes = new byte[4];
//                Buffer.BlockCopy(responseBytes, 28, ipBytes, 0, 4);
//                string publicIp = string.Join(".", ipBytes);
//                ushort port = (ushort)((responseBytes[32] << 8) + responseBytes[33]); // Port is 2 bytes

//                Console.WriteLine($"Public IP: {publicIp}");
//                Console.WriteLine($"Public Port: {port}");
//            }
//            else
//            {
//                Console.WriteLine("Invalid STUN response length.");
//            }
//        }
//        else
//        {
//            Console.WriteLine("Received non-Binding Response.");
//        }
//    }
//}




using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class SecureClient
{
    static async Task Main()
    {
        // Connect to the server
        using (TcpClient client = new TcpClient("127.0.0.1", 8888))
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        using (var writer = new StreamWriter(stream) { AutoFlush = true })
        {
            // Request the RSA public key from the server
            string rsaPublicKey = await reader.ReadLineAsync();
            Console.WriteLine("Received RSA Public Key.");

            // Generate AES key
            byte[] aesKey = new byte[32]; // 256-bit AES key
            new Random().NextBytes(aesKey);

            // Encrypt AES key with server's RSA public key
            byte[] encryptedAesKey = RSAEncryption.EncryptWithRSA(Convert.ToBase64String(aesKey), rsaPublicKey);

            // Send the encrypted AES key to the server
            await writer.WriteLineAsync(Convert.ToBase64String(encryptedAesKey));
            Console.WriteLine("Sent AES key securely.");

            // Encrypt a message with AES
            byte[] aesIV = new byte[16]; // IV should be random in real cases
            string message = "Hello, secure server!";
            byte[] encryptedMessage = AESEncryption.Encrypt(message, aesKey, aesIV);

            // Send the encrypted message to the server
            await writer.WriteLineAsync(Convert.ToBase64String(encryptedMessage));
            Console.WriteLine("Sent encrypted message.");
        }
    }
}

class RSAEncryption
{
    public static byte[] EncryptWithRSA(string plainText, string publicKey)
    {
        using (var rsa = RSA.Create())
        {
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
            return rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA256);
        }
    }

    public static string DecryptWithRSA(byte[] cipherText, string privateKey)
    {
        using (var rsa = RSA.Create())
        {
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
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

