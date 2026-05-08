namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Untyped wrapper over <see cref="InstanceFactory{T}" /> so the materializer can build an entity
/// instance without going through <see cref="Activator.CreateInstance(Type)" />.
/// </summary>
internal interface IInstanceFactory
{
    object Create();
}
