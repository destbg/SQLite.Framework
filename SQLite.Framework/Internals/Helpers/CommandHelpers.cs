using SQLite.Framework.Enums;
using SQLitePCL;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// This class contains helper methods for working with SQLite commands.
/// </summary>
internal static class CommandHelpers
{
    public static Dictionary<string, (int Index, SQLiteColumnType ColumnType)> GetColumnNames(sqlite3_stmt statement)
    {
        int columnCount = raw.sqlite3_column_count(statement);
        Dictionary<string, (int Index, SQLiteColumnType ColumnType)> columnNames = new(columnCount);
        for (int i = 0; i < columnCount; i++)
        {
            string name = raw.sqlite3_column_name(statement, i).utf8_to_string();
            SQLiteColumnType columnType = (SQLiteColumnType)raw.sqlite3_column_type(statement, i);
            columnNames[name] = (i, columnType);
        }

        return columnNames;
    }

    public static object? ReadColumnValue(sqlite3_stmt statement, int index, SQLiteColumnType columnType, Type type, SQLiteStorageOptions options)
    {
        object? value = columnType switch
        {
            SQLiteColumnType.Null => null,
            SQLiteColumnType.Integer => raw.sqlite3_column_int64(statement, index),
            SQLiteColumnType.Real => raw.sqlite3_column_double(statement, index),
            SQLiteColumnType.Text => raw.sqlite3_column_text(statement, index).utf8_to_string(),
            SQLiteColumnType.Blob => raw.sqlite3_column_blob(statement, index).ToArray(),
            _ => throw new NotSupportedException($"Unsupported column type: {columnType}")
        };

        if (value == null)
        {
            return null;
        }

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(DateTime))
        {
            if (value is long ticks)
            {
                return new DateTime(ticks);
            }
            else if (value is string dateString)
            {
                if (long.TryParse(dateString, out long parsedTicks))
                {
                    return new DateTime(parsedTicks);
                }

                return DateTime.Parse(dateString);
            }
        }
        else if (type == typeof(DateTimeOffset))
        {
            if (value is long ticks)
            {
                return new DateTimeOffset(ticks, TimeSpan.Zero);
            }
            else if (value is string dateString)
            {
                return DateTimeOffset.Parse(dateString);
            }
        }
        else if (type == typeof(TimeSpan))
        {
            if (value is long ticks)
            {
                return TimeSpan.FromTicks(ticks);
            }
            else if (value is string timeString)
            {
                return TimeSpan.Parse(timeString);
            }
        }
        else if (type == typeof(DateOnly))
        {
            if (value is long ticks)
            {
                return DateOnly.FromDateTime(new DateTime(ticks));
            }
            else if (value is string dateOnlyString)
            {
                return DateOnly.Parse(dateOnlyString);
            }
        }
        else if (type == typeof(TimeOnly))
        {
            if (value is long ticks)
            {
                return new TimeOnly(ticks);
            }
            else if (value is string timeOnlyString)
            {
                return TimeOnly.Parse(timeOnlyString);
            }
        }
        else if (type == typeof(decimal))
        {
            return Convert.ToDecimal(value);
        }
        else if (type == typeof(Guid))
        {
            if (value is string guidString)
            {
                return Guid.Parse(guidString);
            }
        }
        else if (type == typeof(bool))
        {
            return Convert.ToBoolean(value);
        }
        else if (type.IsEnum)
        {
            if (value is string enumString)
            {
                return Enum.TryParse(type, enumString, out object? parsed) ? parsed : null;
            }

            return Enum.ToObject(type, value);
        }
        else if (options.TypeConverters.TryGetValue(type, out ISQLiteTypeConverter? converter))
        {
            return converter.FromDatabase(value);
        }

        return Convert.ChangeType(value, type);
    }

    public static int BindParameter(sqlite3_stmt statement, string name, object? value, SQLiteStorageOptions options)
    {
        int index = BindParameterIndex(statement, name);

        if (value != null && options.TypeConverters.TryGetValue(value.GetType(), out ISQLiteTypeConverter? converter))
        {
            value = converter.ToDatabase(value);
        }

        return value switch
        {
            null => raw.sqlite3_bind_null(statement, index),
            byte b => raw.sqlite3_bind_int(statement, index, b),
            sbyte sb => raw.sqlite3_bind_int(statement, index, sb),
            short s => raw.sqlite3_bind_int(statement, index, s),
            ushort us => raw.sqlite3_bind_int(statement, index, us),
            int i => raw.sqlite3_bind_int(statement, index, i),
            uint ui => raw.sqlite3_bind_int(statement, index, (int)ui),
            long l => raw.sqlite3_bind_int64(statement, index, l),
            ulong ul => raw.sqlite3_bind_int64(statement, index, (long)ul),
            double d => raw.sqlite3_bind_double(statement, index, d),
            float f => raw.sqlite3_bind_double(statement, index, f),
            char c => raw.sqlite3_bind_text(statement, index, c.ToString()),
            string s => raw.sqlite3_bind_text(statement, index, s),
            byte[] b => raw.sqlite3_bind_blob(statement, index, b),
            bool b => raw.sqlite3_bind_int(statement, index, b ? 1 : 0),
            DateTime dt => options.DateTimeStorage switch
            {
                DateTimeStorageMode.TextTicks => raw.sqlite3_bind_text(statement, index, dt.Ticks.ToString()),
                DateTimeStorageMode.TextFormatted => raw.sqlite3_bind_text(statement, index, dt.ToString(options.DateTimeFormat)),
                _ => raw.sqlite3_bind_int64(statement, index, dt.Ticks)
            },
            DateTimeOffset dto => options.DateTimeOffsetStorage switch
            {
                DateTimeOffsetStorageMode.UtcTicks => raw.sqlite3_bind_int64(statement, index, dto.UtcTicks),
                DateTimeOffsetStorageMode.TextFormatted => raw.sqlite3_bind_text(statement, index, dto.ToString(options.DateTimeOffsetFormat)),
                _ => raw.sqlite3_bind_int64(statement, index, dto.Ticks)
            },
            Guid g => raw.sqlite3_bind_text(statement, index, g.ToString()),
            TimeSpan ts => options.TimeSpanStorage == TimeSpanStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, ts.ToString(options.TimeSpanFormat))
                : raw.sqlite3_bind_int64(statement, index, ts.Ticks),
            DateOnly d => options.DateOnlyStorage == DateOnlyStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, d.ToString(options.DateOnlyFormat))
                : raw.sqlite3_bind_int64(statement, index, d.ToDateTime(default).Ticks),
            TimeOnly t => options.TimeOnlyStorage == TimeOnlyStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, t.ToString(options.TimeOnlyFormat))
                : raw.sqlite3_bind_int64(statement, index, t.Ticks),
            decimal dec => options.DecimalStorage == DecimalStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, dec.ToString(options.DecimalFormat))
                : raw.sqlite3_bind_double(statement, index, (double)dec),
            _ when value.GetType().IsEnum => options.EnumStorage == EnumStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, value.ToString()!)
                : raw.sqlite3_bind_int(statement, index, Convert.ToInt32(value)),
            _ => throw new NotSupportedException($"Type {value.GetType()} is not supported.")
        };
    }

    public static int BindParameterIndex(sqlite3_stmt statement, string name)
    {
        int index = raw.sqlite3_bind_parameter_index(statement, name);
        if (index == 0)
        {
            throw new ArgumentException($"Parameter '{name}' not found in the command text.");
        }

        return index;
    }
}