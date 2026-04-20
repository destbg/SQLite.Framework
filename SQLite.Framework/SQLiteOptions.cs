using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework;

/// <summary>
/// Read-only configuration for a <see cref="SQLiteDatabase" />. Build an instance via
/// <see cref="SQLiteOptionsBuilder" /> and pass it to the <see cref="SQLiteDatabase" /> constructor.
/// </summary>
public sealed class SQLiteOptions
{
    private readonly ConcurrentDictionary<Type, Type?> interfaceToConverterTypeCache = [];

    internal SQLiteOptions()
    {
    }

    /// <summary>
    /// The path to the SQLite database file.
    /// </summary>
    public required string DatabasePath { get; init; }

    /// <summary>
    /// The flags used when opening the SQLite connection.
    /// </summary>
    public required SQLiteOpenFlags OpenFlags { get; init; }

    /// <summary>
    /// When <see langword="true" />, the database operates in WAL (Write-Ahead Logging) mode.
    /// Concurrent writes from independent async contexts proceed without serialization.
    /// A <c>PRAGMA journal_mode = WAL</c> statement is issued automatically when the connection is first opened.
    /// </summary>
    public required bool IsWalMode { get; init; }

    /// <summary>
    /// The encryption key for a SQLCipher database. <see langword="null" /> when no key is set.
    /// Only applied when the framework is compiled with <c>SQLITECIPHER</c>.
    /// </summary>
    public required string? EncryptionKey { get; init; }

    /// <summary>
    /// Controls how DateTime values are stored. Defaults to <see cref="DateTimeStorageMode.Integer" />.
    /// </summary>
    public required DateTimeStorageMode DateTimeStorage { get; init; }

    /// <summary>
    /// The format string used when <see cref="DateTimeStorage" /> is set to <see cref="DateTimeStorageMode.TextFormatted" />.
    /// Defaults to "yyyy-MM-dd HH:mm:ss".
    /// </summary>
    public required string DateTimeFormat { get; init; }

    /// <summary>
    /// Controls how DateTimeOffset values are stored. Defaults to <see cref="DateTimeOffsetStorageMode.Ticks" />.
    /// </summary>
    public required DateTimeOffsetStorageMode DateTimeOffsetStorage { get; init; }

    /// <summary>
    /// The format string used when <see cref="DateTimeOffsetStorage" /> is set to <see cref="DateTimeOffsetStorageMode.TextFormatted" />.
    /// Defaults to "yyyy-MM-dd HH:mm:ss zzz".
    /// </summary>
    public required string DateTimeOffsetFormat { get; init; }

    /// <summary>
    /// Controls how TimeSpan values are stored. Defaults to <see cref="TimeSpanStorageMode.Integer" />.
    /// </summary>
    public required TimeSpanStorageMode TimeSpanStorage { get; init; }

    /// <summary>
    /// The format string used when <see cref="TimeSpanStorage" /> is set to <see cref="TimeSpanStorageMode.Text" />.
    /// Defaults to "c" (constant/invariant format, e.g. "2.03:04:05.0060070").
    /// </summary>
    public required string TimeSpanFormat { get; init; }

    /// <summary>
    /// Controls how DateOnly values are stored. Defaults to <see cref="DateOnlyStorageMode.Integer" />.
    /// </summary>
    public required DateOnlyStorageMode DateOnlyStorage { get; init; }

    /// <summary>
    /// The format string used when <see cref="DateOnlyStorage" /> is set to <see cref="DateOnlyStorageMode.Text" />.
    /// Defaults to "yyyy-MM-dd".
    /// </summary>
    public required string DateOnlyFormat { get; init; }

    /// <summary>
    /// Controls how TimeOnly values are stored. Defaults to <see cref="TimeOnlyStorageMode.Integer" />.
    /// </summary>
    public required TimeOnlyStorageMode TimeOnlyStorage { get; init; }

    /// <summary>
    /// The format string used when <see cref="TimeOnlyStorage" /> is set to <see cref="TimeOnlyStorageMode.Text" />.
    /// Defaults to "HH:mm:ss".
    /// </summary>
    public required string TimeOnlyFormat { get; init; }

    /// <summary>
    /// Controls how decimal values are stored. Defaults to <see cref="DecimalStorageMode.Real" />.
    /// </summary>
    public required DecimalStorageMode DecimalStorage { get; init; }

    /// <summary>
    /// The format string used when <see cref="DecimalStorage" /> is set to <see cref="DecimalStorageMode.Text" />.
    /// Defaults to "G" (general format).
    /// </summary>
    public required string DecimalFormat { get; init; }

    /// <summary>
    /// Controls how enum values are stored. Defaults to <see cref="EnumStorageMode.Integer" />.
    /// </summary>
    public required EnumStorageMode EnumStorage { get; init; }

    /// <summary>
    /// Custom type converters that define how specific .NET types are stored in and read from SQLite.
    /// </summary>
    public required IReadOnlyDictionary<Type, ISQLiteTypeConverter> TypeConverters { get; init; }

    /// <summary>
    /// Custom method translators that convert specific .NET method calls into SQL fragments.
    /// </summary>
    public required IReadOnlyDictionary<MethodInfo, SQLiteMethodTranslator> MethodTranslators { get; init; }

    /// <summary>
    /// Custom method translators for methods that take a predicate lambda as an argument.
    /// </summary>
    public required IReadOnlyDictionary<MethodInfo, SQLitePredicateMethodTranslator> PredicateMethodTranslators { get; init; }

    /// <summary>
    /// Translates property access on custom types into SQL fragments.
    /// </summary>
    public required IReadOnlyList<SQLitePropertyTranslator> PropertyTranslators { get; init; }

    /// <summary>
    /// Interceptors that can handle method calls before the default dispatch logic.
    /// </summary>
    public required IReadOnlyList<Func<MethodCallExpression, ISQLExpressionVisitor, Expression?>> MethodCallInterceptors { get; init; }

    internal Type? GetConverterTypeForInterface(Type interfaceType)
    {
        if (interfaceToConverterTypeCache.TryGetValue(interfaceType, out Type? cached))
        {
            return cached;
        }

        Type? targetElem = CommonHelpers.GetEnumerableElementType(interfaceType);
        Type? result = null;

        if (targetElem != null)
        {
            foreach (KeyValuePair<Type, ISQLiteTypeConverter> kvp in TypeConverters)
            {
                if (CommonHelpers.GetEnumerableElementType(kvp.Key) == targetElem)
                {
                    result = kvp.Key;
                    break;
                }
            }
        }

        interfaceToConverterTypeCache[interfaceType] = result;
        return result;
    }
}