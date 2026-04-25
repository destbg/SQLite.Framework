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
    /// Per-entity hooks that fire before <c>Add</c>. Mutate the entity here for things like an
    /// audit timestamp.
    /// </summary>
    public Dictionary<Type, List<Delegate>> AddHooks { get; } = [];

    /// <summary>
    /// Per-entity hooks that fire before <c>Update</c>.
    /// </summary>
    public Dictionary<Type, List<Delegate>> UpdateHooks { get; } = [];

    /// <summary>
    /// Per-entity hooks that fire before <c>Remove</c>. Useful for soft delete: flip a flag and
    /// call <c>Update</c> from inside the hook, then return <see langword="false" /> to skip the
    /// default DELETE.
    /// </summary>
    public Dictionary<Type, List<Delegate>> RemoveHooks { get; } = [];

    /// <summary>
    /// Per-entity hooks that fire before <c>AddOrUpdate</c> and <c>Upsert</c>.
    /// </summary>
    public Dictionary<Type, List<Delegate>> AddOrUpdateHooks { get; } = [];

    /// <summary>
    /// Cross-cutting action hooks that run before every CRUD action across every entity.
    /// Populated through <see cref="OnAction" />.
    /// </summary>
    public List<SQLiteActionHook> OnActionHooks { get; } = [];

    /// <summary>
    /// Query filters keyed by registration type. Populated through <see cref="AddQueryFilter{T}" />.
    /// Each filter is a typed lambda; the registration type can be the entity type or an interface,
    /// in which case the filter applies to every entity assignable to that interface.
    /// </summary>
    public Dictionary<Type, List<LambdaExpression>> QueryFilters { get; } = [];

    /// <summary>
    /// Builds the <see cref="SQLitePragmas" /> instance the first time <see cref="SQLiteDatabase.Pragmas" /> is read.
    /// The default builds the built-in <see cref="SQLitePragmas" /> class.
    /// </summary>
    public Func<SQLiteDatabase, SQLitePragmas> PragmasFactory { get; set; } = static db => new SQLitePragmas(db);

    /// <summary>
    /// Builds the <see cref="SQLiteSchema" /> instance the first time <see cref="SQLiteDatabase.Schema" /> is read.
    /// The default builds the built-in <see cref="SQLiteSchema" /> class.
    /// </summary>
    public Func<SQLiteDatabase, SQLiteSchema> SchemaFactory { get; set; } = static db => new SQLiteSchema(db);

    /// <summary>
    /// Registers a predicate the framework injects into every query against <typeparamref name="T" />,
    /// or every query against any entity that implements <typeparamref name="T" /> when it is an
    /// interface. The framework rewrites the filter's parameter from <typeparamref name="T" /> to
    /// the concrete entity type when it injects the filter, so the same registration covers every
    /// matching entity. Multiple filters per type are AND-combined. Use
    /// <c>IQueryable&lt;T&gt;.IgnoreQueryFilters()</c> on a per-query basis to opt out.
    /// </summary>
    public SQLiteOptionsBuilder AddQueryFilter<T>(Expression<Func<T, bool>> predicate)
    {
        if (!QueryFilters.TryGetValue(typeof(T), out List<LambdaExpression>? list))
        {
            list = [];
            QueryFilters[typeof(T)] = list;
        }
        list.Add(predicate);
        return this;
    }

    /// <summary>
    /// Registers a cross-cutting hook that runs before every CRUD action across every entity.
    /// The hook can mutate the entity and rewrite the action (for example, turn
    /// <see cref="SQLiteAction.Remove" /> into <see cref="SQLiteAction.Update" /> for a
    /// soft-delete scenario). Multiple hooks chain in registration order; each receives the
    /// action returned by the previous hook.
    /// </summary>
    public SQLiteOptionsBuilder OnAction(SQLiteActionHook hook)
    {
        OnActionHooks.Add(hook);
        return this;
    }

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
    /// Registers an action that runs before every <c>Add</c> for <typeparamref name="T" />.
    /// The action can mutate the entity. The default INSERT always runs after.
    /// </summary>
    public SQLiteOptionsBuilder OnAdd<T>(Action<T> hook)
    {
        return OnAdd<T>((_, item) =>
        {
            hook(item);
            return true;
        });
    }

    /// <summary>
    /// Registers a function that runs before every <c>Add</c> for <typeparamref name="T" />.
    /// Return <see langword="false" /> to skip the default INSERT (and any later hooks).
    /// </summary>
    public SQLiteOptionsBuilder OnAdd<T>(Func<SQLiteDatabase, T, bool> hook)
    {
        AppendHook(AddHooks, typeof(T), hook);
        return this;
    }

    /// <summary>
    /// Registers an action that runs before every <c>Update</c> for <typeparamref name="T" />.
    /// </summary>
    public SQLiteOptionsBuilder OnUpdate<T>(Action<T> hook)
    {
        return OnUpdate<T>((_, item) =>
        {
            hook(item);
            return true;
        });
    }

    /// <summary>
    /// Registers a function that runs before every <c>Update</c> for <typeparamref name="T" />.
    /// Return <see langword="false" /> to skip the default UPDATE.
    /// </summary>
    public SQLiteOptionsBuilder OnUpdate<T>(Func<SQLiteDatabase, T, bool> hook)
    {
        AppendHook(UpdateHooks, typeof(T), hook);
        return this;
    }

    /// <summary>
    /// Registers an action that runs before every <c>Remove</c> for <typeparamref name="T" />.
    /// </summary>
    public SQLiteOptionsBuilder OnRemove<T>(Action<T> hook)
    {
        return OnRemove<T>((_, item) =>
        {
            hook(item);
            return true;
        });
    }

    /// <summary>
    /// Registers a function that runs before every <c>Remove</c> for <typeparamref name="T" />.
    /// Return <see langword="false" /> to skip the default DELETE. Combine with an
    /// <c>Update</c> call inside the hook to implement soft delete.
    /// </summary>
    public SQLiteOptionsBuilder OnRemove<T>(Func<SQLiteDatabase, T, bool> hook)
    {
        AppendHook(RemoveHooks, typeof(T), hook);
        return this;
    }

    /// <summary>
    /// Registers an action that runs before every <c>AddOrUpdate</c> and <c>Upsert</c> for
    /// <typeparamref name="T" />.
    /// </summary>
    public SQLiteOptionsBuilder OnAddOrUpdate<T>(Action<T> hook)
    {
        return OnAddOrUpdate<T>((_, item) =>
        {
            hook(item);
            return true;
        });
    }

    /// <summary>
    /// Registers a function that runs before every <c>AddOrUpdate</c> and <c>Upsert</c> for
    /// <typeparamref name="T" />. Return <see langword="false" /> to skip the default operation.
    /// </summary>
    public SQLiteOptionsBuilder OnAddOrUpdate<T>(Func<SQLiteDatabase, T, bool> hook)
    {
        AppendHook(AddOrUpdateHooks, typeof(T), hook);
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
    /// Sets a custom factory for <see cref="SQLiteDatabase.Pragmas" />. Use this to add more pragmas
    /// by passing a class that inherits from <see cref="SQLitePragmas" />.
    /// </summary>
    public SQLiteOptionsBuilder UsePragmas(Func<SQLiteDatabase, SQLitePragmas> factory)
    {
        PragmasFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    /// <summary>
    /// Sets a custom factory for <see cref="SQLiteDatabase.Schema" />. Use this to plug in a
    /// subclass of <see cref="SQLiteSchema" /> that overrides DDL generation, for example FTS5
    /// trigger SQL.
    /// </summary>
    public SQLiteOptionsBuilder UseSchema(Func<SQLiteDatabase, SQLiteSchema> factory)
    {
        SchemaFactory = factory ?? throw new ArgumentNullException(nameof(factory));
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
            AddHooks = SnapshotHooks(AddHooks),
            UpdateHooks = SnapshotHooks(UpdateHooks),
            RemoveHooks = SnapshotHooks(RemoveHooks),
            AddOrUpdateHooks = SnapshotHooks(AddOrUpdateHooks),
            OnActionHooks = [.. OnActionHooks],
            QueryFilters = SnapshotQueryFilters(QueryFilters),
            PragmasFactory = PragmasFactory,
            SchemaFactory = SchemaFactory,
        };
    }

    private static Dictionary<Type, IReadOnlyList<LambdaExpression>> SnapshotQueryFilters(Dictionary<Type, List<LambdaExpression>> source)
    {
        Dictionary<Type, IReadOnlyList<LambdaExpression>> snapshot = new(source.Count);
        foreach (KeyValuePair<Type, List<LambdaExpression>> kvp in source)
        {
            snapshot[kvp.Key] = [.. kvp.Value];
        }
        return snapshot;
    }

    private static void AppendHook(Dictionary<Type, List<Delegate>> store, Type entityType, Delegate hook)
    {
        if (!store.TryGetValue(entityType, out List<Delegate>? list))
        {
            list = [];
            store[entityType] = list;
        }
        list.Add(hook);
    }

    private static Dictionary<Type, IReadOnlyList<Delegate>> SnapshotHooks(Dictionary<Type, List<Delegate>> source)
    {
        Dictionary<Type, IReadOnlyList<Delegate>> snapshot = new(source.Count);
        foreach (KeyValuePair<Type, List<Delegate>> kvp in source)
        {
            snapshot[kvp.Key] = [.. kvp.Value];
        }
        return snapshot;
    }
}