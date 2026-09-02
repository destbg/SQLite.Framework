using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26qConstructedNodeJoinSources")]
public class H26qConstructedNodeJoinSource
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public int Value { get; set; }
}

[Table("H26qConstructedNodeJoinSides")]
public class H26qConstructedNodeJoinSide
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

public class H26qConstructedNodeJoinChild
{
    public int Value { get; set; }

    public int Preset { get; set; } = 17;
}

public class H26qConstructedNodeJoinOuter
{
    public int Key { get; set; }

    public H26qConstructedNodeJoinChild Child { get; set; } = null!;
}

public class ConstructedNodeJoinSourceParityTests
{
    [Fact]
    public void AnUnsetMemberOfAProjectedJoinSourceUsesItsDefaultValue()
    {
        using TestDatabase db = Setup(nameof(AnUnsetMemberOfAProjectedJoinSourceUsesItsDefaultValue));

        List<int> expected = Sides()
            .Join(
                Sources().Select(s => new H26qConstructedNodeJoinOuter
                {
                    Key = s.Key,
                    Child = new H26qConstructedNodeJoinChild { Value = s.Value }
                }),
                side => side.Key,
                outer => outer.Key,
                (side, outer) => outer.Child.Preset)
            .OrderBy(value => value)
            .ToList();

        List<int> actual = db.Table<H26qConstructedNodeJoinSide>()
            .Join(
                db.Table<H26qConstructedNodeJoinSource>().Select(s => new H26qConstructedNodeJoinOuter
                {
                    Key = s.Key,
                    Child = new H26qConstructedNodeJoinChild { Value = s.Value }
                }),
                side => side.Key,
                outer => outer.Key,
                (side, outer) => outer.Child.Preset)
            .OrderBy(value => value)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26qConstructedNodeJoinSource> Sources()
    {
        return
        [
            new H26qConstructedNodeJoinSource { Id = 1, Key = 1, Value = 3 },
            new H26qConstructedNodeJoinSource { Id = 2, Key = 2, Value = 7 }
        ];
    }

    private static List<H26qConstructedNodeJoinSide> Sides()
    {
        return
        [
            new H26qConstructedNodeJoinSide { Id = 1, Key = 1 },
            new H26qConstructedNodeJoinSide { Id = 2, Key = 2 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26qConstructedNodeJoinSource>().Schema.CreateTable();
        db.Table<H26qConstructedNodeJoinSide>().Schema.CreateTable();
        db.Table<H26qConstructedNodeJoinSource>().AddRange(Sources());
        db.Table<H26qConstructedNodeJoinSide>().AddRange(Sides());
        return db;
    }
}
