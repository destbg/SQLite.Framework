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
    /// Custom member translators that convert specific .NET member calls into SQL fragments.
    /// </summary>
    public required IReadOnlyDictionary<MemberInfo, SQLiteMemberTranslator> MemberTranslators { get; init; }

    /// <summary>
    /// Translates property access on custom types into SQL fragments.
    /// </summary>
    public required IReadOnlyList<SQLitePropertyTranslator> PropertyTranslators { get; init; }

    /// <summary>
    /// Generated entity materializers, keyed by the entity's CLR type. Populated by the
    /// <c>UseGeneratedMaterializers</c> extension emitted by <c>SQLite.Framework.SourceGenerator</c>.
    /// </summary>
    public required IReadOnlyDictionary<Type, Func<SQLiteQueryContext, object?>> EntityMaterializers { get; init; }

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
    /// When <see langword="true" />, any entity or <c>Select</c> projection that would fall back
    /// to the runtime reflection path throws an <see cref="InvalidOperationException" /> instead.
    /// Use this together with <c>UseGeneratedMaterializers</c> to guarantee that every query in
    /// production goes through the source generator.
    /// </summary>
    public bool ReflectionFallbackDisabled { get; init; }

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
    /// Builds the <see cref="SQLiteSchema" /> instance returned by <see cref="SQLiteDatabase.Schema" />.
    /// The default builds the built-in class. To customize how DDL is generated (for example,
    /// to override FTS5 trigger SQL), register a subclass of <see cref="SQLiteSchema" /> through
    /// <see cref="SQLiteOptionsBuilder.UseSchema" />.
    /// </summary>
    public required Func<SQLiteDatabase, SQLiteSchema> SchemaFactory { get; init; }

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