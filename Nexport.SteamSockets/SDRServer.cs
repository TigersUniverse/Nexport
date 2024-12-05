using Nexport.BuiltinMessages;
using Steamworks;
using Steamworks.Data;

namespace Nexport.Transports.SteamSockets;

public class SDRServer : Server, ISocketManager
{
    private readonly Dictionary<SDRClientIdentifier, Connection> connectedClients =
        new Dictionary<SDRClientIdentifier, Connection>();
    private ServerClientManager<SDRClientIdentifier, Connection>? _clientManager;

    private SocketManager? _socketManager;

    public Func<SteamId, bool>? IsSteamIdAllowed = null;

    public SDRServer(ServerSettings settings) : base(settings)
    { }

    public override TransportType TransportType => 0;
    private bool _isOpen = false;
    public override bool IsOpen => _isOpen;
    public override List<ClientIdentifier> ConnectedClients => new List<ClientIdentifier>(connectedClients.Keys);

    public override void BroadcastMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable, ClientIdentifier? excludeClientIdentifier = null)
    {
        foreach (var client in connectedClients)
        {
            if (excludeClientIdentifier != null && excludeClientIdentifier == client.Key)
                continue;
            client.Value.SendMessage(message, SDRClientIdentifier.GetSendType(messageChannel));
        }
    }

    public override void Close(byte[]? closingMessage = null)
    {
        if (closingMessage != null)
            BroadcastMessage(closingMessage);
        _socketManager?.Close();
        _isOpen = false;
    }

    public override void KickClient(ClientIdentifier client, byte[]? kickMessage = null)
    {
        Connection? connection = _clientManager?.GetServerLinkFromConnected((SDRClientIdentifier)client);
        if (connection != null)
        {
            if (kickMessage != null)
                connection?.SendMessage(kickMessage);
            connection?.Close();
        }
    }

    public override void RunTask()
    {
        _clientManager = new ServerClientManager<SDRClientIdentifier, Connection>(Settings);
        _clientManager.ClientConnected += (identifier, conn) =>
        {
            connectedClients.Add(identifier, conn);
            OnConnect.Invoke(identifier);
            ServerClientChange serverClientChange = new ServerClientChange(ConnectedClients);
            BroadcastMessage(Msg.Serialize(serverClientChange), excludeClientIdentifier: identifier);
            serverClientChange.LocalClientIdentifier = identifier;
            SendMessage(identifier, Msg.Serialize(serverClientChange));
        };
        _clientManager.ClientRemoved += (identifier, conn, wasWaited, wasManualDisconnect) =>
        {
            if (!wasManualDisconnect)
                conn.Close();
            if (!wasWaited)
            {
                connectedClients.Remove(identifier);
            }
            OnDisconnect.Invoke(identifier);
            ServerClientChange serverClientChange = new ServerClientChange(ConnectedClients);
            BroadcastMessage(Msg.Serialize(serverClientChange));
        };
        _socketManager = SteamNetworkingSockets.CreateRelaySocket<SocketManager>(Settings.Port);
        _socketManager.Interface = this;
        _isOpen = true;
    }

    public override void SendMessage(ClientIdentifier client, byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
    {
        Connection? connection = _clientManager?.GetServerLinkFromConnected((SDRClientIdentifier)client);
        if (connection != null)
        {
            connection?.SendMessage(message, SDRClientIdentifier.GetSendType(messageChannel));
        }
    }

    public void OnConnected(Connection connection, ConnectionInfo info)
    {
        SDRClientIdentifier identifier = new SDRClientIdentifier(connection.Id);
        _clientManager?.AddClient(identifier, connection, b =>
        {
            if (!b)
                connection.Close();
        });
    }

    public void OnConnecting(Connection connection, ConnectionInfo info)
    {
        if (IsSteamIdAllowed != null)
        {
            if (IsSteamIdAllowed(info.Identity.SteamId))
            {
                connection.Accept();
            }
            else
            {
                connection.Close();
            }
        }
        else
        {
            connection.Accept();
        }
    }

    public void OnDisconnected(Connection connection, ConnectionInfo info)
    {
        _clientManager?.ClientDisconnected(connection);
    }

    unsafe void ISocketManager.OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        try
        {
            Span<byte> bytes = new Span<byte>(data.ToPointer(), size);
            byte[] msg = bytes.ToArray();
            MsgMeta? msgMeta = Msg.GetMeta(msg);
            if (_clientManager.IsClientWaiting(connection) && msgMeta != null)
            {
                try
                {
                    _clientManager.VerifyWaitingClient(connection, msgMeta, b =>
                    {
                        if (!b)
                            connection.Close();
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to Verify Client " + connection.Id + " for reason " + e);
                    connection.Close();
                }
            }
            else if (msgMeta != null)
            {
                if (_clientManager.IsClientPresent(connection))
                {
                    SDRClientIdentifier? clientIdentifier = _clientManager?.GetClientIdentifierFromConnected(connection);
                    if (clientIdentifier != null)
                        OnMessage.Invoke(clientIdentifier, msgMeta, MessageChannel.Unknown);
                }
                else
                    connection.Close();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("SDRServer failed to deserialize message from " + connection.Id + " for reason " + e);
        }
    }
}