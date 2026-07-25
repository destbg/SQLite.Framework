using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MailAcct")]
public class MailAcctRow
{
    [Key]
    public int Id { get; set; }

    [Indexed("IX_MailAcct_Email", 0, IsUnique = true)]
    public string Email { get; set; } = "";
}

public class MigrationDropColumnIndexPreservationTests
{
    [Fact]
    public void DropColumnRebuildKeepsModelIndexes()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<MailAcctRow>().Schema.CreateTable();
        db.Execute("ALTER TABLE \"MailAcct\" ADD COLUMN \"Legacy\" TEXT");
        db.Execute("CREATE VIEW \"MailAcctLegacyView\" AS SELECT \"Legacy\" FROM \"MailAcct\"");
        db.Table<MailAcctRow>().Add(new MailAcctRow { Id = 1, Email = "a@x" });

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<MailAcctRow>("Legacy"))
            .Migrate();

        Assert.ThrowsAny<SQLiteException>(() => db.Table<MailAcctRow>().Add(new MailAcctRow { Id = 2, Email = "a@x" }));
    }
}
