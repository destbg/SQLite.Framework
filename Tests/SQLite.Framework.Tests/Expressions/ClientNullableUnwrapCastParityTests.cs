using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

#pragma warning disable CS8629

namespace SQLite.Framework.Tests;

[Table("H26mNullableUnwrapRows")]
public class H26mNullableUnwrapRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public static class H26mAbsentValue
{
    public static int? Number(int value)
    {
        return value > 1000 ? value : null;
    }

    public static DateTime? Moment(int value)
    {
        return value > 1000 ? new DateTime(2020, 1, 1) : null;
    }
}

public class ClientNullableUnwrapCastParityTests
{
    [Fact]
    public void UnwrappingAnEmptyClientComputedNullableNumberFailsTheSameWayAsLinqToObjects()
    {
        using TestDatabase db = Setup(nameof(UnwrappingAnEmptyClientComputedNullableNumberFailsTheSameWayAsLinqToObjects));
        List<H26mNullableUnwrapRow> local = Rows();

        Assert.Throws<InvalidOperationException>(() => local
            .Select(r => (int)H26mAbsentValue.Number(r.Amount))
            .ToList());

        Assert.Throws<InvalidOperationException>(() => db.Table<H26mNullableUnwrapRow>()
            .Select(r => (int)H26mAbsentValue.Number(r.Amount))
            .ToList());
    }

    [Fact]
    public void UnwrappingAnEmptyClientComputedNullableDateFailsTheSameWayAsLinqToObjects()
    {
        using TestDatabase db = Setup(nameof(UnwrappingAnEmptyClientComputedNullableDateFailsTheSameWayAsLinqToObjects));
        List<H26mNullableUnwrapRow> local = Rows();

        Assert.Throws<InvalidOperationException>(() => local
            .Select(r => (DateTime)H26mAbsentValue.Moment(r.Amount))
            .ToList());

        Assert.Throws<InvalidOperationException>(() => db.Table<H26mNullableUnwrapRow>()
            .Select(r => (DateTime)H26mAbsentValue.Moment(r.Amount))
            .ToList());
    }

    private static List<H26mNullableUnwrapRow> Rows()
    {
        return
        [
            new H26mNullableUnwrapRow { Id = 1, Amount = 5 },
            new H26mNullableUnwrapRow { Id = 2, Amount = 7 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(o =>
        {
            o.SelectMaterializers.Clear();
            o.ReflectionFallbackDisabled = false;
        }, methodName);
        db.Table<H26mNullableUnwrapRow>().Schema.CreateTable();
        db.Table<H26mNullableUnwrapRow>().AddRange(Rows());
        return db;
    }
}
