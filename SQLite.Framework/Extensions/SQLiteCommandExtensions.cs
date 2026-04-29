namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="SQLiteCommand" /> Extensions for SQLite.
/// </summary>
[ExcludeFromCodeCoverage]
public static class SQLiteCommandExtensions
{
    /// <summary>
    /// Executes a query and returns the result set returned by the query.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(SQLiteCommandExtensions))]
    public static IEnumerable<T> ExecuteQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteCommand command)
    {
        using SQLiteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            yield break;
        }

        Dictionary<string, int> columns = CommandHelpers.GetColumnNames(reader.Statement);
        SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query: null);

        do
        {
            yield return (T)BuildQueryObject.CreateInstance(context, typeof(T), query: null)!;
        } while (reader.Read());
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(SQLiteCommandExtensions))]
    internal static IEnumerable<T> ExecuteQueryInternal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteCommand command, SQLQuery query)
    {
        IEnumerable<T> enumerable = Enumerate(command, query);
        return query.Reverse ? enumerable.Reverse() : enumerable;

        static IEnumerable<T> Enumerate(SQLiteCommand command, SQLQuery query)
        {
            using SQLiteDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                yield break;
            }

            Dictionary<string, int> columns = CommandHelpers.GetColumnNames(reader.Statement);
            SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query);

            do
            {
                yield return (T)BuildQueryObject.CreateInstance(context, typeof(T), query)!;
            } while (reader.Read());
        }
    }

    internal static IEnumerable ExecuteQueryUntypedInternal(this SQLiteCommand command, SQLQuery query, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type elementType)
    {
        IEnumerable<object?> enumerable = Enumerate(command, query, elementType);
        return query.Reverse ? enumerable.Reverse() : enumerable;

        static IEnumerable<object?> Enumerate(SQLiteCommand command, SQLQuery query, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type elementType)
        {
            using SQLiteDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                yield break;
            }

            Dictionary<string, int> columns = CommandHelpers.GetColumnNames(reader.Statement);
            SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query);

            do
            {
                yield return BuildQueryObject.CreateInstance(context, elementType, query);
            } while (reader.Read());
        }
    }
}