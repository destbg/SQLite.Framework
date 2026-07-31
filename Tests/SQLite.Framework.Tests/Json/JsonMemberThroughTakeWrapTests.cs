using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CarryJsonPart
{
    public int First { get; set; }
}

public class CarryJsonOuter
{
    public int Id { get; set; }

    public CarryJsonPart? Part { get; set; }
}

public class JsonMemberThroughTakeWrapTests
{
    [Fact]
    public void AJsonMemberProjectedThroughATakeWrapKeepsItsValue()
    {
        using TestDatabase db = Setup(nameof(AJsonMemberProjectedThroughATakeWrapKeepsItsValue));

        List<int> actual = db.Table<CarryJsonDoc>()
            .OrderBy(d => d.Id)
            .Select(d => new { d.Id, First = d.Numbers.First() })
            .Take(2)
            .Where(x => x.First >= 7)
            .OrderBy(x => x.First)
            .Select(x => x.First)
            .AsEnumerable()
            .ToList();

        Assert.Equal(new List<int> { 7, 9 }, actual);
    }

    [Fact]
    public void ANestedJsonMemberProjectedThroughATakeWrapKeepsItsValue()
    {
        using TestDatabase db = Setup(nameof(ANestedJsonMemberProjectedThroughATakeWrapKeepsItsValue));

        List<int> actual = db.Table<CarryJsonDoc>()
            .OrderBy(d => d.Id)
            .Select(d => new CarryJsonOuter { Id = d.Id, Part = new CarryJsonPart { First = d.Numbers.First() } })
            .Take(2)
            .Where(x => x.Part!.First >= 7)
            .OrderBy(x => x.Part!.First)
            .Select(x => x.Part!.First)
            .AsEnumerable()
            .ToList();

        Assert.Equal(new List<int> { 7, 9 }, actual);
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(
            b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(CarryJsonContext.Default.ListInt32),
            methodName);
        db.Table<CarryJsonDoc>().Schema.CreateTable();
        db.Table<CarryJsonDoc>().AddRange(
        [
            new CarryJsonDoc { Id = 1, Numbers = [7, 8] },
            new CarryJsonDoc { Id = 2, Numbers = [9] }
        ]);
        return db;
    }
}
