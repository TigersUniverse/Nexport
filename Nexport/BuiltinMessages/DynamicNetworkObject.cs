using System.Reflection;

namespace Nexport.BuiltinMessages;

/// <summary>
/// A message which can by dynamically set. Simply construct a DynamicNetworkObject with the Data and its type, 
/// then, when deserializing it, call Fix(), then Data will be the correct Type!
/// </summary>
[Msg]
public class DynamicNetworkObject
{
    private const BindingFlags BINDINGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    
    [MsgKey(2)] public string TypeFullName { get; set; }
    [MsgKey(3)] public object? Data { get; set; }

    /// <summary>
    /// Gets the Type the object was before it was restored.
    /// </summary>
    /// <returns>The restored Type</returns>
    public Type? GetRestoredType()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            Type[] assemblyTypes = assembly.GetTypes();
            foreach (Type type in assemblyTypes)
            {
                if(type.FullName != TypeFullName) continue;
                return type;
            }
        }
        return null;
    }

    /// <summary>
    /// Fixes the Dynamic Object after it was serialized, returning it to its previous deserialized state.
    /// </summary>
    /// <exception cref="NullReferenceException">The original Type is not in any loaded assembly</exception>
    public void Fix()
    {
        if(Data == null) return;
        if(Data.GetType() != typeof(object[])) return;
        Type? target = GetRestoredType();
        if (target == null) throw new NullReferenceException();
        object instance = Activator.CreateInstance(target);
        List<(MsgKey, FieldInfo?, PropertyInfo?)> members = new List<(MsgKey, FieldInfo?, PropertyInfo?)>();
        foreach (PropertyInfo p in target.GetProperties(BINDINGS))
        {
            Attribute[] attributes = p.GetCustomAttributes(typeof(MsgKey)).ToArray();
            if(attributes.Length <= 0) continue;
            members.Add(((MsgKey) attributes[0], null, p));
        }
        foreach (FieldInfo f in target.GetFields(BINDINGS))
        {
            Attribute[] attributes = f.GetCustomAttributes(typeof(MsgKey)).ToArray();
            if(attributes.Length <= 0) continue;
            members.Add(((MsgKey) attributes[0], f, null));
        }
        foreach ((MsgKey, FieldInfo?, PropertyInfo?) member in members)
        {
            object? o = ((object?[]) Data)[member.Item1.Identifier];
            if (o != null)
            {
                Type oType = o.GetType();
                if (oType == typeof(object[]) || oType == typeof(object?[]))
                {
                    Type memberType = member.Item2 != null
                        ? member.Item2.FieldType
                        : member.Item3?.PropertyType ?? throw new NullReferenceException();
                    o = FixNesting(memberType, (object?[]) o);
                }
            }
            if (member.Item2 != null)
                member.Item2.SetValue(instance, o == null ? null : Convert.ChangeType(o, member.Item2.FieldType));
            else if(member.Item3 != null)
                member.Item3.SetValue(instance, o == null ? null : Convert.ChangeType(o, member.Item3.PropertyType));
        }
        Data = instance;
    }

    private object FixNesting(Type memberType, object?[] arr)
    {
        object instance = Activator.CreateInstance(memberType);
        List<(MsgKey, FieldInfo?, PropertyInfo?)> members = new List<(MsgKey, FieldInfo?, PropertyInfo?)>();
        foreach (PropertyInfo p in memberType.GetProperties(BINDINGS))
        {
            Attribute[] attributes = p.GetCustomAttributes(typeof(MsgKey)).ToArray();
            if(attributes.Length <= 0) continue;
            members.Add(((MsgKey) attributes[0], null, p));
        }
        foreach (FieldInfo f in memberType.GetFields(BINDINGS))
        {
            Attribute[] attributes = f.GetCustomAttributes(typeof(MsgKey)).ToArray();
            if(attributes.Length <= 0) continue;
            members.Add(((MsgKey) attributes[0], f, null));
        }
        foreach ((MsgKey, FieldInfo?, PropertyInfo?) member in members)
        {
            object? o = arr[member.Item1.Identifier];
            if (o != null)
            {
                Type oType = o.GetType();
                if (oType == typeof(object[]) || oType == typeof(object?[]))
                {
                    Type m = member.Item2 != null
                        ? member.Item2.FieldType
                        : member.Item3?.PropertyType ?? throw new NullReferenceException();
                    o = FixNesting(m, (object?[]) o);
                }
            }
            if (member.Item2 != null)
                member.Item2.SetValue(instance, o == null ? null : Convert.ChangeType(o, member.Item2.FieldType));
            else if(member.Item3 != null)
                member.Item3.SetValue(instance, o == null ? null : Convert.ChangeType(o, member.Item3.PropertyType));
        }
        return instance;
    }
}