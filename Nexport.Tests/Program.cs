using Nexport.Transports;

namespace Nexport.Tests;

internal class Program
{
    private static Server server;
    private static Client client;
        
    public static void Main(string[] args)
    {
        Console.WriteLine("Is this a server? (y/n)");
        string inp = Console.ReadLine() ?? "n";
        Console.WriteLine("Which Transport? (kcp/telepathy/litenetlib)");
        TransportType transportType = Instantiator.GetTransportTypeFromString(Console.ReadLine() ?? "kcp");
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
            server = Instantiator.InstantiateServer(transportType, serverSettings);
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
            ClientSettings clientSettings = new ClientSettings("127.0.0.1", 3456, useMultithreading: true);
            client = Instantiator.InstantiateClient(transportType, clientSettings);
            client.Create();
            client.OnConnect += () =>
            {
                Console.WriteLine("Connected to Server!");
                client.SendMessage(Msg.Serialize(new AuthMessage
                {
                    Password = password
                }));
            };
            client.JoinedServer += () => Console.WriteLine("Joined Server!");
            client.OnNetworkedClientConnect += identifier =>
                Console.WriteLine("Client connected with identifier of " + identifier.Identifier);
            client.OnNetworkedClientDisconnect += identifier =>
                Console.WriteLine("Client disconnected with identifier of " + identifier.Identifier);
            client.OnDisconnect += () => Console.WriteLine("Disconnected from Server!");
        }
    }
}