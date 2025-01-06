using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Client
{
    private TcpClient client;
    private NetworkStream stream;

    public async Task StartClientAsync(string hostIp)
    {
        try
        {
            client = new TcpClient(hostIp, 8000);
            stream = client.GetStream();
            Console.WriteLine("Connected to the host.");

            _ = Task.Run(ReceiveDataAsync);

            while (true)
            {
                string message = Console.ReadLine();
                if (string.IsNullOrEmpty(message)) continue;

                byte[] data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Could not connect to host: {ex.Message}");
        }
    }

    private async Task ReceiveDataAsync()
    {
        try
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received: " + message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving data: {ex.Message}");
        }
    }
}
