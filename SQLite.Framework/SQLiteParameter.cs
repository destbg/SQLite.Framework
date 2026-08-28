namespace SQLite.Framework;

/// <summary>
/// Represents a parameter to be used in a SQLite command.
/// </summary>
public class SQLiteParameter
{
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The value of the parameter.
    /// </summary>
    public required object? Value { get; init; }

    /// <summary>
    /// The type the value was declared with, such as the property type of the column it came from.
    /// The write converter is chosen by this type first. The runtime type of the value is used only
    /// when this is not set, so a derived value of a polymorphic JSON type keeps its type discriminator.
    /// </summary>
    internal Type? DeclaredType { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Name} = {Value}";
    }
}
