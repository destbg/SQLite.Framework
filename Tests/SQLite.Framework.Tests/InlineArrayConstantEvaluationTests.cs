using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class IntArrayHolder
{
    public int[] Items;
    public IntArrayHolder(int[] items) => Items = items;
}

internal sealed class IntArrayBag
{
    public int[] Items { get; set; } = [];
}

internal sealed class StrArrayHolder
{
    public string[] Items;
    public StrArrayHolder(string[] items) => Items = items;
}

internal sealed class IarRow
{
    [Key]
    public int Id { get; set; }
    public int Value { get; set; }
    public string Name { get; set; } = "";
}

public class InlineArrayConstantEvaluationTests
{
    private static readonly IarRow[] Data =
    [
        new IarRow { Id = 1, Value = 2, Name = "apple" },
        new IarRow { Id = 2, Value = 9, Name = "banana" },
        new IarRow { Id = 3, Value = 0, Name = "cherry" },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<IarRow>().Schema.CreateTable();
        db.Table<IarRow>().AddRange(Data);
        return db;
    }

    private static void AssertIds(Func<IarRow, bool> oraclePred, System.Linq.Expressions.Expression<Func<IarRow, bool>> sqlPred, int[] expected)
    {
        using TestDatabase db = CreateDb();
        List<int> oracle = Data.Where(oraclePred).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<IarRow>().Where(sqlPred).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal(expected, oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ArrayInConstructor_ImplicitType()
        => AssertIds(x => new IntArrayHolder(new[] { 1, 2, 3 }).Items.Contains(x.Value),
                     x => new IntArrayHolder(new[] { 1, 2, 3 }).Items.Contains(x.Value), [1]);

    [Fact]
    public void ArrayInConstructor_ExplicitType()
        => AssertIds(x => new IntArrayHolder(new int[] { 1, 2, 3 }).Items.Contains(x.Value),
                     x => new IntArrayHolder(new int[] { 1, 2, 3 }).Items.Contains(x.Value), [1]);

    [Fact]
    public void ArrayInMemberInit()
        => AssertIds(x => new IntArrayBag { Items = new[] { 1, 2, 3 } }.Items.Contains(x.Value),
                     x => new IntArrayBag { Items = new[] { 1, 2, 3 } }.Items.Contains(x.Value), [1]);

    [Fact]
    public void DirectInlineArray_Control()
        => AssertIds(x => new[] { 1, 2, 3 }.Contains(x.Value),
                     x => new[] { 1, 2, 3 }.Contains(x.Value), [1]);

    [Fact]
    public void StringArrayInConstructor()
        => AssertIds(x => new StrArrayHolder(new[] { "apple", "cherry" }).Items.Contains(x.Name),
                     x => new StrArrayHolder(new[] { "apple", "cherry" }).Items.Contains(x.Name), [1, 3]);

    [Fact]
    public void ListInitInConstructor()
        => AssertIds(x => new List<int> { 9 }.Contains(x.Value),
                     x => new List<int> { 9 }.Contains(x.Value), [2]);

    [Fact]
    public void EmptyArrayInConstructor()
        => AssertIds(x => new IntArrayHolder(new int[] { }).Items.Contains(x.Value),
                     x => new IntArrayHolder(new int[] { }).Items.Contains(x.Value), []);

    [Fact]
    public void NewArrayBoundsInConstructor_AllDefault()
        => AssertIds(x => new IntArrayHolder(new int[3]).Items.Contains(x.Value),
                     x => new IntArrayHolder(new int[3]).Items.Contains(x.Value), [3]);
}
