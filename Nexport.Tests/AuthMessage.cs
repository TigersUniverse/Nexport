namespace Nexport.Tests
{
    [Msg]
    public class AuthMessage
    {
        [MsgKey(1)] public string MessageId => typeof(AuthMessage).FullName;
        [MsgKey(2)] public string Password;
    }
}