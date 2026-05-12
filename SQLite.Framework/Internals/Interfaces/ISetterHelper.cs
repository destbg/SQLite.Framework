namespace SQLite.Framework.Internals.Interfaces;

/// <summary>
/// Untyped wrapper over a <see cref="SetterHelper{TInstance, TValue}" /> so the materializer can
/// call the typed setter without knowing the exact generic type.
/// </summary>
internal interface ISetterHelper
{
    Action<object, object?> Set { get; }
}
