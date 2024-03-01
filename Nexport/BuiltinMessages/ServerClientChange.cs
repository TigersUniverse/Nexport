namespace Nexport.BuiltinMessages;

[Msg]
public class ServerClientChange
{
    [MsgKey(2)] public ClientIdentifier[]? ConnectedClients;
    [MsgKey(3)] public ClientIdentifier? LocalClientIdentifier;
        
    public ServerClientChange(){}
    public ServerClientChange(List<ClientIdentifier> clients) => ConnectedClients = clients.ToArray();
}