using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H26lTagGroup
{
    public string Name { get; set; } = "";

    public List<int> Values { get; set; } = [];
}

[JsonSerializable(typeof(List<H26lTagGroup>))]
internal partial class H26lTagGroupContext : JsonSerializerContext;

[Table("H26lTagGroupRows")]
public class H26lTagGroupRow
{
    [Key]
    public int Id { get; set; }

    public List<H26lTagGroup> Groups { get; set; } = [];
}

public class JsonNestedListMemberLengthTests
{
    [Fact]
    public void CountingElementsWhoseNestedListHoldsMoreThanOneValueMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(CountingElementsWhoseNestedListHoldsMoreThanOneValueMatchesInMemory));

        int expected = Groups().Count(g => g.Values.Count > 1);
        int actual = db.Table<H26lTagGroupRow>()
            .Select(r => r.Groups.Count(g => g.Values.Count > 1))
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SummingTheLengthsOfTheNestedListsMatchesInMemory()
    {
        using TestDatabase db = Setup(nameof(SummingTheLengthsOfTheNestedListsMatchesInMemory));

        int expected = Groups().Sum(g => g.Values.Count);
        int actual = db.Table<H26lTagGroupRow>()
            .Select(r => r.Groups.Sum(g => g.Values.Count))
            .First();

        Assert.Equal(expected, actual);
    }

    private static List<H26lTagGroup> Groups()
    {
        return
        [
            new H26lTagGroup { Name = "a", Values = [1, 2] },
            new H26lTagGroup { Name = "b", Values = [3] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<H26lTagGroup>)] =
            new SQLiteJsonConverter<List<H26lTagGroup>>(H26lTagGroupContext.Default.ListH26lTagGroup), methodName);
        db.Table<H26lTagGroupRow>().Schema.CreateTable();
        db.Table<H26lTagGroupRow>().Add(new H26lTagGroupRow { Id = 1, Groups = Groups() });
        return db;
    }
}
