using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game_Curs_Prog;

public static class NetworkManager
{
    private static UdpClient udpClient;
    private static bool isServer;
    private static ConcurrentDictionary<IPEndPoint, Teammate> teammates = new ConcurrentDictionary<IPEndPoint, Teammate>();
    private static ConcurrentDictionary<IPEndPoint, DateTime> clientLastResponse = new ConcurrentDictionary<IPEndPoint, DateTime>();
    private static List<Entity> entities = new List<Entity>();
    private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public static async Task InitNetwork(string[] args)
    {
        try
        {
            udpClient = new UdpClient(7000);
            isServer = true;
            Console.WriteLine("Running in Server (Host) mode on port 7000");
        }
        catch (SocketException)
        {
            udpClient = new UdpClient();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            isServer = false;
            Console.WriteLine("Running in Client mode on port " + ((IPEndPoint)udpClient.Client.LocalEndPoint).Port);
        }

        // Запуск асинхронных задач с Task.Run
        Task.Run(async () => await ReceiveMessages());
        Task.Run(async () => await SendHeroData(Program.player));
        Task.Run(async () => await CheckClientActivity());

        // Блокирующий ввод перенесен в отдельный поток для сервера
        if (isServer)
        {
            Task.Run(() =>
            {
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                cancellationTokenSource.Cancel();
            });
        }
    }

    // Отправка данных героя
    public static async Task SendHeroData(Hero player)
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (Program.player != null)
            {
                string heroData = $"{Program.player.X},{Program.player.Y}";
                byte[] data = Encoding.UTF8.GetBytes(heroData);

                foreach (var clientEndPoint in teammates.Keys)
                {
                    try
                    {
                        await udpClient.SendAsync(data, data.Length, clientEndPoint);
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Error sending to client at {clientEndPoint.Address}:{clientEndPoint.Port}: {ex.Message}");
                    }
                }
            }

            await Task.Delay(100);
        }
    }

    // Получение сообщений
    private static async Task ReceiveMessages()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string receivedMessage = Encoding.UTF8.GetString(result.Buffer);
                IPEndPoint clientEndPoint = result.RemoteEndPoint;

                CreateOrUpdateTeammate(clientEndPoint, receivedMessage);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
            }
        }
    }

    // Создание или обновление тиммейта
    private static void CreateOrUpdateTeammate(IPEndPoint clientEndPoint, string data)
    {
        var teammate = CreateTeammateFromData(data);

        if (teammate != null)
        {
            teammates[clientEndPoint] = teammate;

            if (!entities.Contains(teammate))
            {
                entities.Add(teammate);
            }
        }

        Console.WriteLine($"Client updated/added: {clientEndPoint.Address}:{clientEndPoint.Port}");
    }

    // Создание объекта тиммейта из данных
    private static Teammate CreateTeammateFromData(string data)
    {
        try
        {
            var parts = data.Split(',');
            return new Teammate(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), 'T');
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating teammate from data: {ex.Message}");
            return null;
        }
    }

    // Проверка активности клиентов
    public static async Task CheckClientActivity()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            DateTime now = DateTime.Now;
            var inactiveClients = new List<IPEndPoint>();

            foreach (var client in teammates.Keys)
            {
                if ((now - clientLastResponse[client]).TotalSeconds > 5)
                {
                    Console.WriteLine($"Client at {client} is now inactive");
                    teammates.TryRemove(client, out Teammate removedTeammate);
                    if (removedTeammate != null)
                    {
                        entities.Remove(removedTeammate);
                    }
                    inactiveClients.Add(client);
                }
            }

            foreach (var client in inactiveClients)
            {
                teammates.TryRemove(client, out _);
            }

            await Task.Delay(2000);
        }
    }

    // Начало общения с сервером
    public static async Task StartCommunicating(IPEndPoint serverEndPoint)
    {
        Task.Run(async () => await ReceiveMessagesFromServer());
        Task.Run(async () => await SendMessageToServer(serverEndPoint));
    }

    // Получение сообщений от сервера
    private static async Task ReceiveMessagesFromServer()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string receivedMessage = Encoding.UTF8.GetString(result.Buffer);
                IPEndPoint serverEndPoint = result.RemoteEndPoint;

                CreateOrUpdateTeammate(serverEndPoint, receivedMessage);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
            }
        }
    }

    // Отправка сообщений на сервер
    private static async Task SendMessageToServer(IPEndPoint serverEndPoint)
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (Program.player != null)
            {
                string heroData = $"{Program.player.X},{Program.player.Y}";
                byte[] data = Encoding.UTF8.GetBytes(heroData);
                await udpClient.SendAsync(data, data.Length, serverEndPoint);
            }

            await Task.Delay(100);
        }
    }
}
