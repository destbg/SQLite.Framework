using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21dRowElementRows")]
public class H21dRowElementRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Num { get; set; }

    public override string ToString()
    {
        return Id + "/" + Name;
    }
}

public class StringInlineArrayRowElementParityTests
{
    private static List<H21dRowElementRow> Rows()
    {
        return
        [
            new H21dRowElementRow { Id = 1, Name = "a", Num = 10 },
            new H21dRowElementRow { Id = 2, Name = "b", Num = 20 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21dRowElementRow>().Schema.CreateTable();
        db.Table<H21dRowElementRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ConcatObjectArrayWithRowElementMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H21dRowElementRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r }))
            .ToList();

        List<string> actual = db.Table<H21dRowElementRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinObjectArrayWithRowElementMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H21dRowElementRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object?[] { r.Name, r }))
            .ToList();

        List<string> actual = db.Table<H21dRowElementRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object?[] { r.Name, r }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatObjectArrayWithConditionalRowElementMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H21dRowElementRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r.Num > 15 ? "big" : (object)r }))
            .ToList();

        List<string> actual = db.Table<H21dRowElementRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object?[] { r.Name, r.Num > 15 ? "big" : (object)r }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereConcatObjectArrayWithRowElementMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H21dRowElementRow> local = Rows();

        List<int> expected = local
            .Where(r => string.Concat(new object?[] { r.Name, r }).EndsWith("/a"))
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H21dRowElementRow>()
            .Where(r => string.Concat(new object?[] { r.Name, r }).EndsWith("/a"))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatDirectRowArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();
        List<H21dRowElementRow> local = Rows();

        List<string> expected = local
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(r.Name, r))
            .ToList();

        List<string> actual = db.Table<H21dRowElementRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(r.Name, r))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
