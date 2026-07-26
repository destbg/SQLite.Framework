using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RawIfaceValueRow
{
    public RawIfaceValueRow(int id, IComparable value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; init; }

    public IComparable Value { get; init; }
}

public class RawQueryInterfaceConstructorParameterTests
{
    [Fact]
    public void ARawQueryReadsAnInterfaceTypedConstructorParameter()
    {
        using TestDatabase db = new();

        List<string> actual = db
            .CreateCommand("SELECT 1 AS \"Id\", 'Ann' AS \"Value\" UNION ALL SELECT 2, 'Bob'", [])
            .ExecuteQuery<RawIfaceValueRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Value.ToString()!)
            .ToList();

        Assert.Equal(["Ann", "Bob"], actual);
    }
}
