using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RawSqlObjectParameterTests
{
    [Fact]
    public void DictionaryParametersBindByKey()
    {
        using TestDatabase db = new();
        long? result = db.ExecuteScalar<long?>("SELECT @v + 1", new Dictionary<string, object> { ["v"] = 41L });
        Assert.Equal(42L, result);
    }

    [Fact]
    public void NullableDictionaryParametersBindByKey()
    {
        using TestDatabase db = new();
        long? result = db.ExecuteScalar<long?>("SELECT @v + 1", new Dictionary<string, object?> { ["v"] = 41L });
        Assert.Equal(42L, result);
    }
}
