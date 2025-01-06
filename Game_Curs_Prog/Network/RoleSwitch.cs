using System;
using System.Threading.Tasks;

public class RoleSwitch
{
    public static async Task Main(string[] args)
    {
        var hostIp = await NetworkDiscovery.DiscoverHostAsync();

        if (hostIp == null)
        {
            Console.WriteLine("No host found. Starting as host...");
            var server = new Server();
            await server.StartServerAsync();
        }
        else
        {
            Console.WriteLine($"Host found at {hostIp}. Starting as client...");
            var client = new Client();
            await client.StartClientAsync(hostIp.ToString());
        }
    }
}
