using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RawScalarNullParityTests
{
    [Fact]
    public void Query_NullValueTypeScalar_ReturnsDefault()
    {
        using TestDatabase db = new();

        List<int> actual = db.Query<int>("SELECT NULL");

        Assert.Equal([0], actual);
    }

    [Fact]
    public void QueryFirst_NullValueTypeScalar_ReturnsDefault()
    {
        using TestDatabase db = new();

        int actual = db.QueryFirst<int>("SELECT NULL");

        Assert.Equal(0, actual);
    }

    [Fact]
    public void QuerySingle_NullValueTypeScalar_ReturnsDefault()
    {
        using TestDatabase db = new();

        int actual = db.QuerySingle<int>("SELECT NULL");

        Assert.Equal(0, actual);
    }

    [Fact]
    public void QueryFirst_NullValueTypeScalar_MatchesExecuteScalar()
    {
        using TestDatabase db = new();

        int scalar = db.ExecuteScalar<int>("SELECT NULL");
        int first = db.QueryFirst<int>("SELECT NULL");

        Assert.Equal(scalar, first);
    }
}
