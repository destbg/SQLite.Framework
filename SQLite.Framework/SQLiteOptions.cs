using System.Collections.Concurrent;

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
    /// When <see langword="true" /> (the default), the framework runs
    /// <c>PRAGMA foreign_keys = ON</c> on every connection open. Use
    /// <see cref="SQLiteOptionsBuilder.UseForeignKeys" /> to turn it off.
    /// </summary>
    public required bool IsForeignKeysEnabled { get; init; }

    /// <summary>
    /// The encryption key for a SQLCipher database. <see langword="null" /> when no key is set.
    /// Only applied when the framework is compiled with <c>SQLITECIPHER</c>.
    /// </summary>
    public required string? EncryptionKey { get; init; }

    /// <summary>
    /// The minimum SQLite version the application is willing to commit to. Defaults to
    /// <see cref="SQLiteMinimumVersion.Unspecified" />, which disables enforcement. When set
    /// to a non-default value, <see cref="SQLiteDatabase" /> verifies that the loaded SQLite is
    /// at or above this floor when the connection is first opened, and the framework rejects
    /// SQL translations that need a newer SQLite version than this floor.
    /// </summary>
    public required SQLiteMinimumVersion MinimumSqliteVersion { get; init; }

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
    /// Controls how char values are stored. Defaults to <see cref="CharStorageMode.Text" />.
    /// </summary>
    public required CharStorageMode CharStorage { get; init; }

    /// <summary>
    /// When set, <c>string.Contains</c>, <c>string.StartsWith</c>, and <c>string.EndsWith</c>
    /// translate to case-sensitive SQL (<c>instr</c> / <c>substr</c>) instead of the
    /// case-insensitive <c>LIKE</c>. This matches .NET in-memory LINQ and the EF Core SQLite
    /// provider. The <c>StringComparison.OrdinalIgnoreCase</c> overloads stay case-insensitive.
    /// Defaults to <see langword="false" />.
    /// </summary>
    public bool CaseSensitiveStringComparison { get; init; }

    /// <summary>
    /// Custom type converters that define how specific .NET types are stored in and read from SQLite.
    /// </summary>
    public required IReadOnlyDictionary<Type, ISQLiteTypeConverter> TypeConverters { get; init; }

    /// <summary>
    /// Custom member translators that convert specific .NET member calls into SQL fragments.
    /// </summary>
    public required IReadOnlyDictionary<MemberInfo, SQLiteMemberTranslator> MemberTranslators { get; init; }

    /// <summary>
    /// Translates property access on custom types into SQL fragments.
    /// </summary>
    public required IReadOnlyList<SQLitePropertyTranslator> PropertyTranslators { get; init; }

    /// <summary>
    /// Generated entity materializer builders, keyed by the entity's CLR type. Each value is a
    /// factory that the framework calls <em>once per query</em> with the current
    /// <see cref="SQLiteQueryContext" />. The factory resolves column indices from
    /// <see cref="SQLiteQueryContext.Columns" /> and returns a per-row materializer that closes
    /// over those indices. This pattern moves the column-name lookup out of the row loop so the
    /// generated code can use positional reads. Populated by the
    /// <c>UseGeneratedMaterializers</c> extension emitted by <c>SQLite.Framework.SourceGenerator</c>.
    /// </summary>
    public required IReadOnlyDictionary<Type, Func<SQLiteQueryContext, Func<SQLiteQueryContext, object?>>> EntityMaterializers { get; init; }

    /// <summary>
    /// Generated Select projection materializers, keyed by a canonical signature derived from the
    /// selector lambda's body. Populated by the <c>UseGeneratedMaterializers</c> extension emitted
    /// by <c>SQLite.Framework.SourceGenerator</c>.
    /// </summary>
    public required IReadOnlyDictionary<string, Func<SQLiteQueryContext, object?>> SelectMaterializers { get; init; }

    /// <summary>
    /// Generated GroupBy key-selector extractors, keyed by a canonical signature derived from the
    /// key selector lambda's body. Each entry reads <see cref="SQLiteQueryContext.Input" /> (the
    /// already-materialized row) and returns the group key. Populated by the
    /// <c>UseGeneratedMaterializers</c> extension emitted by <c>SQLite.Framework.SourceGenerator</c>.
    /// </summary>
    public required IReadOnlyDictionary<string, Func<SQLiteQueryContext, object?>> GroupByKeyMaterializers { get; init; }

    /// <summary>
    /// Generated typed grouping executors, keyed by the closed <see cref="IGrouping{TKey,TElement}" />
    /// type. Each entry runs a <c>GroupBy(keySelector)</c> query and groups the rows without
    /// <c>MakeGenericMethod</c>, so materializing an <c>IGrouping&lt;,&gt;</c> works under Native AOT.
    /// Populated by the <c>UseGeneratedMaterializers</c> extension emitted by
    /// <c>SQLite.Framework.SourceGenerator</c>.
    /// </summary>
    public required IReadOnlyDictionary<Type, Func<SQLiteDatabase, Expression, object>> GroupingQueryMaterializers { get; init; }

    /// <summary>
    /// Generated entity column writers, keyed by the entity's CLR type. The inner dictionary maps a
    /// property name to a delegate that binds that column on a prepared statement, replacing the
    /// reflection-based <see cref="PropertyInfo.GetValue(object?)" /> path used by <c>AddRange</c>,
    /// <c>UpdateRange</c>, <c>RemoveRange</c>, <c>AddOrUpdateRange</c>, and <c>UpsertRange</c>.
    /// Populated by the <c>UseGeneratedMaterializers</c> extension emitted by <c>SQLite.Framework.SourceGenerator</c>.
    /// </summary>
    public required IReadOnlyDictionary<Type, IReadOnlyDictionary<string, SQLiteEntityColumnWriter>> EntityWriters { get; init; }

    /// <summary>
    /// When <see langword="true" />, any entity or <c>Select</c> projection that would fall back
    /// to the runtime reflection path throws an <see cref="InvalidOperationException" /> instead.
    /// Use this together with <c>UseGeneratedMaterializers</c> to guarantee that every query in
    /// production goes through the source generator.
    /// </summary>
    public required bool ReflectionFallbackDisabled { get; init; }

    /// <summary>
    /// When <see langword="true" />, the <c>Add</c> family of methods uses the value already set on
    /// an <c>[AutoIncrement]</c> primary key when that value is not the type default. The row is
    /// inserted at that key, which fails with a uniqueness error if the key is already taken.
    /// When the value is the type default, SQLite assigns a new key and writes it back to the
    /// entity, the same as before. Defaults to <see langword="false" />, in which case any value
    /// you set on an <c>[AutoIncrement]</c> primary key is overwritten by the generated key.
    /// Set this to <see langword="true" /> to match Entity Framework Core's <c>Add</c> behavior.
    /// </summary>
    public required bool ExplicitAutoIncrementKeysPreserved { get; init; }

    /// <summary>
    /// When <see langword="true" />, a read from a different async context waits for the active
    /// transaction to commit or roll back before it runs. Reads from the transaction's own
    /// context, or from any context that holds the connection lock, do not wait. Use this when
    /// a separate-connection transaction is running and you do not want other code to read data
    /// that may be rolled back. Defaults to <see langword="false" />.
    /// </summary>
    public required bool BlockReadsDuringTransaction { get; init; }

    /// <summary>
    /// Per-entity hooks that fire before <c>Add</c>. Each delegate is a
    /// <c>Func&lt;SQLiteDatabase, T, bool&gt;</c>. Returning <see langword="false" /> skips the
    /// default INSERT and any later hooks. Populated through
    /// <see cref="SQLiteOptionsBuilder.OnAdd{T}(Action{T})" /> and its overloads.
    /// </summary>
    public required IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> AddHooks { get; init; }

    /// <summary>
    /// Per-entity hooks that fire before <c>Update</c>. Returning <see langword="false" /> skips
    /// the default UPDATE.
    /// </summary>
    public required IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> UpdateHooks { get; init; }

    /// <summary>
    /// Per-entity hooks that fire before <c>Remove</c>. Returning <see langword="false" /> skips
    /// the default DELETE. Useful for soft delete: flip a flag and call <c>Update</c> from inside
    /// the hook, then return <see langword="false" />.
    /// </summary>
    public required IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> RemoveHooks { get; init; }

    /// <summary>
    /// Per-entity hooks that fire before <c>AddOrUpdate</c> and <c>Upsert</c>. Returning
    /// <see langword="false" /> skips the default operation.
    /// </summary>
    public required IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> AddOrUpdateHooks { get; init; }

    /// <summary>
    /// Cross-cutting action hooks that run before every CRUD action, in registration order.
    /// Each hook receives the action returned by the previous hook and may mutate the entity
    /// or rewrite the action. The framework runs the action returned by the last hook.
    /// </summary>
    public required IReadOnlyList<SQLiteActionHook> OnActionHooks { get; init; }

    /// <summary>
    /// Predicates that the framework injects into every <c>Table&lt;E&gt;()</c> reference
    /// at query-translation time. The dictionary key is the registered type (either the entity
    /// type or an interface). A filter applies to every entity type assignable to that key,
    /// so a single registration against an interface like <c>ISoftDelete</c> covers every
    /// entity that implements it. Multiple filters per key are AND-combined.
    /// </summary>
    public required IReadOnlyDictionary<Type, IReadOnlyList<LambdaExpression>> QueryFilters { get; init; }

    /// <summary>
    /// Builds the <see cref="SQLitePragmas" /> instance returned by <see cref="SQLiteDatabase.Pragmas" />.
    /// The default builds the built-in class. To add more pragmas, register a class
    /// that inherits from <see cref="SQLitePragmas" /> through <see cref="SQLiteOptionsBuilder.UsePragmas" />.
    /// </summary>
    public required Func<SQLiteDatabase, SQLitePragmas> PragmasFactory { get; init; }

    /// <summary>
    /// Hooks called for every <see cref="SQLiteCommand" /> the framework runs.
    /// Populated through <see cref="SQLiteOptionsBuilder.AddCommandInterceptor" /> and
    /// <see cref="SQLiteOptionsBuilder.LogCommands" />.
    /// </summary>
    public required IReadOnlyList<ISQLiteCommandInterceptor> CommandInterceptors { get; init; }

    /// <summary>
    /// When <see langword="true" />, the built-in interceptor registered through
    /// <see cref="SQLiteOptionsBuilder.LogCommands" /> inlines parameter values in its
    /// formatted output. Off by default so logs are safe to ship to production. Set this
    /// through <see cref="SQLiteOptionsBuilder.EnableSensitiveParameterLogging" />.
    /// </summary>
    public required bool SensitiveParameterLoggingEnabled { get; init; }

    /// <summary>
    /// Builds the <see cref="SQLiteSchema" /> instance returned by <see cref="SQLiteDatabase.Schema" />.
    /// The default builds the built-in class. To customize how DDL is generated (for example,
    /// to override FTS5 trigger SQL), register a subclass of <see cref="SQLiteSchema" /> through
    /// <see cref="SQLiteOptionsBuilder.UseSchema" />.
    /// </summary>
    public required Func<SQLiteDatabase, SQLiteSchema> SchemaFactory { get; init; }

    /// <summary>
    /// Throws <see cref="NotSupportedException" /> when <paramref name="requiredVersion" /> is
    /// greater than the configured <see cref="MinimumSqliteVersion" />. Always succeeds when
    /// the floor is <see cref="SQLiteMinimumVersion.Unspecified" />. Used by built-in SQL
    /// emitters to fail fast when a query, DDL fragment, or pragma needs a newer SQLite than
    /// the caller has committed to.
    /// </summary>
    public void EnsureMinimumVersion(SQLiteMinimumVersion requiredVersion, string featureName)
    {
        if (MinimumSqliteVersion == SQLiteMinimumVersion.Unspecified)
        {
            return;
        }

        if ((int)requiredVersion > (int)MinimumSqliteVersion)
        {
            ThrowMinimumVersionNotSupported(requiredVersion, featureName);
        }
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="requiredVersion" /> is
    /// less than or equal to the configured <see cref="MinimumSqliteVersion" />. Always returns <see langword="true" /> when
    /// the floor is <see cref="SQLiteMinimumVersion.Unspecified" />. Used by built-in SQL
    /// emitters to check if a query, DDL fragment, or pragma can run with the current SQLite version.
    /// </summary>
    public bool OverMinimumVersion(SQLiteMinimumVersion requiredVersion)
    {
        if (MinimumSqliteVersion == SQLiteMinimumVersion.Unspecified)
        {
            return true;
        }

        return (int)requiredVersion <= (int)MinimumSqliteVersion;
    }

    /// <summary>
    /// Binds <paramref name="value" /> to <paramref name="parameterIndex" /> on
    /// <paramref name="statement" />, applying the storage modes and registered
    /// <see cref="ISQLiteTypeConverter" />s on these options. Public so generated
    /// <see cref="EntityWriters" /> code can fall back to the runtime path for column types
    /// it cannot bind directly.
    /// </summary>
    public void BindParameter(sqlite3_stmt statement, int parameterIndex, object? value)
    {
        CommandHelpers.BindParameterByIndex(statement, parameterIndex, value, this);
    }

    /// <summary>
    /// Returns true when a custom type converter is registered for <paramref name="type" />.
    /// Generated <see cref="EntityWriters" /> code calls this to decide whether to bind a column
    /// through the converter instead of a fast direct bind, matching the reflection path.
    /// </summary>
    public bool HasConverter(Type type)
    {
        return TypeConverters.Count != 0 && TypeConverters.ContainsKey(type);
    }

    internal void ThrowMinimumVersionNotSupported(SQLiteMinimumVersion requiredVersion, string featureName)
    {
        throw new NotSupportedException(
            $"{featureName} requires SQLite {SQLiteVersionFormatter.Format((int)requiredVersion)} or later. " +
            $"The configured minimum is {SQLiteVersionFormatter.Format((int)MinimumSqliteVersion)}. " +
            $"Raise the value passed to UseMinimumSqliteVersion or remove the call that needs the newer feature."
        );
    }

    internal Type? GetConverterTypeForInterface(Type interfaceType)
    {
        if (interfaceToConverterTypeCache.TryGetValue(interfaceType, out Type? cached))
        {
            return cached;
        }

        Type? targetElem = TypeHelpers.GetEnumerableElementType(interfaceType);
        Type? result = null;

        if (targetElem != null)
        {
            foreach (KeyValuePair<Type, ISQLiteTypeConverter> kvp in TypeConverters)
            {
                if (TypeHelpers.GetEnumerableElementType(kvp.Key) == targetElem)
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
