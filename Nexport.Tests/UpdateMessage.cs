namespace Nexport.Tests;

[Msg]
public class UpdateMessage
{
    [MsgKey(1)] public string MessageId => typeof(UpdateMessage).FullName;
    [MsgKey(2)] public List<object> Objects = new List<object>();

    public UpdateMessage Fill(int times)
    {
        for (int i = 0; i < times; i++)
        {
            float randomNumber = (float) new Random().NextDouble() * 1000;
            Objects.Add(randomNumber);
        }
        return this;
    }
}