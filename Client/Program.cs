using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Client
{
    private static TcpClient client;
    private static NetworkStream stream;

    public static void Main()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 8000);
            stream = client.GetStream();
            Console.WriteLine("Connected to the server.");

            Thread receiveThread = new Thread(ReceiveData);
            receiveThread.Start();

            while (true)
            {
                string message = Console.ReadLine();
                if (string.IsNullOrEmpty(message)) continue;

                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Could not connect to server: {ex.Message}");
        }
    }

    private static void ReceiveData()
    {
        try
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received: " + message);

                // Обновление позиции статического объекта на основе полученных данных
                // Например:
                // var данные = ParseMessage(message);
                // StaticEntity.UpdatePosition(данные.X, данные.Y);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving data: {ex.Message}");
        }
    }
}



