using Nexport.Transports;
using Nexport.Transports.SteamSockets;

namespace Nexport.Tests;

internal class Program
{
    private static Server server;
    private static Client client;
        
    public static void Main(string[] args)
    {
        Console.WriteLine("Is this a server? (y/n)");
        string inp = Console.ReadLine() ?? "n";
        Console.WriteLine("Which Transport? (kcp/telepathy/litenetlib/sdr)");
        string transport = Console.ReadLine() ?? "kcp";
        bool isSdr = transport.ToLower().Equals("sdr");
        if (isSdr)
        {
            SDRClientIdentifier.InitSteamSpacewar();
        }
        if (inp.ToLower().Contains("y"))
        {
            ServerSettings serverSettings = new ServerSettings("0.0.0.0", 3456, useMultithreading: true, requireMessageAuth: true)
            {
                ValidateMessage = (identifier, meta, result) =>
                {
                    AuthMessage authMessage = Msg.Deserialize<AuthMessage>(meta.RawData);
                    result.Invoke(authMessage.Password == "1234");
                }
            };
            if (isSdr)
            {
                server = new SDRServer(serverSettings);
            }
            else
            {
                TransportType transportType = Instantiator.GetTransportTypeFromString(transport);
                server = Instantiator.InstantiateServer(transportType, serverSettings);
            }
            server.Create();
            server.OnConnect += identifier =>
                Console.WriteLine("Client connected with identifier of " + identifier.Identifier);
            server.OnDisconnect += identifier =>
                Console.WriteLine("Client disconnected with identifier of " + identifier.Identifier);
        }
        else
        {
            Console.WriteLine("What is the Password to Connect?");
            string password = Console.ReadLine() ?? "idk";
            Console.WriteLine("What is the IP Address? (leave blank for 127.0.0.1)");
            string ip = Console.ReadLine() ?? "127.0.0.1";
            ClientSettings clientSettings = new ClientSettings(ip, 3456, useMultithreading: true);
            if (isSdr)
            {
                client = new SDRClient(clientSettings);
            }
            else
            {
                TransportType transportType = Instantiator.GetTransportTypeFromString(transport);
                client = Instantiator.InstantiateClient(transportType, clientSettings);
            }
            client.Create();
            client.OnConnect += () =>
            {
                Console.WriteLine("Connected to Server!");
                client.SendMessage(Msg.Serialize(new AuthMessage
                {
                    Password = password
                }));
            };
            client.JoinedServer += () =>
            {
                Console.WriteLine("Joined Server!");
                new Thread(() =>
                {
                    while (client.IsOpen)
                    {
                        DateTime before = DateTime.Now;
                        byte[] data = Msg.Serialize(new UpdateMessage().Fill(10000));
                        Console.WriteLine($"Compression took {(DateTime.Now - before).Milliseconds}ms with size of {data.Length} bytes");
                        client.SendMessage(data);
                        Thread.Sleep(1);
                    }
                }).Start();
            };
            client.OnNetworkedClientConnect += identifier =>
                Console.WriteLine("Client connected with identifier of " + identifier.Identifier);
            client.OnNetworkedClientDisconnect += identifier =>
                Console.WriteLine("Client disconnected with identifier of " + identifier.Identifier);
            client.OnDisconnect += () => Console.WriteLine("Disconnected from Server!");
        }
        Console.ReadKey(true);
    }
}