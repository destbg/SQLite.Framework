using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;

namespace SQLite.Framework;

/// <summary>
/// Fluent builder that configures an <see cref="SQLiteOptions" /> instance.
/// Mutate the properties or call the fluent <c>Use*</c>/<c>Add*</c> helpers, then call <see cref="Build" />
/// to produce a read-only options instance suitable for <see cref="SQLiteDatabase" />.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SQLiteOptionsBuilder
{
    /// <summary>
    /// Initializes a new builder for the database at the given path.
    /// </summary>
    public SQLiteOptionsBuilder(string databasePath)
    {
        DatabasePath = databasePath;
    }

    /// <summary>
    /// The path to the SQLite database file.
    /// </summary>
    public string DatabasePath { get; set; }

    /// <summary>
    /// The flags used when opening the SQLite connection.
    /// </summary>
    public SQLiteOpenFlags OpenFlags { get; set; } = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite;

    /// <summary>
    /// When <see langword="true" />, the database operates in WAL (Write-Ahead Logging) mode.
    /// </summary>
    public bool IsWalMode { get; set; }

    /// <summary>
    /// The encryption key for a SQLCipher database. Only applied when the framework is compiled with <c>SQLITECIPHER</c>.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Controls how DateTime values are stored. Defaults to <see cref="DateTimeStorageMode.Integer" />.
    /// </summary>
    public DateTimeStorageMode DateTimeStorage { get; set; } = DateTimeStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="DateTimeStorage" /> is set to <see cref="DateTimeStorageMode.TextFormatted" />.
    /// </summary>
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Controls how DateTimeOffset values are stored. Defaults to <see cref="DateTimeOffsetStorageMode.Ticks" />.
    /// </summary>
    public DateTimeOffsetStorageMode DateTimeOffsetStorage { get; set; } = DateTimeOffsetStorageMode.Ticks;

    /// <summary>
    /// The format string used when <see cref="DateTimeOffsetStorage" /> is set to <see cref="DateTimeOffsetStorageMode.TextFormatted" />.
    /// </summary>
    public string DateTimeOffsetFormat { get; set; } = "yyyy-MM-dd HH:mm:ss zzz";

    /// <summary>
    /// Controls how TimeSpan values are stored. Defaults to <see cref="TimeSpanStorageMode.Integer" />.
    /// </summary>
    public TimeSpanStorageMode TimeSpanStorage { get; set; } = TimeSpanStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="TimeSpanStorage" /> is set to <see cref="TimeSpanStorageMode.Text" />.
    /// </summary>
    public string TimeSpanFormat { get; set; } = "c";

    /// <summary>
    /// Controls how DateOnly values are stored. Defaults to <see cref="DateOnlyStorageMode.Integer" />.
    /// </summary>
    public DateOnlyStorageMode DateOnlyStorage { get; set; } = DateOnlyStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="DateOnlyStorage" /> is set to <see cref="DateOnlyStorageMode.Text" />.
    /// </summary>
    public string DateOnlyFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Controls how TimeOnly values are stored. Defaults to <see cref="TimeOnlyStorageMode.Integer" />.
    /// </summary>
    public TimeOnlyStorageMode TimeOnlyStorage { get; set; } = TimeOnlyStorageMode.Integer;

    /// <summary>
    /// The format string used when <see cref="TimeOnlyStorage" /> is set to <see cref="TimeOnlyStorageMode.Text" />.
    /// </summary>
    public string TimeOnlyFormat { get; set; } = "HH:mm:ss";

    /// <summary>
    /// Controls how decimal values are stored. Defaults to <see cref="DecimalStorageMode.Real" />.
    /// </summary>
    public DecimalStorageMode DecimalStorage { get; set; } = DecimalStorageMode.Real;

    /// <summary>
    /// The format string used when <see cref="DecimalStorage" /> is set to <see cref="DecimalStorageMode.Text" />.
    /// </summary>
    public string DecimalFormat { get; set; } = "G";

    /// <summary>
    /// Controls how enum values are stored. Defaults to <see cref="EnumStorageMode.Integer" />.
    /// </summary>
    public EnumStorageMode EnumStorage { get; set; } = EnumStorageMode.Integer;

    /// <summary>
    /// Custom type converters that define how specific .NET types are stored in and read from SQLite.
    /// </summary>
    public Dictionary<Type, ISQLiteTypeConverter> TypeConverters { get; } = [];

    /// <summary>
    /// Custom method translators that convert specific .NET method calls into SQL fragments.
    /// </summary>
    public Dictionary<MethodInfo, SQLiteMethodTranslator> MethodTranslators { get; } = [];

    /// <summary>
    /// Custom method translators for methods that take a predicate lambda as an argument.
    /// </summary>
    public Dictionary<MethodInfo, SQLitePredicateMethodTranslator> PredicateMethodTranslators { get; } = [];

    /// <summary>
    /// Translates property access on custom types into SQL fragments.
    /// </summary>
    public List<SQLitePropertyTranslator> PropertyTranslators { get; } = [];

    /// <summary>
    /// Interceptors that can handle method calls before the default dispatch logic.
    /// </summary>
    public List<Func<MethodCallExpression, ISQLExpressionVisitor, Expression?>> MethodCallInterceptors { get; } = [];

    /// <summary>
    /// Generated entity materializers, keyed by the entity's CLR type.
    /// Populated by the <c>UseGeneratedMaterializers</c> extension emitted by <c>SQLite.Framework.SourceGenerator</c>.
    /// User code should go through that extension rather than mutating this map directly.
    /// </summary>
    public Dictionary<Type, Func<SQLiteQueryContext, object?>> EntityMaterializers { get; } = [];

    /// <summary>
    /// Generated Select projection materializers, keyed by a canonical signature derived from the
    /// selector lambda's body. Populated by the <c>UseGeneratedMaterializers</c> extension emitted by
    /// <c>SQLite.Framework.SourceGenerator</c>.
    /// </summary>
    public Dictionary<string, Func<SQLiteQueryContext, object?>> SelectMaterializers { get; } = [];

    /// <summary>
    /// Generated GroupBy key-selector extractors, keyed by a canonical signature derived from the
    /// key selector lambda's body. Populated by the <c>UseGeneratedMaterializers</c> extension
    /// emitted by <c>SQLite.Framework.SourceGenerator</c>.
    /// </summary>
    public Dictionary<string, Func<SQLiteQueryContext, object?>> GroupByKeyMaterializers { get; } = [];

    /// <summary>
    /// When <see langword="true" />, any entity or <c>Select</c> projection that would fall back
    /// to the runtime reflection path throws an <see cref="InvalidOperationException" /> instead.
    /// Defaults to <see langword="false" />.
    /// </summary>
    public bool ReflectionFallbackDisabled { get; set; }

    /// <summary>
    /// Sets the flags used when opening the SQLite connection.
    /// </summary>
    public SQLiteOptionsBuilder UseOpenFlags(SQLiteOpenFlags flags)
    {
        OpenFlags = flags;
        return this;
    }

    /// <summary>
    /// Enables (or disables) WAL (Write-Ahead Logging) mode.
    /// </summary>
    public SQLiteOptionsBuilder UseWalMode(bool enabled = true)
    {
        IsWalMode = enabled;
        return this;
    }

    /// <summary>
    /// Sets the encryption key for a SQLCipher database.
    /// </summary>
    public SQLiteOptionsBuilder UseEncryptionKey(string key)
    {
        EncryptionKey = key;
        return this;
    }

    /// <summary>
    /// Sets the DateTime storage mode.
    /// </summary>
    public SQLiteOptionsBuilder UseDateTimeStorage(DateTimeStorageMode mode, string? format = null)
    {
        DateTimeStorage = mode;
        if (format != null)
        {
            DateTimeFormat = format;
        }

        return this;
    }

    /// <summary>
    /// Sets the DateTimeOffset storage mode.
    /// </summary>
    public SQLiteOptionsBuilder UseDateTimeOffsetStorage(DateTimeOffsetStorageMode mode, string? format = null)
    {
        DateTimeOffsetStorage = mode;
        if (format != null)
        {
            DateTimeOffsetFormat = format;
        }

        return this;
    }

    /// <summary>
    /// Sets the TimeSpan storage mode.
    /// </summary>
    public SQLiteOptionsBuilder UseTimeSpanStorage(TimeSpanStorageMode mode, string? format = null)
    {
        TimeSpanStorage = mode;
        if (format != null)
        {
            TimeSpanFormat = format;
        }

        return this;
    }

    /// <summary>
    /// Sets the DateOnly storage mode.
    /// </summary>
    public SQLiteOptionsBuilder UseDateOnlyStorage(DateOnlyStorageMode mode, string? format = null)
    {
        DateOnlyStorage = mode;
        if (format != null)
        {
            DateOnlyFormat = format;
        }

        return this;
    }

    /// <summary>
    /// Sets the TimeOnly storage mode.
    /// </summary>
    public SQLiteOptionsBuilder UseTimeOnlyStorage(TimeOnlyStorageMode mode, string? format = null)
    {
        TimeOnlyStorage = mode;
        if (format != null)
        {
            TimeOnlyFormat = format;
        }

        return this;
    }

    /// <summary>
    /// Sets the decimal storage mode.
    /// </summary>
    public SQLiteOptionsBuilder UseDecimalStorage(DecimalStorageMode mode, string? format = null)
    {
        DecimalStorage = mode;
        if (format != null)
        {
            DecimalFormat = format;
        }

        return this;
    }

    /// <summary>
    /// Sets the enum storage mode.
    /// </summary>
    public SQLiteOptionsBuilder UseEnumStorage(EnumStorageMode mode)
    {
        EnumStorage = mode;
        return this;
    }

    /// <summary>
    /// Registers a custom type converter for the given CLR type.
    /// </summary>
    public SQLiteOptionsBuilder AddTypeConverter(Type clrType, ISQLiteTypeConverter converter)
    {
        TypeConverters[clrType] = converter;
        return this;
    }

    /// <summary>
    /// Registers a custom type converter for <typeparamref name="T" />.
    /// </summary>
    public SQLiteOptionsBuilder AddTypeConverter<T>(ISQLiteTypeConverter converter)
    {
        TypeConverters[typeof(T)] = converter;
        return this;
    }

    /// <summary>
    /// Registers a SQL translator for a specific method.
    /// </summary>
    public SQLiteOptionsBuilder AddMethodTranslator(MethodInfo method, SQLiteMethodTranslator translator)
    {
        MethodTranslators[method] = translator;
        return this;
    }

    /// <summary>
    /// Registers a SQL translator for a method that takes a predicate lambda as an argument.
    /// </summary>
    public SQLiteOptionsBuilder AddPredicateMethodTranslator(MethodInfo method, SQLitePredicateMethodTranslator translator)
    {
        PredicateMethodTranslators[method] = translator;
        return this;
    }

    /// <summary>
    /// Appends a property access SQL translator.
    /// </summary>
    public SQLiteOptionsBuilder AddPropertyTranslator(SQLitePropertyTranslator translator)
    {
        PropertyTranslators.Add(translator);
        return this;
    }

    /// <summary>
    /// Appends a method call interceptor.
    /// </summary>
    public SQLiteOptionsBuilder AddMethodCallInterceptor(Func<MethodCallExpression, ISQLExpressionVisitor, Expression?> interceptor)
    {
        MethodCallInterceptors.Add(interceptor);
        return this;
    }

    /// <summary>
    /// Disables the runtime reflection fallback. Any entity or <c>Select</c> projection that is
    /// not covered by a generated materializer will throw an <see cref="InvalidOperationException" />.
    /// Call this together with <c>UseGeneratedMaterializers</c> to guarantee that every query in
    /// production is handled by the source generator.
    /// </summary>
    public SQLiteOptionsBuilder DisableReflectionFallback(bool disabled = true)
    {
        ReflectionFallbackDisabled = disabled;
        return this;
    }

    /// <summary>
    /// Produces a read-only <see cref="SQLiteOptions" /> snapshot of this builder's current state.
    /// Subsequent mutations on this builder do not affect the returned options instance.
    /// </summary>
    public SQLiteOptions Build()
    {
        return new SQLiteOptions
        {
            DatabasePath = DatabasePath,
            OpenFlags = OpenFlags,
            IsWalMode = IsWalMode,
            EncryptionKey = EncryptionKey,
            DateTimeStorage = DateTimeStorage,
            DateTimeFormat = DateTimeFormat,
            DateTimeOffsetStorage = DateTimeOffsetStorage,
            DateTimeOffsetFormat = DateTimeOffsetFormat,
            TimeSpanStorage = TimeSpanStorage,
            TimeSpanFormat = TimeSpanFormat,
            DateOnlyStorage = DateOnlyStorage,
            DateOnlyFormat = DateOnlyFormat,
            TimeOnlyStorage = TimeOnlyStorage,
            TimeOnlyFormat = TimeOnlyFormat,
            DecimalStorage = DecimalStorage,
            DecimalFormat = DecimalFormat,
            EnumStorage = EnumStorage,
            TypeConverters = new Dictionary<Type, ISQLiteTypeConverter>(TypeConverters),
            MethodTranslators = new Dictionary<MethodInfo, SQLiteMethodTranslator>(MethodTranslators),
            PredicateMethodTranslators = new Dictionary<MethodInfo, SQLitePredicateMethodTranslator>(PredicateMethodTranslators),
            PropertyTranslators = [.. PropertyTranslators],
            MethodCallInterceptors = [.. MethodCallInterceptors],
            EntityMaterializers = new Dictionary<Type, Func<SQLiteQueryContext, object?>>(EntityMaterializers),
            SelectMaterializers = new Dictionary<string, Func<SQLiteQueryContext, object?>>(SelectMaterializers),
            GroupByKeyMaterializers = new Dictionary<string, Func<SQLiteQueryContext, object?>>(GroupByKeyMaterializers),
            ReflectionFallbackDisabled = ReflectionFallbackDisabled,
        };
    }
}