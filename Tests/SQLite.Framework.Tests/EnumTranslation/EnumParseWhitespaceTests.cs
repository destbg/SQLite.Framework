using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class EnumParseWhitespaceTests
{
    [Fact]
    public void EnumParseTrimsLeadingTabLikeDotNet()
    {
        using TestDatabase db = new();
        db.Table<NullableStringEntity>().Schema.CreateTable();
        db.Table<NullableStringEntity>().Add(new NullableStringEntity { Id = 1, Name = "\tNewspaper" });

        PublisherType oracle = Enum.Parse<PublisherType>("\tNewspaper");
        PublisherType actual = db.Table<NullableStringEntity>()
            .Where(x => x.Id == 1)
            .Select(x => Enum.Parse<PublisherType>(x.Name!))
            .First();

        Assert.Equal(PublisherType.Newspaper, oracle);
        Assert.Equal(oracle, actual);
    }
}
