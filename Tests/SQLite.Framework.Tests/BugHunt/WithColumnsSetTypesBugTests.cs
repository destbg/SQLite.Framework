using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

file enum HuntColor
{
    Red = 0,
    Green = 1,
    Blue = 2,
}

[Table("HuntRows")]
file sealed class HuntRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTime When { get; set; }
}

public class WithColumnsSetTypesBugTests
{
    [Fact]
    public void WithColumnsSetDateTimeConstantRoundTrips()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<HuntRow>();

        HuntRow row = new() { Id = 1, Name = "a", When = new DateTime(2000, 1, 1) };
        db.Table<HuntRow>().Add(row);

        DateTime target = new(2021, 6, 15, 10, 30, 0);

        db.Table<HuntRow>()
            .WithColumns(c => c.Set(r => r.When, target))
            .Update(row);

        DateTime actual = db.Table<HuntRow>().Single().When;

        Assert.Equal(target, actual);
    }

    [Fact]
    public void WithColumnsSetEnumUnderTextStorageWritesMemberName()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<HuntRow>().Column("Color", SQLiteColumnType.Text, nullable: true),
            options => options.UseEnumStorage(EnumStorageMode.Text));
        db.Schema.CreateTable<HuntRow>();

        db.Table<HuntRow>()
            .WithColumns(c => c.Set(r => SQLiteColumn.Of<HuntColor>(r, "Color"), HuntColor.Green))
            .Add(new HuntRow { Id = 1, Name = "a", When = new DateTime(2000, 1, 1) });

        string stored = db.ExecuteScalar<string>("SELECT \"Color\" FROM \"HuntRows\"")!;

        Assert.Equal("Green", stored);
    }
}
