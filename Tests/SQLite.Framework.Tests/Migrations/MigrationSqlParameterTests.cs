using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SqlParamRows")]
public class SqlParameterStepRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationSqlParameterTests
{
    [Fact]
    public void ParametersAreBoundToTheStatement()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SqlParameterStepRow>()
                .Sql(
                    "INSERT INTO \"SqlParamRows\" (\"Id\", \"Name\") VALUES (@id, @name)",
                    new SQLiteParameter { Name = "@id", Value = 1 },
                    new SQLiteParameter { Name = "@name", Value = "it's bound" }))
            .Migrate();

        Assert.Equal("it's bound", db.Table<SqlParameterStepRow>().Single().Name);
    }

    [Fact]
    public void StatementWithoutParametersStillRuns()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SqlParameterStepRow>()
                .Sql("INSERT INTO \"SqlParamRows\" (\"Id\", \"Name\") VALUES (2, 'plain')"))
            .Migrate();

        Assert.Equal("plain", db.Table<SqlParameterStepRow>().Single().Name);
    }

    [Fact]
    public void NullParametersArrayThrows()
    {
        using TestDatabase db = new(useFile: true);

        Assert.Throws<ArgumentNullException>(() => db.Schema.Migrations()
            .Version(1, m => m.Sql("SELECT 1", null!))
            .Migrate());
    }
}
