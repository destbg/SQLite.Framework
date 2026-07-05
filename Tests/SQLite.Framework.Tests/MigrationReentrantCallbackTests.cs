using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SeededEntry")]
public class SeededEntryRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

public class MigrationReentrantCallbackTests
{
    [Fact]
    public void MigrateInsideACallbackThrows()
    {
        using TestDatabase db = new(useFile: true);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m
                .RunBefore(ctx => ctx.Database.Schema.Migrations()
                    .Version(1, mm => mm.CreateTable<SeededEntryRow>().Insert(new SeededEntryRow { Note = "seed" }))
                    .Migrate())
                .CreateTable<SeededEntryRow>()
                .Insert(new SeededEntryRow { Note = "seed" }))
            .Migrate());

        Assert.Equal("Migrate cannot run inside a migration callback. Remove the nested Migrate or Script call.", ex.Message);
        Assert.Equal(0, db.Pragmas.UserVersion);
    }
}
