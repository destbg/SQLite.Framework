using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NullableArgumentRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }

    public int? Bonus { get; set; }

    public string Name { get; set; } = "";
}

public class ClientEvalNullableArgumentRebuildTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<NullableArgumentRow>().Schema.CreateTable();
        db.Table<NullableArgumentRow>().Add(new NullableArgumentRow { Id = 1, Amount = 1, Bonus = null, Name = "a" });
        db.Table<NullableArgumentRow>().Add(new NullableArgumentRow { Id = 2, Amount = 2, Bonus = 4, Name = "b" });
        return db;
    }

    [Fact]
    public void NullableColumnPassedToClientMethodKeepsItsValue()
    {
        using TestDatabase db = SetupDatabase();

        List<int?> expected = db.Table<NullableArgumentRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.PassNullable(r.Bonus))
            .ToList();

        Assert.Equal([null, 4], expected);

        List<int?> actual = db.Table<NullableArgumentRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.PassNullable(r.Bonus))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientMethodResultAddedToNullableColumnLifts()
    {
        using TestDatabase db = SetupDatabase();

        List<int?> expected = db.Table<NullableArgumentRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount) + r.Bonus)
            .ToList();

        Assert.Equal([null, null], expected);

        List<int?> actual = db.Table<NullableArgumentRow>()
            .OrderBy(r => r.Id)
            .Select(r => ClientEvalTestFunctions.ToNullable(r.Amount) + r.Bonus)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientMethodResultConcatenatedWithIntColumnFormats()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<NullableArgumentRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => r.Name.Normalize() + r.Id)
            .ToList();

        Assert.Equal(["a1", "b2"], expected);

        List<string> actual = db.Table<NullableArgumentRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Name.Normalize() + r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ObjectArrayInitializerWithColumnsAndClientCallJoins()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<NullableArgumentRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object[] { r.Amount, r.Name, ClientEvalTestFunctions.Pass(r.Amount) }))
            .ToList();

        Assert.Equal(["1-a-1", "2-b-2"], expected);

        List<string> actual = db.Table<NullableArgumentRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new object[] { r.Amount, r.Name, ClientEvalTestFunctions.Pass(r.Amount) }))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
