namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Derives the storage affinity class of a declared column type using the affinity rules from
/// the SQLite documentation, so type names such as <c>INT</c> and <c>INTEGER</c> or
/// <c>VARCHAR</c> and <c>TEXT</c> compare as equivalent while different classes still differ.
/// </summary>
internal static class TypeAffinityResolver
{
    public static SQLiteTypeAffinity Resolve(string declaredType)
    {
        if (declaredType.Contains("INT", StringComparison.OrdinalIgnoreCase))
        {
            return SQLiteTypeAffinity.Integer;
        }

        if (declaredType.Contains("CHAR", StringComparison.OrdinalIgnoreCase)
            || declaredType.Contains("CLOB", StringComparison.OrdinalIgnoreCase)
            || declaredType.Contains("TEXT", StringComparison.OrdinalIgnoreCase))
        {
            return SQLiteTypeAffinity.Text;
        }

        if (declaredType.Length == 0 || declaredType.Contains("BLOB", StringComparison.OrdinalIgnoreCase))
        {
            return SQLiteTypeAffinity.Blob;
        }

        if (declaredType.Contains("REAL", StringComparison.OrdinalIgnoreCase)
            || declaredType.Contains("FLOA", StringComparison.OrdinalIgnoreCase)
            || declaredType.Contains("DOUB", StringComparison.OrdinalIgnoreCase))
        {
            return SQLiteTypeAffinity.Real;
        }

        return SQLiteTypeAffinity.Numeric;
    }
}
