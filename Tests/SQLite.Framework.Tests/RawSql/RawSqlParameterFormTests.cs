using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RawSqlParameterFormTests
{
    [Fact]
    public void AnonymousObjectBindsColonPrefixedParameter()
    {
        using TestDatabase db = new();
        long? result = db.ExecuteScalar<long?>("SELECT :v + 1", new { v = 41L });
        Assert.Equal(42L, result);
    }

    [Fact]
    public void AnonymousObjectBindsDollarPrefixedParameter()
    {
        using TestDatabase db = new();
        long? result = db.ExecuteScalar<long?>("SELECT $v + 1", new { v = 41L });
        Assert.Equal(42L, result);
    }

    [Fact]
    public void ParameterNameWithoutPrefixBinds()
    {
        using TestDatabase db = new();
        long? result = db.ExecuteScalar<long?>("SELECT @v + 1", new SQLiteParameter { Name = "v", Value = 41L });
        Assert.Equal(42L, result);
    }
}
