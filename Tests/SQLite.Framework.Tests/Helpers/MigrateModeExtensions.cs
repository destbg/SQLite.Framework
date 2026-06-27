using System.Diagnostics.CodeAnalysis;
using SQLite.Framework;
using SQLite.Framework.Extensions;

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
        return Run<T>(schema.Database.Schema, mode == MigrateMode.Rebuild, null);
    }

    public static int Migrate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema, MigrateMode mode, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        return Run(schema.Database.Schema, mode == MigrateMode.Rebuild, fill);
    }

    public static int Migrate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema)
    {
        return Run<T>(schema.Database.Schema, false, null);
    }

    public static int Migrate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        return Run(schema.Database.Schema, false, fill);
    }

    public static int MigrateByRebuild<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema)
    {
        return Run<T>(schema.Database.Schema, true, null);
    }

    public static int MigrateByRebuild<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        return Run(schema.Database.Schema, true, fill);
    }

    public static int Migrate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema)
    {
        return Run<T>(schema, false, null);
    }

    public static int Migrate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        return Run(schema, false, fill);
    }

    public static int MigrateByRebuild<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema)
    {
        return Run<T>(schema, true, null);
    }

    public static int MigrateByRebuild<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        return Run(schema, true, fill);
    }

    public static Task<int> MigrateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return RunAsync<T>(schema, false, null, ct);
    }

    public static Task<int> MigrateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill, CancellationToken ct = default)
    {
        return RunAsync(schema, false, fill, ct);
    }

    public static Task<int> MigrateByRebuildAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return RunAsync<T>(schema, true, null, ct);
    }

    public static Task<int> MigrateByRebuildAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill, CancellationToken ct = default)
    {
        return RunAsync(schema, true, fill, ct);
    }

    public static Task<int> MigrateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema, CancellationToken ct = default)
    {
        return RunAsync<T>(schema.Database.Schema, false, null, ct);
    }

    public static Task<int> MigrateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill, CancellationToken ct = default)
    {
        return RunAsync(schema.Database.Schema, false, fill, ct);
    }

    public static Task<int> MigrateByRebuildAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema, CancellationToken ct = default)
    {
        return RunAsync<T>(schema.Database.Schema, true, null, ct);
    }

    public static Task<int> MigrateByRebuildAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableSchema<T> schema, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill, CancellationToken ct = default)
    {
        return RunAsync(schema.Database.Schema, true, fill, ct);
    }

    public static void AssertSchemaEquivalent(string? expected, string? actual)
    {
        Assert.Equal(StripWhitespace(expected), StripWhitespace(actual));
    }

    private static int Run<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(SQLiteSchema schema, bool rebuild, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>>? fill)
    {
        int next = schema.Database.Pragmas.UserVersion + 1;
        return schema.Migrations().Version(next, m => m.TableChanged(fill, rebuild)).Migrate();
    }

    private static Task<int> RunAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(SQLiteSchema schema, bool rebuild, Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>>? fill, CancellationToken ct)
    {
        int next = schema.Database.Pragmas.UserVersion + 1;
        return schema.Migrations().Version(next, m => m.TableChanged(fill, rebuild)).MigrateAsync(ct);
    }

    private static string? StripWhitespace(string? value)
    {
        return value == null ? null : new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }
}
