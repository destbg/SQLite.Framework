using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("StampedNote")]
public class StampedNoteRow
{
    [Key]
    public int Id { get; set; }
}

file sealed class ShadowColumnDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<StampedNoteRow>().Column("RowVersion", SQLiteColumnType.Integer, nullable: false, defaultSql: "0");
    }
}

public class ModelValidationShadowColumnTests
{
    [Fact]
    public void AMissingShadowColumnIsReported()
    {
        using ShadowColumnDb db = new();
        db.Schema.CreateTable<StampedNoteRow>();
        db.Execute("ALTER TABLE \"StampedNote\" DROP COLUMN \"RowVersion\"");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<StampedNoteRow>();

        Assert.False(result.IsValid);
    }
}
