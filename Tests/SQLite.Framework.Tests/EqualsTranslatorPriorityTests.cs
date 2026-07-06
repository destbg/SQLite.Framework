using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("EqualsTranslatorItem")]
public class EqualsTranslatorItem
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }

    public string Name { get; set; } = "";
}

public class EqualsTranslatorWrapper
{
    public int N { get; set; }

    public bool Equals(int other)
    {
        return N == other * 1000;
    }
}

public class EqualsTranslatorPriorityTests
{
    [Fact]
    public void CustomStringEqualsTranslatorApplies()
    {
        MethodInfo equalsMethod = typeof(string).GetMethod(nameof(string.Equals), [typeof(string)])!;
        using TestDatabase db = new(b =>
        {
            b.MemberTranslators[equalsMethod] = SimpleTranslator.AsSimple(
                (instance, args) => $"(UPPER({instance}) = UPPER({args[0]}))");
        });
        db.Table<EqualsTranslatorItem>().Schema.CreateTable();
        db.Table<EqualsTranslatorItem>().Add(new EqualsTranslatorItem { Id = 1, Value = 0, Name = "a" });

        List<int> ids = db.Table<EqualsTranslatorItem>()
            .Where(r => r.Name.Equals("A"))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void CustomEqualsMethodOnOwnTypeTranslates()
    {
        MethodInfo equalsMethod = typeof(EqualsTranslatorWrapper).GetMethod(nameof(EqualsTranslatorWrapper.Equals), [typeof(int)])!;
        using TestDatabase db = new(b =>
        {
            b.MemberTranslators[equalsMethod] = SimpleTranslator.AsSimple(
                (instance, args) => $"({instance} = ({args[0]} * 1000))");
        });
        db.Table<EqualsTranslatorItem>().Schema.CreateTable();
        db.Table<EqualsTranslatorItem>().Add(new EqualsTranslatorItem { Id = 1, Value = 3000, Name = "a" });

        List<int> ids = db.Table<EqualsTranslatorItem>()
            .Where(r => new EqualsTranslatorWrapper { N = r.Value }.Equals(3))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], ids);
    }
}
