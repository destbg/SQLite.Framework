using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds the column map for a referenced common table expression. A CTE whose element type is a
/// single value, such as <c>int</c> or <c>string</c>, exposes one column named
/// <see cref="Constants.CteScalarColumn"/>. Any other element type exposes one column per public
/// property.
/// </summary>
internal static class CteColumnMapper
{
    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Entity element types have public properties.")]
    public static Dictionary<string, Expression> BuildColumns(Type elementType, string prefix, SQLiteOptions options, SQLiteCounters counters)
    {
        if (TypeHelpers.IsSimple(elementType, options))
        {
            return new Dictionary<string, Expression>
            {
                [string.Empty] = SQLiteExpression.Leaf(elementType, counters.NextIdentifier(), $"{prefix}.{IdentifierGuard.Quote(Constants.CteScalarColumn)}")
            };
        }

        return elementType.GetProperties()
            .Where(f => f.GetCustomAttribute<NotMappedAttribute>() == null)
            .ToDictionary(f => f.Name, Expression (f) => SQLiteExpression.Leaf(f.PropertyType, counters.NextIdentifier(), $"{prefix}.{IdentifierGuard.Quote(f.Name)}"));
    }

    public static string[]? ScalarColumnNames(Type elementType, SQLiteOptions options)
    {
        return TypeHelpers.IsSimple(elementType, options) ? [Constants.CteScalarColumn] : null;
    }
}
