using SQLite.Framework.Tests.Interfaces;

namespace SQLite.Framework.Tests.Helpers;

internal static class GenericConstrainedExtensions
{
    public static int LiveCount<T>(this IQueryable<T> source)
        where T : ISoftDelete
    {
        return source.Count(f => !f.IsDeleted);
    }
}
