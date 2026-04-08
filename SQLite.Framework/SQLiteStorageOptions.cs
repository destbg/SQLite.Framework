using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework;

/// <summary>
/// Controls how specific .NET types are stored in and read from the database.
/// Using the default values allows for higher control over what is translated into SQLite.
/// </summary>
public class SQLiteStorageOptions
{
    private readonly ConcurrentDictionary<Type, Type?> interfaceToConverterTypeCache = [];

    /// <summary>
    /// Controls how DateTime values are stored. Defaults to <see cref="DateTimeStorageMode.Integer" />.
    /// </summary>
    public DateTimeStorageMode DateTimeStorage { get; set; } = DateTimeStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="DateTimeStorage" /> is set to <see cref="DateTimeStorageMode.TextFormatted" />.
    /// Defaults to "yyyy-MM-dd HH:mm:ss".
    /// </summary>
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Controls how DateTimeOffset values are stored. Defaults to <see cref="DateTimeOffsetStorageMode.Ticks" />.
    /// </summary>
    public DateTimeOffsetStorageMode DateTimeOffsetStorage { get; set; } = DateTimeOffsetStorageMode.Ticks;

    /// <summary>
    /// The format string used when <see cref="DateTimeOffsetStorage" /> is set to <see cref="DateTimeOffsetStorageMode.TextFormatted" />.
    /// Defaults to "yyyy-MM-dd HH:mm:ss zzz".
    /// </summary>
    public string DateTimeOffsetFormat { get; set; } = "yyyy-MM-dd HH:mm:ss zzz";

    /// <summary>
    /// Controls how TimeSpan values are stored. Defaults to <see cref="TimeSpanStorageMode.Integer" />.
    /// </summary>
    public TimeSpanStorageMode TimeSpanStorage { get; set; } = TimeSpanStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="TimeSpanStorage" /> is set to <see cref="TimeSpanStorageMode.Text" />.
    /// Defaults to "c" (constant/invariant format, e.g. "2.03:04:05.0060070").
    /// </summary>
    public string TimeSpanFormat { get; set; } = "c";

    /// <summary>
    /// Controls how DateOnly values are stored. Defaults to <see cref="DateOnlyStorageMode.Integer" />.
    /// </summary>
    public DateOnlyStorageMode DateOnlyStorage { get; set; } = DateOnlyStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="DateOnlyStorage" /> is set to <see cref="DateOnlyStorageMode.Text" />.
    /// Defaults to "yyyy-MM-dd".
    /// </summary>
    public string DateOnlyFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Controls how TimeOnly values are stored. Defaults to <see cref="TimeOnlyStorageMode.Integer" />.
    /// </summary>
    public TimeOnlyStorageMode TimeOnlyStorage { get; set; } = TimeOnlyStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="TimeOnlyStorage" /> is set to <see cref="TimeOnlyStorageMode.Text" />.
    /// Defaults to "HH:mm:ss".
    /// </summary>
    public string TimeOnlyFormat { get; set; } = "HH:mm:ss";

    /// <summary>
    /// Controls how decimal values are stored. Defaults to <see cref="DecimalStorageMode.Real" />.
    /// </summary>
    public DecimalStorageMode DecimalStorage { get; set; } = DecimalStorageMode.Real;

    /// <summary>
    /// The format string used when <see cref="DecimalStorage" /> is set to <see cref="DecimalStorageMode.Text" />.
    /// Defaults to "G" (general format).
    /// </summary>
    public string DecimalFormat { get; set; } = "G";

    /// <summary>
    /// Controls how enum values are stored. Defaults to <see cref="EnumStorageMode.Integer" />.
    /// </summary>
    public EnumStorageMode EnumStorage { get; set; } = EnumStorageMode.Integer;

    /// <summary>
    /// Custom type converters that define how specific .NET types are stored in and read from SQLite.
    /// The key is the .NET type; the value is the converter implementation.
    /// </summary>
    public Dictionary<Type, ISQLiteTypeConverter> TypeConverters { get; } = [];

    /// <summary>
    /// Custom method translators that convert specific .NET method calls into SQL fragments.
    /// The key is the <see cref="MethodInfo" /> of the method to translate; the value is the translator delegate.
    /// </summary>
    public Dictionary<MethodInfo, SQLiteMethodTranslator> MethodTranslators { get; } = [];

    /// <summary>
    /// Custom method translators for methods that take a predicate lambda as an argument.
    /// The lambda parameter is automatically bound to <c>value</c> from a <c>json_each</c> subquery.
    /// The key is the <see cref="MethodInfo" /> of the method to translate; the value is the translator delegate.
    /// </summary>
    public Dictionary<MethodInfo, SQLitePredicateMethodTranslator> PredicateMethodTranslators { get; } = [];

    /// <summary>
    /// Translates property access on custom types into SQL fragments.
    /// Each translator returns a SQL fragment or <c>null</c> if it does not handle the given member.
    /// Translators are tried in order until one returns a non-null result.
    /// </summary>
    public List<SQLitePropertyTranslator> PropertyTranslators { get; } = [];

    /// <summary>
    /// Interceptors that can handle method calls before the default dispatch logic.
    /// Each interceptor receives the method call expression and a visitor, and returns a translated expression or null to pass.
    /// Interceptors are tried in order until one returns a non-null result.
    /// </summary>
    public List<Func<MethodCallExpression, ISQLExpressionVisitor, Expression?>> MethodCallInterceptors { get; } = [];

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
