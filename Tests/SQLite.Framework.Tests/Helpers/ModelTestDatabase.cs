using System.Runtime.CompilerServices;

namespace SQLite.Framework.Tests.Helpers;

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
