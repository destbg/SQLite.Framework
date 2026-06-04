using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CompKey")]
file sealed class CompKeyEntity
{
    public int Id { get; set; }
    public int Code { get; set; }
    public string Name { get; set; } = "";
}

[Table("IdxA")]
file sealed class IndexedA
{
    [Key]
    public int Id { get; set; }

    [Indexed]
    public int SharedColumn { get; set; }
}

[Table("IdxB")]
file sealed class IndexedB
{
    [Key]
    public int Id { get; set; }

    [Indexed]
    public int SharedColumn { get; set; }
}

file enum WidgetStatus
{
    None = 0,
    Active = 1,
    Retired = 2,
}

[Table("Widgets")]
file sealed class Widget
{
    [Key]
    public int Id { get; set; }

    public WidgetStatus Status { get; set; }
}

public class SchemaDeclarationTests
{
    private static string? TableSql(TestDatabase db, string name) =>
        db.ExecuteScalar<string>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{name}'");

    [Fact]
    public void CompositePrimaryKeyHonorsDeclaredColumnOrder()
    {
        using ModelTestDatabase db = new(model => model.Entity<CompKeyEntity>().HasKey(e => new { e.Code, e.Id }));
        db.Schema.CreateTable<CompKeyEntity>();

        Assert.Contains("PRIMARY KEY (\"Code\", \"Id\")", TableSql(db, "CompKey"));
    }

    [Fact]
    public void IndexedColumnsWithSameNameOnDifferentTablesBothGetIndexes()
    {
        using TestDatabase db = new();
        db.Table<IndexedA>().Schema.CreateTable();
        db.Table<IndexedB>().Schema.CreateTable();

        long indexesOnB = db.ExecuteScalar<long>(
            "SELECT count(*) FROM sqlite_master WHERE type = 'index' AND tbl_name = 'IdxB' AND sql IS NOT NULL");

        Assert.True(indexesOnB >= 1);
    }

    [Fact]
    public void DefaultEnumValueUnderTextStorageMatchesExplicitlyWrittenValue()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<Widget>().Default(w => w.Status, WidgetStatus.Active),
            options => options.UseEnumStorage(EnumStorageMode.Text));
        db.Table<Widget>().Schema.CreateTable();

        db.Table<Widget>().Add(new Widget { Id = 1, Status = WidgetStatus.Active });
        db.Table<Widget>().Add(new Widget { Id = 2, Status = WidgetStatus.None });

        List<int> activeIds = db.Table<Widget>()
            .Where(w => w.Status == WidgetStatus.Active)
            .Select(w => w.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(new[] { 1, 2 }, activeIds);
    }
}
