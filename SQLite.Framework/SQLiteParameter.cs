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

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Name} = {Value}";
    }
}
