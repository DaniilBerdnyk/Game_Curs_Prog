using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Server
{
    private static TcpListener listener;
    private static List<TcpClient> players = new List<TcpClient>();
    private static int playerIdCounter = 1;

    public static void Main()
    {
        listener = new TcpListener(IPAddress.Any, 8000);
        listener.Start();
        Console.WriteLine("Server started on port 8000...");

        Thread acceptThread = new Thread(AcceptClients);
        acceptThread.Start();
    }

    private static void AcceptClients()
    {
        while (true)
        {
            try
            {
                TcpClient player = listener.AcceptTcpClient();
                lock (players)
                {
                    players.Add(player);
                }
                Console.WriteLine($"Player {playerIdCounter} connected.");
                Thread playerThread = new Thread(() => HandlePlayer(player, playerIdCounter));
                playerThread.Start();
                playerIdCounter++;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Error accepting player: {ex.Message}");
            }
        }
    }

    private static void HandlePlayer(TcpClient player, int playerId)
    {
        try
        {
            NetworkStream stream = player.GetStream();
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                lock (players)
                {
                    foreach (var otherPlayer in players)
                    {
                        if (otherPlayer != player)
                        {
                            otherPlayer.GetStream().Write(buffer, 0, bytesRead);
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

