namespace Nexport;

[AttributeUsage(AttributeTargets.Class)]
public class MsgCompress : Attribute
{
    internal int Level { get; }

    public MsgCompress(int level) => Level = level;
}