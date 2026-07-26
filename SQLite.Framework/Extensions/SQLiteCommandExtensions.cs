namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="SQLiteCommand" /> Extensions for SQLite.
/// </summary>
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

        Dictionary<string, int> columns = CommandHelpers.GetColumnNames(reader.Statement!);
        SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query: null);
        Func<SQLiteQueryContext, object?> materializer = BuildQueryObject.BuildMaterializer(reader, columns, query: null, typeof(T));

        do
        {
            yield return materializer(context) is { } value ? (T)value : default!;
        } while (reader.Read());
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(SQLiteCommandExtensions))]
    internal static IEnumerable<T> ExecuteQueryInternal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteCommand command, SQLQuery query)
    {
        IEnumerable<T> enumerable = ApplyClientOperators(Enumerate(command, query), query);

        return enumerable;

        static IEnumerable<T> Enumerate(SQLiteCommand command, SQLQuery query)
        {
            using SQLiteDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                yield break;
            }

            Dictionary<string, int> columns = CommandHelpers.GetColumnNames(reader.Statement!);
            SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query);
            Func<SQLiteQueryContext, object?> materializer = BuildQueryObject.BuildMaterializer(reader, columns, query, typeof(T));

            do
            {
                yield return materializer(context) is { } value ? (T)value : default!;
            } while (reader.Read());
        }
    }

    internal static IEnumerable ExecuteQueryUntypedInternal(this SQLiteCommand command, SQLQuery query, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type elementType)
    {
        IEnumerable<object?> enumerable = ApplyClientOperators(Enumerate(command, query, elementType), query);

        return enumerable;

        static IEnumerable<object?> Enumerate(SQLiteCommand command, SQLQuery query, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type elementType)
        {
            using SQLiteDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                yield break;
            }

            Dictionary<string, int> columns = CommandHelpers.GetColumnNames(reader.Statement!);
            SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query);
            Func<SQLiteQueryContext, object?> materializer = BuildQueryObject.BuildMaterializer(reader, columns, query, elementType);

            do
            {
                yield return materializer(context);
            } while (reader.Read());
        }
    }

    private static IEnumerable<T> ApplyClientOperators<T>(IEnumerable<T> enumerable, SQLQuery query)
    {
        if (query.ClientDistinct)
        {
            if (query.Reverse && query.ReverseBeforeDistinct)
            {
                enumerable = enumerable.Reverse();
            }

            enumerable = enumerable.Distinct();

            if (query.Reverse && !query.ReverseBeforeDistinct)
            {
                enumerable = enumerable.Reverse();
            }
        }
        else if (query.Reverse)
        {
            enumerable = enumerable.Reverse();
        }

        if (query.ClientSkip is { } clientSkip)
        {
            enumerable = enumerable.Skip((int)clientSkip);
        }

        if (query.ClientTake is { } clientTake)
        {
            enumerable = enumerable.Take((int)clientTake);
        }

        return enumerable;
    }
}
