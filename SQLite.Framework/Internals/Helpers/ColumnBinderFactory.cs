namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds a typed bind delegate for one entity column so a range write can bind the value
/// through a getter delegate and a direct <c>sqlite3_bind_*</c> call, without the boxing and
/// the runtime type switch of <see cref="CommandHelpers.BindParameterByIndex" />. Only the
/// plain column types are handled. Returns <see langword="null" /> for every other shape, in
/// which case the caller falls back to the boxed reflection bind.
/// </summary>
internal static class ColumnBinderFactory
{
    public static Action<sqlite3_stmt, T>? TryCreate<T>(TableColumn column, int parameterIndex, SQLiteOptions options)
    {
        if (typeof(T).IsValueType)
        {
            return null;
        }

        MethodInfo? getMethod = column.PropertyInfo.GetMethod;
        if (getMethod == null || getMethod.IsStatic)
        {
            return null;
        }

        Type type = column.PropertyInfo.PropertyType;
        if (options.TypeConverters.ContainsKey(Nullable.GetUnderlyingType(type) ?? type))
        {
            return null;
        }

        if (type == typeof(int))
        {
            Func<T, int> getter = getMethod.CreateDelegate<Func<T, int>>();
            return (stmt, item) => raw.sqlite3_bind_int(stmt, parameterIndex, getter(item));
        }

        if (type == typeof(long))
        {
            Func<T, long> getter = getMethod.CreateDelegate<Func<T, long>>();
            return (stmt, item) => raw.sqlite3_bind_int64(stmt, parameterIndex, getter(item));
        }

        if (type == typeof(double))
        {
            Func<T, double> getter = getMethod.CreateDelegate<Func<T, double>>();
            return (stmt, item) => raw.sqlite3_bind_double(stmt, parameterIndex, getter(item));
        }

        if (type == typeof(bool))
        {
            Func<T, bool> getter = getMethod.CreateDelegate<Func<T, bool>>();
            return (stmt, item) => raw.sqlite3_bind_int(stmt, parameterIndex, getter(item) ? 1 : 0);
        }

        if (type == typeof(string))
        {
            Func<T, string?> getter = getMethod.CreateDelegate<Func<T, string?>>();
            return (stmt, item) =>
            {
                string? value = getter(item);
                if (value == null)
                {
                    raw.sqlite3_bind_null(stmt, parameterIndex);
                }
                else
                {
                    raw.sqlite3_bind_text(stmt, parameterIndex, value);
                }
            };
        }

        if (type == typeof(int?))
        {
            Func<T, int?> getter = getMethod.CreateDelegate<Func<T, int?>>();
            return (stmt, item) =>
            {
                int? value = getter(item);
                if (value == null)
                {
                    raw.sqlite3_bind_null(stmt, parameterIndex);
                }
                else
                {
                    raw.sqlite3_bind_int(stmt, parameterIndex, value.GetValueOrDefault());
                }
            };
        }

        if (type == typeof(long?))
        {
            Func<T, long?> getter = getMethod.CreateDelegate<Func<T, long?>>();
            return (stmt, item) =>
            {
                long? value = getter(item);
                if (value == null)
                {
                    raw.sqlite3_bind_null(stmt, parameterIndex);
                }
                else
                {
                    raw.sqlite3_bind_int64(stmt, parameterIndex, value.GetValueOrDefault());
                }
            };
        }

        if (type == typeof(double?))
        {
            Func<T, double?> getter = getMethod.CreateDelegate<Func<T, double?>>();
            return (stmt, item) =>
            {
                double? value = getter(item);
                if (value == null)
                {
                    raw.sqlite3_bind_null(stmt, parameterIndex);
                }
                else
                {
                    raw.sqlite3_bind_double(stmt, parameterIndex, value.GetValueOrDefault());
                }
            };
        }

        if (type == typeof(bool?))
        {
            Func<T, bool?> getter = getMethod.CreateDelegate<Func<T, bool?>>();
            return (stmt, item) =>
            {
                bool? value = getter(item);
                if (value == null)
                {
                    raw.sqlite3_bind_null(stmt, parameterIndex);
                }
                else
                {
                    raw.sqlite3_bind_int(stmt, parameterIndex, value.GetValueOrDefault() ? 1 : 0);
                }
            };
        }

        return null;
    }
}
