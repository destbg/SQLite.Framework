using System.Text;

namespace SQLite.Framework.Internals.Helpers;

internal static class StringBuilderPool
{
    private const int MaxCapacityToKeep = 4096;

    [ThreadStatic]
    private static StringBuilder? cached;

    public static StringBuilder Rent()
    {
        StringBuilder? sb = cached;
        if (sb == null)
        {
            return new StringBuilder();
        }
        cached = null;
        return sb;
    }

    public static string ToStringAndReturn(StringBuilder sb)
    {
        string result = sb.ToString();
        if (sb.Capacity <= MaxCapacityToKeep)
        {
            sb.Clear();
            cached = sb;
        }
        return result;
    }
}
