namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// This class contains helper methods for working with SQLite commands.
/// </summary>
internal static class CommandHelpers
{
    public static Dictionary<string, int> GetColumnNames(sqlite3_stmt statement)
    {
        int columnCount = raw.sqlite3_column_count(statement);
        Dictionary<string, int> columnNames = new(columnCount);
        for (int i = 0; i < columnCount; i++)
        {
            string name = raw.sqlite3_column_name(statement, i).utf8_to_string();
            columnNames[name] = i;
        }

        return columnNames;
    }

    public static object? ReadColumnValue(sqlite3_stmt statement, int index, SQLiteColumnType columnType, Type type, SQLiteOptions options)
    {
        if (columnType == SQLiteColumnType.Null)
        {
            return null;
        }

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (options.TypeConverters.TryGetValue(type, out ISQLiteTypeConverter? typeConverter))
        {
            return typeConverter.FromDatabase(ReadRawValue(statement, index, columnType));
        }
        else if (type == typeof(DateTime))
        {
            return ReadDateTime(statement, index, columnType, options);
        }
        else if (type == typeof(DateTimeOffset))
        {
            return ReadDateTimeOffset(statement, index, columnType, options);
        }
        else if (type == typeof(TimeSpan))
        {
            return ReadTimeSpan(statement, index, columnType, options);
        }
        else if (type == typeof(DateOnly))
        {
            return ReadDateOnly(statement, index, columnType, options);
        }
        else if (type == typeof(TimeOnly))
        {
            return ReadTimeOnly(statement, index, columnType, options);
        }
        else if (type == typeof(Guid))
        {
            return ReadGuid(statement, index, columnType);
        }
        else if (type == typeof(decimal))
        {
            return ReadDecimal(statement, index, columnType, options);
        }

        object value = ReadRawValue(statement, index, columnType);

        if (type == typeof(object))
        {
            return value;
        }
        else if (type == typeof(uint))
        {
            if (value is long uintValue)
            {
                return unchecked((uint)uintValue);
            }
        }
        else if (type == typeof(ulong))
        {
            if (value is long ulongValue)
            {
                return unchecked((ulong)ulongValue);
            }
        }
        else if (type == typeof(bool))
        {
            return Convert.ToBoolean(value);
        }
        else if (options.TypeConverters.TryGetValue(type, out ISQLiteTypeConverter? converter))
        {
            return converter.FromDatabase(value);
        }
        else if (type.IsEnum)
        {
            if (value is string enumString)
            {
                return Enum.TryParse(type, enumString, out object? parsed) ? parsed : null;
            }

            return Enum.ToObject(type, value);
        }
        else if (type == typeof(string) && value is byte[] blobBytes)
        {
            return Encoding.UTF8.GetString(blobBytes);
        }

        return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }

    public static object ReadRawValue(sqlite3_stmt statement, int index, SQLiteColumnType columnType)
    {
        return columnType switch
        {
            SQLiteColumnType.Integer => raw.sqlite3_column_int64(statement, index),
            SQLiteColumnType.Real => raw.sqlite3_column_double(statement, index),
            SQLiteColumnType.Text => raw.sqlite3_column_text(statement, index).utf8_to_string(),
            SQLiteColumnType.Blob => raw.sqlite3_column_blob(statement, index).ToArray(),
            _ => throw new NotSupportedException($"Unsupported column type: {columnType}")
        };
    }

    public static DateTime ReadDateTime(sqlite3_stmt statement, int index, SQLiteColumnType columnType, SQLiteOptions options)
    {
        if (columnType == SQLiteColumnType.Integer)
        {
            return new DateTime(raw.sqlite3_column_int64(statement, index));
        }

        if (columnType == SQLiteColumnType.Text)
        {
            string dateString = raw.sqlite3_column_text(statement, index).utf8_to_string();
            if (options.DateTimeStorage == DateTimeStorageMode.TextFormatted)
            {
                return DateTime.ParseExact(dateString, options.DateTimeFormat, CultureInfo.InvariantCulture);
            }

            if (long.TryParse(dateString, out long parsedTicks))
            {
                return new DateTime(parsedTicks);
            }

            return DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        }

        return (DateTime)Convert.ChangeType(ReadRawValue(statement, index, columnType), typeof(DateTime), CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset ReadDateTimeOffset(sqlite3_stmt statement, int index, SQLiteColumnType columnType, SQLiteOptions options)
    {
        if (columnType == SQLiteColumnType.Integer)
        {
            return new DateTimeOffset(raw.sqlite3_column_int64(statement, index), TimeSpan.Zero);
        }

        if (columnType == SQLiteColumnType.Text)
        {
            string dateString = raw.sqlite3_column_text(statement, index).utf8_to_string();
            return options.DateTimeOffsetStorage == DateTimeOffsetStorageMode.TextFormatted
                ? DateTimeOffset.ParseExact(dateString, options.DateTimeOffsetFormat, CultureInfo.InvariantCulture)
                : DateTimeOffset.Parse(dateString, CultureInfo.InvariantCulture);
        }

        return (DateTimeOffset)Convert.ChangeType(ReadRawValue(statement, index, columnType), typeof(DateTimeOffset), CultureInfo.InvariantCulture);
    }

    public static TimeSpan ReadTimeSpan(sqlite3_stmt statement, int index, SQLiteColumnType columnType, SQLiteOptions options)
    {
        if (columnType == SQLiteColumnType.Integer)
        {
            return TimeSpan.FromTicks(raw.sqlite3_column_int64(statement, index));
        }

        if (columnType == SQLiteColumnType.Text)
        {
            string timeString = raw.sqlite3_column_text(statement, index).utf8_to_string();
            return TimeSpan.ParseExact(timeString, options.TimeSpanFormat, CultureInfo.InvariantCulture);
        }

        return (TimeSpan)Convert.ChangeType(ReadRawValue(statement, index, columnType), typeof(TimeSpan), CultureInfo.InvariantCulture);
    }

    public static DateOnly ReadDateOnly(sqlite3_stmt statement, int index, SQLiteColumnType columnType, SQLiteOptions options)
    {
        if (columnType == SQLiteColumnType.Integer)
        {
            return DateOnly.FromDateTime(new DateTime(raw.sqlite3_column_int64(statement, index)));
        }

        if (columnType == SQLiteColumnType.Text)
        {
            string dateOnlyString = raw.sqlite3_column_text(statement, index).utf8_to_string();
            return DateOnly.ParseExact(dateOnlyString, options.DateOnlyFormat, CultureInfo.InvariantCulture);
        }

        return (DateOnly)Convert.ChangeType(ReadRawValue(statement, index, columnType), typeof(DateOnly), CultureInfo.InvariantCulture);
    }

    public static TimeOnly ReadTimeOnly(sqlite3_stmt statement, int index, SQLiteColumnType columnType, SQLiteOptions options)
    {
        if (columnType == SQLiteColumnType.Integer)
        {
            return new TimeOnly(raw.sqlite3_column_int64(statement, index));
        }

        if (columnType == SQLiteColumnType.Text)
        {
            string timeOnlyString = raw.sqlite3_column_text(statement, index).utf8_to_string();
            return TimeOnly.ParseExact(timeOnlyString, options.TimeOnlyFormat, CultureInfo.InvariantCulture);
        }

        return (TimeOnly)Convert.ChangeType(ReadRawValue(statement, index, columnType), typeof(TimeOnly), CultureInfo.InvariantCulture);
    }

    public static Guid ReadGuid(sqlite3_stmt statement, int index, SQLiteColumnType columnType)
    {
        if (columnType == SQLiteColumnType.Text)
        {
            return Guid.Parse(raw.sqlite3_column_text(statement, index).utf8_to_string());
        }

        return (Guid)Convert.ChangeType(ReadRawValue(statement, index, columnType), typeof(Guid), CultureInfo.InvariantCulture);
    }

    public static decimal ReadDecimal(sqlite3_stmt statement, int index, SQLiteColumnType columnType, SQLiteOptions options)
    {
        if (columnType == SQLiteColumnType.Text)
        {
            return decimal.Parse(raw.sqlite3_column_text(statement, index).utf8_to_string(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        double d = columnType == SQLiteColumnType.Real
            ? raw.sqlite3_column_double(statement, index)
            : Convert.ToDouble(ReadRawValue(statement, index, columnType), CultureInfo.InvariantCulture);
        if (d >= (double)decimal.MaxValue)
        {
            return decimal.MaxValue;
        }

        if (d <= (double)decimal.MinValue)
        {
            return decimal.MinValue;
        }

        return (decimal)d;
    }

    public static int BindParameter(sqlite3_stmt statement, string name, object? value, SQLiteOptions options)
    {
        int index = BindParameterIndex(statement, name);
        return BindParameterByIndex(statement, index, value, options);
    }

    public static int BindParameterByIndex(sqlite3_stmt statement, int index, object? value, SQLiteOptions options)
    {
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
            uint ui => raw.sqlite3_bind_int64(statement, index, ui),
            long l => raw.sqlite3_bind_int64(statement, index, l),
            ulong ul => raw.sqlite3_bind_int64(statement, index, (long)ul),
            double d => raw.sqlite3_bind_double(statement, index, d),
            float f => raw.sqlite3_bind_double(statement, index, f),
            char c => options.CharStorage == CharStorageMode.Integer
                ? raw.sqlite3_bind_int(statement, index, c)
                : raw.sqlite3_bind_text(statement, index, c.ToString()),
            string s => raw.sqlite3_bind_text(statement, index, s),
            byte[] b => raw.sqlite3_bind_blob(statement, index, b),
            bool b => raw.sqlite3_bind_int(statement, index, b ? 1 : 0),
            DateTime dt => options.DateTimeStorage switch
            {
                DateTimeStorageMode.TextTicks => raw.sqlite3_bind_text(statement, index, dt.Ticks.ToString(CultureInfo.InvariantCulture)),
                DateTimeStorageMode.TextFormatted => raw.sqlite3_bind_text(statement, index, dt.ToString(options.DateTimeFormat, CultureInfo.InvariantCulture)),
                _ => raw.sqlite3_bind_int64(statement, index, dt.Ticks)
            },
            DateTimeOffset dto => options.DateTimeOffsetStorage switch
            {
                DateTimeOffsetStorageMode.UtcTicks => raw.sqlite3_bind_int64(statement, index, dto.UtcTicks),
                DateTimeOffsetStorageMode.TextFormatted => raw.sqlite3_bind_text(statement, index, dto.ToString(options.DateTimeOffsetFormat, CultureInfo.InvariantCulture)),
                _ => raw.sqlite3_bind_int64(statement, index, dto.Ticks)
            },
            Guid g => raw.sqlite3_bind_text(statement, index, g.ToString()),
            TimeSpan ts => options.TimeSpanStorage == TimeSpanStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, ts.ToString(options.TimeSpanFormat, CultureInfo.InvariantCulture))
                : raw.sqlite3_bind_int64(statement, index, ts.Ticks),
            DateOnly d => options.DateOnlyStorage == DateOnlyStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, d.ToString(options.DateOnlyFormat, CultureInfo.InvariantCulture))
                : raw.sqlite3_bind_int64(statement, index, d.ToDateTime(default).Ticks),
            TimeOnly t => options.TimeOnlyStorage == TimeOnlyStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, t.ToString(options.TimeOnlyFormat, CultureInfo.InvariantCulture))
                : raw.sqlite3_bind_int64(statement, index, t.Ticks),
            decimal dec => options.DecimalStorage == DecimalStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, dec.ToString(options.DecimalFormat, CultureInfo.InvariantCulture))
                : raw.sqlite3_bind_double(statement, index, (double)dec),
            _ when value.GetType().IsEnum => options.EnumStorage == EnumStorageMode.Text
                ? raw.sqlite3_bind_text(statement, index, value.ToString()!)
                : raw.sqlite3_bind_int64(statement, index, EnumToInt64(value)),
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

    private static long EnumToInt64(object value)
    {
        Type underlying = Enum.GetUnderlyingType(value.GetType());
        return underlying == typeof(ulong)
            ? unchecked((long)Convert.ToUInt64(value, CultureInfo.InvariantCulture))
            : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }
}
