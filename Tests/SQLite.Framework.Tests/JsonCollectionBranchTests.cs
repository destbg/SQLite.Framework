using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonIntListRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

internal sealed class JsonNullableBoolListRow
{
    [Key]
    public int Id { get; set; }

    public List<bool?> Flags { get; set; } = [];
}

public class JsonCollectionBranchTests
{
    [Fact]
    public void PrependProjectsToFront()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonIntListRow>().Schema.CreateTable();
        db.Table<JsonIntListRow>().Add(new JsonIntListRow { Id = 1, Numbers = [3, 1, 2] });

        List<int> expected = new List<int> { 3, 1, 2 }.Prepend(9).ToList();
        Assert.Equal([9, 3, 1, 2], expected);

        List<int> actual = db.Table<JsonIntListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Numbers.Prepend(9).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableBoolListDistinctProjects()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<bool?>)] = new SQLiteJsonConverter<List<bool?>>(TestJsonContext.Default.ListNullableBoolean));
        db.Table<JsonNullableBoolListRow>().Schema.CreateTable();
        db.Table<JsonNullableBoolListRow>().Add(new JsonNullableBoolListRow { Id = 1, Flags = [true, null, false, true] });

        List<bool?> expected = new List<bool?> { true, null, false, true }.Distinct().ToList();
        Assert.Equal([true, null, false], expected);

        List<bool?> actual = db.Table<JsonNullableBoolListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Flags.Distinct().ToList())
            .First();

        Assert.Equal(expected, actual);
    }
}
