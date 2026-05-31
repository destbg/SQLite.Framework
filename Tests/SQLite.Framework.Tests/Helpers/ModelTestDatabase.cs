using System.Runtime.CompilerServices;

namespace SQLite.Framework.Tests.Helpers;

/// <summary>
/// A test database that applies a supplied model configuration in
/// <see cref="SQLiteDatabase.OnModelCreating" />. Use it to declare indexes, checks, computed
/// columns, defaults, foreign keys, and the like for a single test without writing a dedicated
/// subclass.
/// </summary>
public sealed class ModelTestDatabase : TestDatabase
{
    private readonly Action<SQLiteModelBuilder> configure;

    public ModelTestDatabase(Action<SQLiteModelBuilder> configure, [CallerMemberName] string? methodName = null)
        : base(methodName)
    {
        this.configure = configure;
    }

    public ModelTestDatabase(Action<SQLiteModelBuilder> configure, Action<SQLiteOptionsBuilder> configureOptions, [CallerMemberName] string? methodName = null)
        : base(configureOptions, methodName)
    {
        this.configure = configure;
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        configure(builder);
    }
}
