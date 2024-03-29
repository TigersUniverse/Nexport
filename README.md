# Nexport
A Simple C# Networking Library

## Support

Nexport is written in C#, compiled to .Net Framework 4.8 and .NET 7

## How to use

### Creating a Message

Unlike other networking libraries, where you invoke methods to set data with certain types, Nexport allows you to control your messages with classes. Messages are Serialized and Deserialized using [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp).

Message Classes are found once when Msg is first invoked (`static Msg()`), but can be scanned again by calling `Msg.RefreshMessageTypes()`

Below is an example message that can be Serialized and Deserialized

```cs
[Msg]
public class MyCoolMessage
{
    [MsgKey(1)] public string Message1;
    [MsgKey(2)] public int Message2;
}
```

The only restriction is that any serializable class must be on both the Server and Client to be Serialized/Deserialized

If you want to compress the message, simply add the `MsgCompress(int)` attribute

```cs
[Msg]
[MsgCompress(16)]
public class MyCoolMessage
{
    [MsgKey(1)] public string Message1;
    [MsgKey(2)] public int Message2;
}
```

*Compression Level is from 1 to 22*

### Creating a Server

To create a server, all you need to do is create some ServerSettings, and then Instantiate a Server from the Instantiator class.

Below is an example of creating ServerSettings, and a KCP Server

```cs
ServerSettings serverSettings = new ServerSettings("127.0.0.1", 1234);
Server server = Instantiator.InstantiateServer(TransportType.KCP, serverSettings);
```

Then, you can subscribe to some events

```cs
// Invoked when an approved Client connects, where identifier is ClientIdentifier
server.OnConnect += identifier => { };
// Invoked when an approved Client sends a message; where identifier is ClientIdentifier, messageMeta is MsgMeta, and channel is MessageChannel (if available)
server.OnMessage += (identifier, messageMeta, channel) => { };
// Invoked when an approved Client disconnects, where identifier is ClientIdentifier
server.OnDisconnect += identifier => { };
```

Finally, to start the server, just do

```cs
// Creates and Starts the Server
server.Create();
```

To send a message, instantiate the message, serialize it, then send it to a client via. their ClientIdentifier

```cs
MyCoolMessage message = new MyCoolMessage
{
    Message1 = "Hello, World!",
    Message2 = 21
};
byte[] data = Msg.Serialize(message);
// You can also set the third parameter to a certain MessageChannel
server.SendMessage(identifier, data);
```

To Broadcast a message, do the same, but with a different method, and without the ClientIdentifier

```cs
MyCoolMessage message = new MyCoolMessage
{
    Message1 = "Hello, World!",
    Message2 = 21
};
byte[] data = Msg.Serialize(message);
// You can also set the second parameter to a certain MessageChannel
server.BroadcastMessage(data);
```

You can also Broadcast a Message, but exclude one ClientIdentifier

```cs
MyCoolMessage message = new MyCoolMessage
{
    Message1 = "Hello, World!",
    Message2 = 21
};
byte[] data = Msg.Serialize(message);
server.BroadcastMessage(data, excludeClientIdentifier: identifier);
```

If you need to Kick a client, just invoke the KickClient method

```cs
// You can also input byte[] as the second parameter to send a Kick Message
server.KickClient(identifier);
```

When you're done with a server, simply close it

```cs
// You can also send a closing message!
server.Close();
```

### Creating a Client

To create a client, all you need to do is create some ClientSettings, and then Instantiate a Client from the Instantiator class.

Below is an example of creating ClientSettings, and a KCP Client

```cs
ClientSettings clientSettings = new ClientSettings("127.0.0.1", 1234);
Client client = Instantiator.InstantiateClient(TransportType.KCP, clientSettings);
```

Then, you can subscribe to some events

```cs
// Invoked when a client connects to the server
client.OnConnect += () => { };
// Invoked when a client has been verified by the server
client.JoinedServer += () => { };
// Invoked when a Networked Client connects, where identifier is ClientIdentifier
client.OnNetworkedClientConnect += identifier => { };
// Invoked when a client receives a message from the server; where meta is MsgMeta, and channel is MessageChannel
client.OnMessage += (meta, channel) => { };
// Invoked when a Networked Client disconnects, where identifier is ClientIdentifier
client.OnNetworkedClientDisconnect += identifier => { };
// Invoked when a client has disconnected from the server
client.OnDisconnect += () => { };
```

Finally, to start the client, just do

```cs
// Creates and Starts the Client
client.Create();
```

To send a message, instantiate the message, serialize it, then send it to the server

```cs
MyCoolMessage message = new MyCoolMessage
{
    Message1 = "Hello, World!",
    Message2 = 21
};
byte[] data = Msg.Serialize(message);
// You can also set the second parameter to a certain MessageChannel
client.SendMessage(data);
```

When you're done with a client, simply close it

```cs
client.Close();
```

### Handling Messages

When a client or server receives a Message, you will be given an object with the class of MsgMeta. The MsgMeta class is what contains all information about a message, from only its byte array.

```cs
// Assuming Client, no ClientIdentifier needed
public static void HandleMessage(MsgMeta meta)
{
    if(meta.TypeOfData == typeof(MyCoolMessage))
    {
        MyCoolMessage myCoolMessage = (MyCoolMessage) meta.Data;
        // do something with the myCoolMessage object
    }
}
```

Here, we switch-case the possible entries of MessageIds that are handled, then if we find our `MyCoolMessage`, deserialize it, and do something with it. If your code is breakpoint-sensitive, you should wrap this method in a try-catch.

## Transports

+ [kcp2k](https://github.com/vis2k/kcp2k)
+ [Telepathy](https://github.com/vis2k/Telepathy)
+ [LiteNetLib](https://github.com/RevenantX/LiteNetLib)