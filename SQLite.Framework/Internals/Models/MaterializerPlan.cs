namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Reflection data for building objects of a type. Worked out once and held in
/// <see cref="ReflectionMaterializerCache" /> so it can be reused.
/// </summary>
internal sealed class MaterializerPlan
{
    public required PropertySlot[] Slots { get; init; }

    public IInstanceFactory? Factory { get; init; }
}
