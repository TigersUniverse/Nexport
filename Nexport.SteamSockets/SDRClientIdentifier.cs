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