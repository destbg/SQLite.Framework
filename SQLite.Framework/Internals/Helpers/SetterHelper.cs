namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Wraps a typed <see cref="Action{TInstance, TValue}" /> built with
/// <see cref="MethodInfo.CreateDelegate(Type)" />. The materializer uses this to call a property
/// setter without going through <see cref="PropertyInfo.SetValue(object?, object?)" />. Built once
/// per property in <see cref="ReflectionMaterializerCache" /> and reused for every row.
/// </summary>
internal sealed class SetterHelper<TInstance, TValue> : ISetterHelper
    where TInstance : class
{
    private readonly Action<TInstance, TValue> typed;

    public SetterHelper(MethodInfo setMethod)
    {
        typed = setMethod.CreateDelegate<Action<TInstance, TValue>>();
        Set = (instance, value) => typed((TInstance)instance, value is null ? default! : (TValue)value);
    }

    public Action<object, object?> Set { get; }
}
