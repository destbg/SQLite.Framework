using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TranslatorScopeItem")]
public class TranslatorScopeItem
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }

    public string Name { get; set; } = "";

    public byte[]? Data { get; set; }
}

public class TwoPropEqualsWrapper
{
    public int A { get; set; }

    public int B { get; set; }

    public bool Equals(int other)
    {
        return A + B == other;
    }
}

public class ListBindingEqualsWrapper
{
    public List<int> Items { get; } = [];

    public bool Equals(int other)
    {
        return Items.Count == other;
    }
}

public class HolderEqualsWrapper
{
    public TwoPropEqualsWrapper? Inner { get; set; }

    public bool Equals(int other)
    {
        return Inner!.A + Inner.B == other;
    }
}

public class MethodTranslatorPriorityScopeTests
{
    private static List<TranslatorScopeItem> Rows() =>
    [
        new() { Id = 1, Value = 3, Name = "a", Data = [1, 2] },
        new() { Id = 2, Value = 5, Name = "b", Data = [1, 2, 3] },
    ];

    private static TestDatabase Seed(TestDatabase db)
    {
        db.Table<TranslatorScopeItem>().Schema.CreateTable();
        db.Table<TranslatorScopeItem>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void StaticObjectEqualsTranslatorApplies()
    {
        MethodInfo equalsMethod = typeof(object).GetMethod(nameof(object.Equals), [typeof(object), typeof(object)])!;
        using TestDatabase db = Seed(new TestDatabase(b =>
        {
            b.MemberTranslators[equalsMethod] = SimpleTranslator.AsSimple(
                (_, args) => $"(UPPER({args[0]}) = UPPER({args[1]}))");
        }));

        List<int> ids = db.Table<TranslatorScopeItem>()
            .Where(r => object.Equals(r.Name, "A"))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void CustomEqualsTranslatorAppliesInSelectProjection()
    {
        MethodInfo equalsMethod = typeof(string).GetMethod(nameof(string.Equals), [typeof(string)])!;
        using TestDatabase db = Seed(new TestDatabase(b =>
        {
            b.MemberTranslators[equalsMethod] = SimpleTranslator.AsSimple(
                (instance, args) => $"(UPPER({instance}) = UPPER({args[0]}))");
        }));

        List<bool> flags = db.Table<TranslatorScopeItem>()
            .OrderBy(r => r.Id)
            .Select(r => r.Name.Equals("A"))
            .ToList();

        Assert.Equal([true, false], flags);
    }

    [Fact]
    public void SequenceEqualMethodTranslatorApplies()
    {
        MethodInfo sequenceEqual = typeof(Enumerable).GetMethods()
            .Single(m => m.Name == nameof(Enumerable.SequenceEqual) && m.GetParameters().Length == 2);
        using TestDatabase db = Seed(new TestDatabase(b =>
        {
            b.MemberTranslators[sequenceEqual] = SimpleTranslator.AsSimple(
                (_, args) => $"(LENGTH({args[0]}) = LENGTH({args[1]}))");
        }));

        List<int> ids = db.Table<TranslatorScopeItem>()
            .Where(r => Enumerable.SequenceEqual(r.Data!, new byte[] { 9, 9 }))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void SequenceEqualWithoutTranslatorComparesBlobs()
    {
        using TestDatabase db = Seed(new TestDatabase(nameof(SequenceEqualWithoutTranslatorComparesBlobs)));

        List<int> ids = db.Table<TranslatorScopeItem>()
            .Where(r => r.Data!.SequenceEqual(new byte[] { 1, 2 }))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void MultiBindingWrapperEqualsComputesInMemory()
    {
        MethodInfo equalsMethod = typeof(TwoPropEqualsWrapper).GetMethod(nameof(TwoPropEqualsWrapper.Equals), [typeof(int)])!;
        using TestDatabase db = Seed(new TestDatabase(b =>
        {
            b.MemberTranslators[equalsMethod] = SimpleTranslator.AsSimple(
                (instance, args) => $"({instance} = {args[0]})");
        }));

        List<bool> expected = Rows().Select(r => new TwoPropEqualsWrapper { A = r.Value, B = r.Value }.Equals(6)).ToList();
        List<bool> actual = db.Table<TranslatorScopeItem>()
            .OrderBy(r => r.Id)
            .Select(r => new TwoPropEqualsWrapper { A = r.Value, B = r.Value }.Equals(6))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ListBindingWrapperEqualsComputesInMemory()
    {
        MethodInfo equalsMethod = typeof(ListBindingEqualsWrapper).GetMethod(nameof(ListBindingEqualsWrapper.Equals), [typeof(int)])!;
        using TestDatabase db = Seed(new TestDatabase(b =>
        {
            b.MemberTranslators[equalsMethod] = SimpleTranslator.AsSimple(
                (instance, args) => $"({instance} = {args[0]})");
        }));

        List<bool> expected = Rows().Select(r => new ListBindingEqualsWrapper { Items = { r.Value } }.Equals(1)).ToList();
        List<bool> actual = db.Table<TranslatorScopeItem>()
            .OrderBy(r => r.Id)
            .Select(r => new ListBindingEqualsWrapper { Items = { r.Value } }.Equals(1))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedInitWrapperEqualsComputesInMemory()
    {
        MethodInfo equalsMethod = typeof(HolderEqualsWrapper).GetMethod(nameof(HolderEqualsWrapper.Equals), [typeof(int)])!;
        using TestDatabase db = Seed(new TestDatabase(b =>
        {
            b.MemberTranslators[equalsMethod] = SimpleTranslator.AsSimple(
                (instance, args) => $"({instance} = {args[0]})");
        }));

        List<bool> expected = Rows().Select(r => new HolderEqualsWrapper { Inner = new TwoPropEqualsWrapper { A = r.Value, B = 1 } }.Equals(4)).ToList();
        List<bool> actual = db.Table<TranslatorScopeItem>()
            .OrderBy(r => r.Id)
            .Select(r => new HolderEqualsWrapper { Inner = new TwoPropEqualsWrapper { A = r.Value, B = 1 } }.Equals(4))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnregisteredEqualsStillTranslatesBuiltIn()
    {
        using TestDatabase db = Seed(new TestDatabase(nameof(UnregisteredEqualsStillTranslatesBuiltIn)));

        List<int> ids = db.Table<TranslatorScopeItem>()
            .Where(r => r.Value.Equals(3))
            .Select(r => r.Id)
            .ToList();

        Assert.Equal([1], ids);
    }
}
