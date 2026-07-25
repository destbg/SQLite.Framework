using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("mig_rawsql_before_recreate_rows")]
public class RawSqlBeforeRecreateRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationRawSqlBeforeRecreateTests
{
    [Fact]
    public void RawSqlInsertBeforeADropAndRecreateDoesNotLeakIntoTheFreshTable()
    {
        using TestDatabase collapsed = new(useFile: true);
        collapsed.Schema.Migrations()
            .Version(2, m => m
                .CreateTable<RawSqlBeforeRecreateRow>()
                .Sql("INSERT INTO \"mig_rawsql_before_recreate_rows\" (\"Id\", \"Name\") VALUES (1, 'seed')"))
            .Version(3, m => m.DropTable<RawSqlBeforeRecreateRow>())
            .Version(4, m => m.CreateTable<RawSqlBeforeRecreateRow>())
            .Migrate();

        using TestDatabase stepwise = new(useFile: true);
        stepwise.Schema.Migrations()
            .Version(2, m => m
                .CreateTable<RawSqlBeforeRecreateRow>()
                .Sql("INSERT INTO \"mig_rawsql_before_recreate_rows\" (\"Id\", \"Name\") VALUES (1, 'seed')"))
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m
                .CreateTable<RawSqlBeforeRecreateRow>()
                .Sql("INSERT INTO \"mig_rawsql_before_recreate_rows\" (\"Id\", \"Name\") VALUES (1, 'seed')"))
            .Version(3, m => m.DropTable<RawSqlBeforeRecreateRow>())
            .Migrate();
        stepwise.Schema.Migrations()
            .Version(2, m => m
                .CreateTable<RawSqlBeforeRecreateRow>()
                .Sql("INSERT INTO \"mig_rawsql_before_recreate_rows\" (\"Id\", \"Name\") VALUES (1, 'seed')"))
            .Version(3, m => m.DropTable<RawSqlBeforeRecreateRow>())
            .Version(4, m => m.CreateTable<RawSqlBeforeRecreateRow>())
            .Migrate();

        int stepwiseCount = stepwise.Table<RawSqlBeforeRecreateRow>().Count();
        int collapsedCount = collapsed.Table<RawSqlBeforeRecreateRow>().Count();

        Assert.Equal(0, stepwiseCount);
        Assert.Equal(stepwiseCount, collapsedCount);
    }

    [Fact]
    public void RunCallbackInsertBeforeADropAndRecreateDoesNotLeakIntoTheFreshTable()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(2, m => m
                .CreateTable<RawSqlBeforeRecreateRow>()
                .Run(ctx => ctx.Database.Execute("INSERT INTO \"mig_rawsql_before_recreate_rows\" (\"Id\", \"Name\") VALUES (1, 'seed')")))
            .Version(3, m => m.DropTable<RawSqlBeforeRecreateRow>())
            .Version(4, m => m.CreateTable<RawSqlBeforeRecreateRow>())
            .Migrate();

        Assert.Equal(0, db.Table<RawSqlBeforeRecreateRow>().Count());
    }

    [Fact]
    public void RawSqlInsertAfterTheRecreateSurvives()
    {
        using TestDatabase db = new(useFile: true);
        db.Schema.Migrations()
            .Version(2, m => m.CreateTable<RawSqlBeforeRecreateRow>())
            .Version(3, m => m.DropTable<RawSqlBeforeRecreateRow>())
            .Version(4, m => m
                .CreateTable<RawSqlBeforeRecreateRow>()
                .Sql("INSERT INTO \"mig_rawsql_before_recreate_rows\" (\"Id\", \"Name\") VALUES (2, 'kept')"))
            .Migrate();

        RawSqlBeforeRecreateRow survivor = db.Table<RawSqlBeforeRecreateRow>().Single();

        Assert.Equal(2, survivor.Id);
        Assert.Equal("kept", survivor.Name);
    }
}
