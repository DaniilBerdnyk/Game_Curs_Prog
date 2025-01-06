﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Server
{
    private static TcpListener listener;
    private static List<TcpClient> players = new List<TcpClient>();
    private static int playerIdCounter = 1;

    public static async Task Main()
    {
        await StartServerAsync();
    }

    public static async Task StartServerAsync()
    {
        listener = new TcpListener(IPAddress.Any, 8000);
        listener.Start();
        Console.WriteLine("Server started on port 8000...");

        _ = Task.Run(NetworkDiscovery.StartServerDiscoveryAsync);

        while (true)
        {
            try
            {
                TcpClient player = await listener.AcceptTcpClientAsync();
                lock (players)
                {
                    players.Add(player);
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

    private static async Task HandlePlayerAsync(TcpClient player, int playerId)
    {
        try
        {
            NetworkStream stream = player.GetStream();
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                lock (players)
                {
                    foreach (var otherPlayer in players)
                    {
                        if (otherPlayer != player)
                        {
                            await otherPlayer.GetStream().WriteAsync(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error handling player {playerId}: {ex.Message}");
        }
        finally
        {
            lock (players)
            {
                players.Remove(player);
            }
            player?.Close();
            Console.WriteLine($"Player {playerId} disconnected.");
        }
    }
}
