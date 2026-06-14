using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonImmutableArrayRow
{
    [Key]
    public int Id { get; set; }

    public ImmutableArray<int> Numbers { get; set; } = ImmutableArray<int>.Empty;
}

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

internal sealed class JsonBoolListRow
{
    [Key]
    public int Id { get; set; }

    public List<bool> Flags { get; set; } = [];
}

internal sealed class JsonDictRow
{
    [Key]
    public int Id { get; set; }

    public Dictionary<string, int> Map { get; set; } = new();
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
    public void FilteredToArrayLengthProjects()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonIntListRow>().Schema.CreateTable();
        db.Table<JsonIntListRow>().Add(new JsonIntListRow { Id = 1, Numbers = [3, 1, 2, 5] });

        int expected = new List<int> { 3, 1, 2, 5 }.Where(x => x > 1).ToArray().Length;
        Assert.Equal(3, expected);

        int actual = db.Table<JsonIntListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Numbers.Where(x => x > 1).ToArray().Length)
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilteredToListMaterializes()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonIntListRow>().Schema.CreateTable();
        db.Table<JsonIntListRow>().Add(new JsonIntListRow { Id = 1, Numbers = [3, 1, 2, 5] });

        List<int> expected = new List<int> { 3, 1, 2, 5 }.Where(x => x > 1).ToList();
        Assert.Equal([3, 2, 5], expected);

        List<int> actual = db.Table<JsonIntListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Numbers.Where(x => x > 1).ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DerivedToHashSetThrowsClearError()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonIntListRow>().Schema.CreateTable();
        db.Table<JsonIntListRow>().Add(new JsonIntListRow { Id = 1, Numbers = [3, 1, 2, 5] });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<JsonIntListRow>()
                .Where(r => r.Id == 1)
                .Select(r => r.Numbers.ToHashSet())
                .First());
    }

    [Fact]
    public void DerivedToArrayMaterializeThrowsClearError()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonIntListRow>().Schema.CreateTable();
        db.Table<JsonIntListRow>().Add(new JsonIntListRow { Id = 1, Numbers = [3, 1, 2, 5] });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<JsonIntListRow>()
                .Where(r => r.Id == 1)
                .Select(r => r.Numbers.Where(x => x > 1).ToArray())
                .First());
    }

    [Fact]
    public void ImmutableArrayLengthProjects()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(ImmutableArray<int>)] = new SQLiteJsonConverter<ImmutableArray<int>>(TestJsonContext.Default.ImmutableArrayInt32));
        db.Table<JsonImmutableArrayRow>().Schema.CreateTable();
        db.Table<JsonImmutableArrayRow>().Add(new JsonImmutableArrayRow { Id = 1, Numbers = [3, 1, 2] });

        int actual = db.Table<JsonImmutableArrayRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Numbers.Length)
            .First();

        Assert.Equal(3, actual);
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

    [Fact]
    public void BoolListDistinctProjects()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<bool>)] = new SQLiteJsonConverter<List<bool>>(TestJsonContext.Default.ListBoolean));
        db.Table<JsonBoolListRow>().Schema.CreateTable();
        db.Table<JsonBoolListRow>().Add(new JsonBoolListRow { Id = 1, Flags = [true, false, true] });

        List<bool> expected = new List<bool> { true, false, true }.Distinct().ToList();
        Assert.Equal([true, false], expected);

        List<bool> actual = db.Table<JsonBoolListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Flags.Distinct().ToList())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DictionaryCountNestedMemberProjection()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(Dictionary<string, int>)] = new SQLiteJsonConverter<Dictionary<string, int>>(TestJsonContext.Default.DictionaryStringInt32));
        db.Table<JsonDictRow>().Schema.CreateTable();
        db.Table<JsonDictRow>().Add(new JsonDictRow { Id = 1, Map = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });

        int count = db.Table<JsonDictRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Map.Count)
            .First();

        Assert.Equal(2, count);
    }
}
