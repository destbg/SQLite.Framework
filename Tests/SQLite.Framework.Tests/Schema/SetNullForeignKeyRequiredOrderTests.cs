using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26jSetNullParents")]
public class H26jSetNullParent
{
    [Key]
    public int Id { get; set; }
}

[Table("H26jSetNullChildren")]
public class H26jSetNullChild
{
    [Key]
    public int Id { get; set; }

    public int? ParentId { get; set; }
}

public sealed class H26jSetNullThenRequiredDatabase : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H26jSetNullChild>()
            .ForeignKey<H26jSetNullParent>(c => c.ParentId, onDelete: SQLiteForeignKeyAction.SetNull)
            .IsRequired(c => c.ParentId);
    }
}

public class SetNullForeignKeyRequiredOrderTests
{
    [Fact]
    public void ASetNullForeignKeyOnAColumnMadeRequiredAfterwardsIsRejected()
    {
        using H26jSetNullThenRequiredDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<H26jSetNullChild>());
    }
}
