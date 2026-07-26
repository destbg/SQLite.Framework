using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NestedParameterDictionaryBindingTests
{
    [Fact]
    public void ADictionaryValueThatIsAlreadyAParameterBindsItsInnerValue()
    {
        using TestDatabase db = new();
        Dictionary<string, SQLiteParameter> parameters = new()
        {
            ["@v"] = new SQLiteParameter { Name = "@v", Value = 41L }
        };

        long? actual = db.ExecuteScalar<long?>("SELECT @v + 1", parameters);

        Assert.Equal(42L, actual);
    }

    [Fact]
    public void ATypedDictionaryValueThatIsNotAParameterBindsAsIs()
    {
        using TestDatabase db = new();
        Dictionary<string, long> parameters = new() { ["@v"] = 41L };

        long? actual = db.ExecuteScalar<long?>("SELECT @v + 1", parameters);

        Assert.Equal(42L, actual);
    }

    [Fact]
    public void ADictionaryWithANonStringKeyThrows()
    {
        using TestDatabase db = new();
        System.Collections.Hashtable parameters = new() { [1] = 41L };

        Assert.Throws<ArgumentException>(() => db.ExecuteScalar<long?>("SELECT @v + 1", parameters));
    }
}
