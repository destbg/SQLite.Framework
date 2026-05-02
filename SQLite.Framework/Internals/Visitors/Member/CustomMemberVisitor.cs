namespace SQLite.Framework.Internals.Visitors.Member;

internal static class CustomMemberVisitor
{
    internal static string TrimClose(string sql)
    {
        return sql[..^1];
    }
}
