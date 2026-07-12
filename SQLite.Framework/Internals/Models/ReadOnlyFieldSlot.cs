namespace SQLite.Framework.Internals.Models;

/// <summary>
/// A materializer slot for a get-only property that no constructor parameter supplies. The value
/// is written through the compiler generated backing field after the instance is constructed.
/// </summary>
internal sealed class ReadOnlyFieldSlot
{
    /// <summary>
    /// The compiler generated backing field of the property.
    /// </summary>
    public required FieldInfo Field { get; init; }

    /// <summary>
    /// The result column the value is read from.
    /// </summary>
    public required int ColumnIndex { get; init; }

    /// <summary>
    /// The declared property type, used for the column read.
    /// </summary>
    public required Type DeclaredType { get; init; }

    /// <summary>
    /// The property type with any Nullable wrapper removed.
    /// </summary>
    public required Type TargetType { get; init; }

    /// <summary>
    /// Whether the target type is an enum.
    /// </summary>
    public required bool IsEnum { get; init; }

    /// <summary>
    /// The underlying integer type of the enum or null when the target is not an enum.
    /// </summary>
    public required Type? EnumUnderlyingType { get; init; }
}
