using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Server
{
    private TcpListener listener;
    private List<TcpClient> players = new List<TcpClient>();
    private int playerIdCounter = 1;
    private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    public async Task StartServerAsync()
    {
        listener = new TcpListener(IPAddress.Any, 8000);
        listener.Start();
        Console.WriteLine("Server started on port 8000...");

        _ = Task.Run(NetworkDiscovery.StartHostDiscoveryAsync);

        while (true)
        {
            try
            {
                TcpClient player = await listener.AcceptTcpClientAsync();
                await semaphore.WaitAsync();
                try
                {
                    players.Add(player);
                }
                finally
                {
                    semaphore.Release();
                }
                Console.WriteLine($"Player {playerIdCounter} connected.");
                _ = Task.Run(() => HandlePlayerAsync(player, playerIdCounter));
                playerIdCounter++;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Error accepting player: {ex.Message}");
            }
        }
    }

    private async Task HandlePlayerAsync(TcpClient player, int playerId)
    {
        try
        {
            NetworkStream stream = player.GetStream();
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                await semaphore.WaitAsync();
                try
                {
                    foreach (var otherPlayer in players)
                    {
                        if (otherPlayer != player)
                        {
                            await otherPlayer.GetStream().WriteAsync(buffer, 0, bytesRead);
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error handling player {playerId}: {ex.Message}");
        }
        finally
        {
            await semaphore.WaitAsync();
            try
            {
                players.Remove(player);
            }
            finally
            {
                semaphore.Release();
            }
            player?.Close();
            Console.WriteLine($"Player {playerId} disconnected.");
        }
    }
}
