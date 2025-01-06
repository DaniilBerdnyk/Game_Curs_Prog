﻿using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Client
{
    private static TcpClient client;
    private static NetworkStream stream;

    public static async Task Main()
    {
        var serverIp = await NetworkDiscovery.DiscoverServerAsync();

        if (serverIp == null)
        {
            Console.WriteLine("No server found. Please start the server first.");
            return;
        }

        Console.WriteLine($"Server found at {serverIp}. Starting as client...");
        await StartClientAsync(serverIp.ToString());
    }

    private static async Task StartClientAsync(string serverIp)
    {
        try
        {
            client = new TcpClient(serverIp, 8000);
            stream = client.GetStream();
            Console.WriteLine("Connected to the server.");

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
            Console.WriteLine($"Could not connect to server: {ex.Message}");
        }
    }

    private static async Task ReceiveDataAsync()
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
