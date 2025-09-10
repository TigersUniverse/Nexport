using System.Net;
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
        });
        TaskCompletionSource completionSource = new TaskCompletionSource();
        SteamServer.OnSteamServersConnected += () =>
        {
            Console.WriteLine(SteamServer.LoggedOn);
            completionSource.SetResult();
        };
        SteamServer.LogOnAnonymous();
        SteamServer.AdvertiseServer = true;
        SteamServer.ServerName = "spacewar test";
        SteamServer.MapName = "Map";
        await completionSource.Task.ConfigureAwait(false);
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