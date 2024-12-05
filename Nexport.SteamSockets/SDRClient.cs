using Nexport;
using Nexport.Transports;
using Steamworks;
using Steamworks.Data;

namespace Nexport.Transports.SteamSockets;

public class SDRClient : Client, IConnectionManager
{
    public override TransportType TransportType => 0;
    public override bool IsOpen => _connectionManager?.Connected ?? false;
    private ConnectionManager? _connectionManager;

    public SDRClient(ClientSettings settings) : base(settings)
    { }

    public void OnConnected(ConnectionInfo info)
    {
        OnConnect();
    }

    public void OnConnecting(ConnectionInfo info)
    {
    }

    public void OnDisconnected(ConnectionInfo info)
    {
        OnDisconnect();
    }

    unsafe void IConnectionManager.OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        try
        {
            Span<byte> bytes = new Span<byte>(data.ToPointer(), size);
            byte[] byteData = bytes.ToArray();
            MsgMeta? meta = Msg.GetMeta(byteData);
            if (meta != null)
                OnMessage.Invoke(meta, MessageChannel.Unknown);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to read message for reason " + e);
        }
    }

    public override void Close(byte[]? closingMessage = null)
    {
        if (closingMessage != null)
            SendMessage(closingMessage);
        _connectionManager?.Close();
    }

    public override void RunTask()
    {
        if (!ulong.TryParse(Settings.Ip, out ulong id))
            return;
        _connectionManager = SteamNetworkingSockets.ConnectRelay<ConnectionManager>(id, Settings.Port);
        _connectionManager.Interface = this;
    }

    public override void SendMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
    {
        _connectionManager?.Connection.SendMessage(message, SDRClientIdentifier.GetSendType(messageChannel));
    }
}