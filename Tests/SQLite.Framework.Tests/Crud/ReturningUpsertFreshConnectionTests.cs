using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FreshUpsert")]
public class FreshUpsertRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ReturningUpsertFreshConnectionTests
{
    [Fact]
    public void ReturningUpsertOnFreshConnectionWrites()
    {
        using TestDatabase setup = new(useFile: true);
        setup.Table<FreshUpsertRow>().Schema.CreateTable();

        SQLiteOptionsBuilder builder = new(setup.Options.DatabasePath);
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
        using SQLiteDatabase db = new(builder.Build());
        FreshUpsertRow? returned = db.Table<FreshUpsertRow>()
            .Returning()
            .Upsert(new FreshUpsertRow { Id = 1, Name = "x" }, c => c.OnConflict(x => x.Id).DoUpdateAll());

        Assert.NotNull(returned);
    }
}
