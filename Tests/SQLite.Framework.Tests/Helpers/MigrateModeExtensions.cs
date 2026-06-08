using System.Diagnostics.CodeAnalysis;
using SQLite.Framework;

namespace SQLite.Framework.Tests.Helpers;

public enum MigrateMode
{
    InPlace,
    Rebuild,
}

public static class MigrateModeExtensions
{
    public static int Migrate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema, MigrateMode mode)
    {
        return mode == MigrateMode.Rebuild ? schema.MigrateByRebuild() : schema.Migrate();
    }

    public static int Migrate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema, MigrateMode mode, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        return mode == MigrateMode.Rebuild ? schema.MigrateByRebuild(fill) : schema.Migrate(fill);
    }

    public static void AssertSchemaEquivalent(string? expected, string? actual)
    {
        Assert.Equal(StripWhitespace(expected), StripWhitespace(actual));
    }

    private static string? StripWhitespace(string? value)
    {
        return value == null ? null : new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }
}
