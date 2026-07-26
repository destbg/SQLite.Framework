using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ConstantUnaryValueRows")]
public class ConstantUnaryValueRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public long Big { get; set; }

    public bool Flag { get; set; }

    public decimal Amount { get; set; }

    public TimeSpan Span { get; set; }

    public char Letter { get; set; }

    public double Rate { get; set; }
}

public class ConstantUnaryValueTranslationTests
{
    [Fact]
    public void ProjectsANegatedCapturedLocal()
    {
        int amount = 3;
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        List<int> expected = Memory().Select(_ => -amount).ToList();
        List<int> actual = db.Table<ConstantUnaryValueRow>().Select(_ => -amount).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FiltersOnANegatedCapturedLocal()
    {
        bool flag = true;
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        List<int> expected = Memory().Where(_ => !flag).Select(r => r.Id).ToList();
        List<int> actual = db.Table<ConstantUnaryValueRow>().Where(_ => !flag).Select(r => r.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UpdateStoresANegatedLong()
    {
        long big = 9_000_000_000L;
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        db.Table<ConstantUnaryValueRow>().ExecuteUpdate(s => s.Set(x => x.Big, _ => -big));

        Assert.Equal(-big, db.Table<ConstantUnaryValueRow>().Select(r => r.Big).Single());
    }

    [Fact]
    public void UpdateStoresANegatedDecimal()
    {
        decimal amount = 12.75m;
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        db.Table<ConstantUnaryValueRow>().ExecuteUpdate(s => s.Set(x => x.Amount, _ => -amount));

        Assert.Equal(-amount, db.Table<ConstantUnaryValueRow>().Select(r => r.Amount).Single());
    }

    [Fact]
    public void UpdateStoresANegatedDouble()
    {
        double rate = 2.5;
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        db.Table<ConstantUnaryValueRow>().ExecuteUpdate(s => s.Set(x => x.Rate, _ => -rate));

        Assert.Equal(-rate, db.Table<ConstantUnaryValueRow>().Select(r => r.Rate).Single());
    }

    [Fact]
    public void UpdateStoresANegatedTimeSpan()
    {
        TimeSpan span = TimeSpan.FromMinutes(30);
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        db.Table<ConstantUnaryValueRow>().ExecuteUpdate(s => s.Set(x => x.Span, _ => -span));

        Assert.Equal(-span, db.Table<ConstantUnaryValueRow>().Select(r => r.Span).Single());
    }

    [Fact]
    public void UpdateStoresAConvertedCharacterCodeAsText()
    {
        int code = 'Q';
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        db.Table<ConstantUnaryValueRow>().ExecuteUpdate(s => s.Set(x => x.Letter, _ => (char)code));

        Assert.Equal((char)code, db.Table<ConstantUnaryValueRow>().Select(r => r.Letter).Single());
    }

    [Fact]
    public void UpdateStoresAConvertedCharacterCodeAsInteger()
    {
        int code = 'Q';
        using TestDatabase db = new(b => b.UseCharStorage(CharStorageMode.Integer));
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        db.Table<ConstantUnaryValueRow>().ExecuteUpdate(s => s.Set(x => x.Letter, _ => (char)code));

        Assert.Equal((char)code, db.Table<ConstantUnaryValueRow>().Select(r => r.Letter).Single());
    }

    [Fact]
    public void UpdateStoresACapturedArrayLength()
    {
        int[] values = [4, 5, 6];
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        db.Table<ConstantUnaryValueRow>().ExecuteUpdate(s => s.Set(x => x.Val, _ => values.Length));

        Assert.Equal(values.Length, db.Table<ConstantUnaryValueRow>().Select(r => r.Val).Single());
    }

    [Fact]
    public void UpdateStoresAComplementedUnsignedValue()
    {
        uint mask = 8u;
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        db.Table<ConstantUnaryValueRow>().ExecuteUpdate(s => s.Set(x => x.Big, _ => ~mask));

        Assert.Equal(~mask, (uint)db.Table<ConstantUnaryValueRow>().Select(r => r.Big).Single());
    }

    [Fact]
    public void ProjectsACapturedReferenceCastWithTheAsOperator()
    {
        object boxed = "kiwi";
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();
        db.Table<ConstantUnaryValueRow>().Add(Row());

        List<string?> expected = Memory().Select(_ => boxed as string).ToList();
        List<string?> actual = db.Table<ConstantUnaryValueRow>().Select(_ => boxed as string).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WriteColumnsStoreANegatedValue()
    {
        int amount = 3;
        using TestDatabase db = new();
        db.Table<ConstantUnaryValueRow>().Schema.CreateTable();

        db.Table<ConstantUnaryValueRow>()
            .WithColumns(c => c.Set(x => x.Val, _ => -amount))
            .Add(Row());

        Assert.Equal(-amount, db.Table<ConstantUnaryValueRow>().Select(r => r.Val).Single());
    }

    private static ConstantUnaryValueRow Row()
    {
        return new ConstantUnaryValueRow { Id = 1, Val = 0, Big = 0, Flag = true, Amount = 0m, Span = TimeSpan.Zero, Letter = 'a', Rate = 0 };
    }

    private static List<ConstantUnaryValueRow> Memory()
    {
        return [Row()];
    }
}
