using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class NetworkDiscovery
{
    private const int DiscoveryPort = 8001;
    private const string DiscoveryMessage = "DISCOVER_SERVER";

    public static async Task<IPAddress> DiscoverServerAsync()
    {
        using (var udpClient = new UdpClient())
        {
            udpClient.EnableBroadcast = true;
            var endpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
            var message = Encoding.UTF8.GetBytes(DiscoveryMessage);

            await udpClient.SendAsync(message, message.Length, endpoint);

            var receiveTask = udpClient.ReceiveAsync();
            if (await Task.WhenAny(receiveTask, Task.Delay(5000)) == receiveTask)
            {
                var response = await receiveTask;
                return response.RemoteEndPoint.Address;
            }
        }

        return null;
    }

    public static async Task StartServerDiscoveryAsync()
    {
        using (var udpClient = new UdpClient(DiscoveryPort))
        {
            while (true)
            {
                var result = await udpClient.ReceiveAsync();
                var message = Encoding.UTF8.GetString(result.Buffer);

                if (message == DiscoveryMessage)
                {
                    var response = Encoding.UTF8.GetBytes("SERVER_HERE");
                    await udpClient.SendAsync(response, response.Length, result.RemoteEndPoint);
                }
            }
        }
    }
}
