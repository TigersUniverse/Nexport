using System.Net;
using System.Net.Sockets;
using Steamworks;
using Steamworks.Data;

namespace Nexport.Transports.SteamSockets;

public class SDRClientIdentifier : ClientIdentifier
{
    public SDRClientIdentifier(uint id) => Identifier = id.ToString();
    public uint ToSteamId() => Convert.ToUInt32(Identifier);

    public static void InitSteamSpacewar()
    {
        Dispatch.OnException = Console.WriteLine;
        SteamClient.Init(480);
    }

    public static async Task InitSteamServerSpacewar(IPAddress ip, ushort port)
    {
        Dispatch.OnException = Console.WriteLine;
        SteamServer.Init(480, new SteamServerInit()
        {
            DedicatedServer = true,
            IpAddress = ip,
            GamePort = port,
            QueryPort = (ushort)(port + 1),
            ModDir = "spacewar",
            GameDescription = "spacewar",
            Secure = false,
            VersionString = "1.0.0.0",
        }.WithQueryShareGamePort());
        TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
        SteamServer.OnSteamServersConnected += () =>
        {
            Console.WriteLine(SteamServer.LoggedOn);
            completionSource.SetResult(true);
        };
        SteamServer.LogOnAnonymous();
        SteamServer.AdvertiseServer = true;
        SteamServer.ServerName = "spacewar test";
        SteamServer.MapName = "Map";
        await completionSource.Task;
        // Thread thread = new Thread(() => UdpServer(port));
        // thread.Start();
    }

    public static void UdpServer(ushort port)
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
        UdpClient client = new UdpClient();
        client.ExclusiveAddressUse = false;
        client.EnableBroadcast = true;
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        // client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
        client.Client.Bind(endPoint);
        // client.JoinMulticastGroup(endPoint.Address);
        while (true)
        {
            if (client.Available > 0)
            {
                byte[] data = client.Receive(ref endPoint);
                if (data.Length > 3 && data[0] == byte.MaxValue && data[1] == byte.MaxValue && data[2] == byte.MaxValue && data[3] == byte.MaxValue)
                {
                    SteamServer.HandleIncomingPacket(data, data.Length, endPoint.Address.IpToInt32(), (ushort)endPoint.Port);
                    while (SteamServer.GetOutgoingPacket(out OutgoingPacket packet))
                    {
                        client.Send(packet.Data, packet.Size, new IPEndPoint(Utility.Int32ToIp(packet.Address), packet.Port));
                    }
                }
                else
                {
                    client.Send(data, data.Length, new IPEndPoint(IPAddress.Any, port));
                }
            }
        }
    }

    public static SendType GetSendType(MessageChannel messageChannel)
    {
        switch (messageChannel)
        {
            case MessageChannel.Reliable:
            case MessageChannel.ReliableUnordered:
            case MessageChannel.ReliableSequenced:
                return SendType.Reliable;
            case MessageChannel.Unreliable:
            case MessageChannel.UnreliableSequenced:
                return SendType.Unreliable;
            default:
                throw new Exception("Unknown MessageChannel " + messageChannel);
        }
    }
}