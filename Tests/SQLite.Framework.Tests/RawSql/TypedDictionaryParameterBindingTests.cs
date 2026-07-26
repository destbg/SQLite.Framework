using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TypedDictionaryParameterBindingTests
{
    [Fact]
    public void ObjectValuedDictionaryBindsItsEntries()
    {
        using TestDatabase db = new();
        Dictionary<string, object?> parameters = new() { ["@v"] = 41L };

        long? actual = db.ExecuteScalar<long?>("SELECT @v + 1", parameters);

        Assert.Equal(42L, actual);
    }

    [Fact]
    public void LongValuedDictionaryBindsItsEntries()
    {
        using TestDatabase db = new();
        Dictionary<string, long> parameters = new() { ["@v"] = 41L };

        long? actual = db.ExecuteScalar<long?>("SELECT @v + 1", parameters);

        Assert.Equal(42L, actual);
    }

    [Fact]
    public void StringValuedDictionaryBindsItsEntries()
    {
        using TestDatabase db = new();
        Dictionary<string, string> parameters = new() { ["@v"] = "abc" };

        string? actual = db.ExecuteScalar<string>("SELECT @v", parameters);

        Assert.Equal("abc", actual);
    }

    [Fact]
    public void DictionaryWithNonStringKeysIsRejected()
    {
        using TestDatabase db = new();
        Dictionary<int, string> parameters = new() { [1] = "abc" };

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => db.ExecuteScalar<string>("SELECT @v", parameters));

        Assert.Contains("must be strings", exception.Message);
    }

    [Fact]
    public void IntValuedDictionaryFiltersRows()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H21iDictRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");
        db.Execute("INSERT INTO \"H21iDictRows\" (\"Id\", \"Value\") VALUES (1, 10)");
        db.Execute("INSERT INTO \"H21iDictRows\" (\"Id\", \"Value\") VALUES (2, 20)");

        List<int> rows = [10, 20];
        List<int> expected = rows.Where(v => v == 20).ToList();

        Dictionary<string, int> parameters = new() { ["@v"] = 20 };
        List<int> actual = db.Query<int>("SELECT \"Value\" FROM \"H21iDictRows\" WHERE \"Value\" = @v", parameters);

        Assert.Equal(expected, actual);
    }
}
