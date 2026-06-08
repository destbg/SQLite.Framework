using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PositionalExtraRows")]
file sealed class PositionalExtraRow
{
    public PositionalExtraRow(int a, int b)
    {
        A = a;
        B = b;
    }

    public int A { get; init; }

    public int B { get; init; }

    [Key]
    public int Id { get; init; }
}

[Table("CamelCtorRows")]
file sealed class CamelCtorRow
{
    public CamelCtorRow(int id, string note)
    {
        Id = id;
        Note = note;
    }

    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

public class PositionalConstructorMaterializationTests
{
    [Fact]
    public void PositionalRecordKeepsNonConstructorPropertyOnRead()
    {
        using TestDatabase db = new();
        db.Table<PositionalExtraRow>().Schema.CreateTable();
        db.Table<PositionalExtraRow>().Add(new PositionalExtraRow(100, 7) { Id = 1 });

        int storedId = db.Query<int>("SELECT \"Id\" FROM \"PositionalExtraRows\"").First();
        PositionalExtraRow actual = db.Table<PositionalExtraRow>().First();

        Assert.Equal(1, storedId);
        Assert.Equal(100, actual.A);
        Assert.Equal(7, actual.B);
        Assert.Equal(storedId, actual.Id);
    }

    [Fact]
    public void CamelCaseConstructorParametersMaterializeStoredValues()
    {
        using TestDatabase db = new();
        db.Table<CamelCtorRow>().Schema.CreateTable();
        db.Table<CamelCtorRow>().Add(new CamelCtorRow(5, "kept"));

        int storedId = db.Query<int>("SELECT \"Id\" FROM \"CamelCtorRows\"").First();
        string storedNote = db.Query<string>("SELECT \"Note\" FROM \"CamelCtorRows\"").First();
        CamelCtorRow actual = db.Table<CamelCtorRow>().First();

        Assert.Equal(5, storedId);
        Assert.Equal("kept", storedNote);
        Assert.Equal(storedId, actual.Id);
        Assert.Equal(storedNote, actual.Note);
    }
}
