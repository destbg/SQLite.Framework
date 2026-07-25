using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SeedEvent")]
public class SeedEventRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public DateTime HeldAt { get; set; }
}

public class MigrationInsertIfMissingTextDateKeyTests
{
    [Fact]
    public void SkipsRowWhoseTextStoredKeyIsAlreadyInTheTable()
    {
        DateTime key = new(2024, 1, 2, 3, 4, 5, 123);
        using TestDatabase db = new(o => o.UseDateTimeStorage(DateTimeStorageMode.TextFormatted, "yyyy-MM-dd HH:mm:ss"), useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SeedEventRow>()
                .Insert(new SeedEventRow { HeldAt = key }))
            .Migrate();

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<SeedEventRow>()
                .Insert(new SeedEventRow { HeldAt = key }))
            .Version(2, m => m.InsertIfMissing(x => x.HeldAt, new SeedEventRow { HeldAt = key }))
            .Migrate();

        Assert.Equal(1, db.Table<SeedEventRow>().Count());
    }
}
