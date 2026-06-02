using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StringCaseFoldingTests
{
    private static TestDatabase Seed(params string[] names)
    {
        TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        for (int i = 0; i < names.Length; i++)
        {
            db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = i + 1, Name = names[i] });
        }

        return db;
    }

    [Fact]
    public void ToUpper_Ascii_MatchesDotNet()
    {
        using TestDatabase db = Seed("abc");

        string actual = db.Table<NullableStringEntity>().Where(r => r.Id == 1).Select(r => r.Name!.ToUpper()).First();

        Assert.Equal("abc".ToUpper(), actual);
    }

    [Fact]
    public void ToLower_Ascii_MatchesDotNet()
    {
        using TestDatabase db = Seed("ABC");

        string actual = db.Table<NullableStringEntity>().Where(r => r.Id == 1).Select(r => r.Name!.ToLower()).First();

        Assert.Equal("ABC".ToLower(), actual);
    }

    [Fact]
    public void ToUpper_MatchesSqliteUpper()
    {
        string[] inputs = ["abc", "café", "абв"];
        using TestDatabase db = Seed(inputs);

        List<string> viaFramework = db.Table<NullableStringEntity>().OrderBy(r => r.Id).Select(r => r.Name!.ToUpper()).ToList();
        List<string> viaSqlite = Enumerable.Range(1, inputs.Length)
            .Select(id => db.ExecuteScalar<string>($"SELECT upper(\"Name\") FROM \"NullableStringEntity\" WHERE \"Id\" = {id}")!)
            .ToList();

        Assert.Equal(viaSqlite, viaFramework);
    }

    [Fact]
    public void ToLower_MatchesSqliteLower()
    {
        string[] inputs = ["ABC", "CAFÉ", "АБВ"];
        using TestDatabase db = Seed(inputs);

        List<string> viaFramework = db.Table<NullableStringEntity>().OrderBy(r => r.Id).Select(r => r.Name!.ToLower()).ToList();
        List<string> viaSqlite = Enumerable.Range(1, inputs.Length)
            .Select(id => db.ExecuteScalar<string>($"SELECT lower(\"Name\") FROM \"NullableStringEntity\" WHERE \"Id\" = {id}")!)
            .ToList();

        Assert.Equal(viaSqlite, viaFramework);
    }
}
