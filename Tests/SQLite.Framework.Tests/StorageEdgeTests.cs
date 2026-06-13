using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file enum DStatus
{
    None = 0,
    Active = 1
}

[Table("DefaultEnumRows")]
file sealed class DefaultEnumRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }

    [DefaultValue(DStatus.Active)]
    public DStatus Status { get; set; }
}

[Table("TwoArgDefaultEnumRows")]
file sealed class TwoArgDefaultEnumRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Name { get; set; }

    [DefaultValue(typeof(DStatus), "Active")]
    public DStatus Status { get; set; }
}

public class StorageEdgeTests
{
    [Fact]
    public void ScalarUnparseableEnumTextDoesNotCrash()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Execute("CREATE TABLE \"EnumScalar\" (\"V\" TEXT)");
        db.Execute("INSERT INTO \"EnumScalar\" (\"V\") VALUES ('garbage')");

        Exception? ex = Record.Exception(() => db.ExecuteScalar<PublisherType>("SELECT \"V\" FROM \"EnumScalar\""));

        Assert.False(ex is NullReferenceException, ex?.ToString());
    }

    [Fact]
    public void EnumDefaultLiteralMatchesTextStorage()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<DefaultEnumRow>().Schema.CreateTable();
        db.Table<DefaultEnumRow>().Add(new DefaultEnumRow { Name = "defaulted" });

        List<DefaultEnumRow> active = db.Table<DefaultEnumRow>()
            .Where(r => r.Status == DStatus.Active)
            .ToList();

        Assert.Single(active);
    }

    [Fact]
    public void TwoArgEnumDefaultLiteralApplies()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<TwoArgDefaultEnumRow>().Schema.CreateTable();
        db.Table<TwoArgDefaultEnumRow>().Add(new TwoArgDefaultEnumRow { Name = "defaulted" });

        DStatus stored = db.Table<TwoArgDefaultEnumRow>().Single().Status;

        Assert.Equal(DStatus.Active, stored);
    }
}
