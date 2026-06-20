namespace SQLite.Framework.Tests.DependencyInjection;

public sealed class PrimaryDatabase : SQLiteDatabase
{
    public PrimaryDatabase(SQLiteOptions options)
        : base(options)
    {
    }
}

public sealed class SecondaryDatabase : SQLiteDatabase
{
    public SecondaryDatabase(SQLiteOptions options)
        : base(options)
    {
    }
}

public sealed class DatabaseDependency
{
    public string Marker { get; } = "marker";
}

public sealed class DependentDatabase : SQLiteDatabase
{
    public DependentDatabase(SQLiteOptions options, DatabaseDependency dependency)
        : base(options)
    {
        Dependency = dependency;
    }

    public DatabaseDependency Dependency { get; }
}
