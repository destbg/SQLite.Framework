using SQLite.Framework.Internals;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TypeMetadataInternalTests
{
    [Fact]
    public void HasJsonConverterStripsNullableBeforeLookup()
    {
        using TestDatabase db = new();

        Assert.False(db.Options.HasJsonConverter(typeof(int?)));
        Assert.False(db.Options.HasJsonConverter(typeof(int)));
    }
}
