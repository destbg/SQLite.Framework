using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PersonTagNode
{
    public List<string> Tags { get; set; } = [];
}

public class GroupNode
{
    public List<ItemNode> Items { get; set; } = [];
}

public class ItemNode
{
    public List<int> Vals { get; set; } = [];
}

[JsonSerializable(typeof(List<PersonTagNode>))]
[JsonSerializable(typeof(List<GroupNode>))]
internal partial class NestedNodeContext : JsonSerializerContext;

internal sealed class TagOwnerRow
{
    [Key]
    public int Id { get; set; }

    public List<PersonTagNode> People { get; set; } = [];
}

internal sealed class GroupOwnerRow
{
    [Key]
    public int Id { get; set; }

    public List<GroupNode> Groups { get; set; } = [];
}

public class JsonSelectManyChainParityTests
{
    [Fact]
    public void SelectManyThenLastMatchesLinq()
    {
        List<PersonTagNode> local =
        [
            new PersonTagNode { Tags = ["a", "b", "c"] },
            new PersonTagNode { Tags = ["d"] },
        ];
        using TestDatabase db = new(b => b.AddJsonContext(NestedNodeContext.Default));
        db.Table<TagOwnerRow>().Schema.CreateTable();
        db.Table<TagOwnerRow>().Add(new TagOwnerRow { Id = 1, People = local });

        string expected = local.SelectMany(p => p.Tags).Last();
        string actual = db.Table<TagOwnerRow>().Select(r => r.People.SelectMany(p => p.Tags).Last()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyThenReverseMatchesLinq()
    {
        List<PersonTagNode> local =
        [
            new PersonTagNode { Tags = ["a", "b", "c"] },
            new PersonTagNode { Tags = ["d"] },
        ];
        using TestDatabase db = new(b => b.AddJsonContext(NestedNodeContext.Default));
        db.Table<TagOwnerRow>().Schema.CreateTable();
        db.Table<TagOwnerRow>().Add(new TagOwnerRow { Id = 1, People = local });

        List<string> expected = local.SelectMany(p => p.Tags).Reverse().ToList();
        List<string> actual = db.Table<TagOwnerRow>().Select(r => r.People.SelectMany(p => p.Tags).Reverse().ToList()).First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChainedSelectManyCountMatchesLinq()
    {
        List<GroupNode> local =
        [
            new GroupNode { Items = [new ItemNode { Vals = [1, 2] }, new ItemNode { Vals = [3] }] },
            new GroupNode { Items = [new ItemNode { Vals = [4, 5, 6] }] },
        ];
        using TestDatabase db = new(b => b.AddJsonContext(NestedNodeContext.Default));
        db.Table<GroupOwnerRow>().Schema.CreateTable();
        db.Table<GroupOwnerRow>().Add(new GroupOwnerRow { Id = 1, Groups = local });

        int expected = local.SelectMany(g => g.Items).SelectMany(i => i.Vals).Count();
        int actual = db.Table<GroupOwnerRow>().Select(r => r.Groups.SelectMany(g => g.Items).SelectMany(i => i.Vals).Count()).First();

        Assert.Equal(expected, actual);
    }
}
