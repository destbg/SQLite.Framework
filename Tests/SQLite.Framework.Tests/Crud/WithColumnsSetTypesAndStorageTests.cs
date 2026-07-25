using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file enum WcColor
{
    Red = 0,
    Green = 1,
    Blue = 2,
}

[Table("WcSetRows")]
file sealed class WcSetRow
{
    [Key]
    public int Id { get; set; }

    public WcColor Color { get; set; }

    public DateTime Created { get; set; }

    public Guid Uid { get; set; }

    public TimeSpan Span { get; set; }

    public DateOnly Day { get; set; }

    public TimeOnly Clock { get; set; }

    public char Letter { get; set; }

    public byte[] Data { get; set; } = [];
}

public class WithColumnsSetTypesAndStorageTests
{
    private static readonly DateTime Created = new(2021, 6, 15, 10, 30, 45, DateTimeKind.Utc);
    private static readonly Guid Uid = new("11112222-3333-4444-5555-666677778888");
    private static readonly TimeSpan Span = new(1, 30, 0);
    private static readonly DateOnly Day = new(2021, 6, 15);
    private static readonly TimeOnly Clock = new(10, 30, 45);
    private static readonly byte[] Data = [1, 2, 3, 4];

    [Fact]
    public void Set_AllTypes_RoundTrip()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<WcSetRow>().Schema.CreateTable();

        db.Table<WcSetRow>()
            .WithColumns(c => c
                .Set(x => x.Color, WcColor.Green)
                .Set(x => x.Created, Created)
                .Set(x => x.Uid, Uid)
                .Set(x => x.Span, Span)
                .Set(x => x.Day, Day)
                .Set(x => x.Clock, Clock)
                .Set(x => x.Letter, 'Q')
                .Set(x => x.Data, Data))
            .Add(new WcSetRow { Id = 1, Data = [] });

        WcSetRow row = db.Table<WcSetRow>().Single();

        Assert.Equal(WcColor.Green, row.Color);
        Assert.Equal(Created, row.Created);
        Assert.Equal(Uid, row.Uid);
        Assert.Equal(Span, row.Span);
        Assert.Equal(Day, row.Day);
        Assert.Equal(Clock, row.Clock);
        Assert.Equal('Q', row.Letter);
        Assert.Equal(Data, row.Data);
    }

    [Fact]
    public void Set_EnumUnderTextStorage_MatchesNormalAddMemberName()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<WcSetRow>().Schema.CreateTable();

        db.Table<WcSetRow>().Add(new WcSetRow { Id = 1, Color = WcColor.Green, Data = [] });
        db.Table<WcSetRow>()
            .WithColumns(c => c.Set(x => x.Color, WcColor.Green))
            .Add(new WcSetRow { Id = 2, Data = [] });

        string normal = db.ExecuteScalar<string>("SELECT \"Color\" FROM \"WcSetRows\" WHERE \"Id\" = 1")!;
        string withColumns = db.ExecuteScalar<string>("SELECT \"Color\" FROM \"WcSetRows\" WHERE \"Id\" = 2")!;

        Assert.Equal("Green", normal);
        Assert.Equal(normal, withColumns);
    }

    [Fact]
    public void SetExpression_DateTimeConstant_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<WcSetRow>().Schema.CreateTable();
        db.Table<WcSetRow>().Add(new WcSetRow { Id = 1, Data = [] });

        db.Table<WcSetRow>()
            .WithColumns(c => c.Set(x => x.Created, _ => Created))
            .Update(new WcSetRow { Id = 1, Data = [] });

        Assert.Equal(Created, db.Table<WcSetRow>().Single().Created);
    }

    [Fact]
    public void SetExpression_EnumConstantUnderTextStorage_StoresMemberName()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<WcSetRow>().Schema.CreateTable();

        db.Table<WcSetRow>()
            .WithColumns(c => c.Set(x => x.Color, _ => WcColor.Green))
            .Add(new WcSetRow { Id = 1, Data = [] });

        string stored = db.ExecuteScalar<string>("SELECT \"Color\" FROM \"WcSetRows\"")!;
        Assert.Equal("Green", stored);
    }

    [Fact]
    public void Set_DateTime_MatchesNormalAddRawStorage()
    {
        using TestDatabase db = new();
        db.Table<WcSetRow>().Schema.CreateTable();

        db.Table<WcSetRow>().Add(new WcSetRow { Id = 1, Created = Created, Data = [] });
        db.Table<WcSetRow>()
            .WithColumns(c => c.Set(x => x.Created, Created))
            .Add(new WcSetRow { Id = 2, Data = [] });

        long normal = db.ExecuteScalar<long>("SELECT \"Created\" FROM \"WcSetRows\" WHERE \"Id\" = 1");
        long withColumns = db.ExecuteScalar<long>("SELECT \"Created\" FROM \"WcSetRows\" WHERE \"Id\" = 2");

        Assert.Equal(Created.Ticks, normal);
        Assert.Equal(normal, withColumns);
    }
}
