using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GenericTranslatorRegistrationRows")]
public class GenericTranslatorRegistrationRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }

    public string Name { get; set; } = "";
}

public static class GenericPairFunctions<T>
{
    public static string Tag(string s)
    {
        return "CLR-S";
    }

    public static string Tag(int i)
    {
        return "CLR-I";
    }
}

public static class GenericSuffixFunctions<T>
{
    public static string Apply<U>(U value)
    {
        return "CLR-APPLY";
    }
}

public class GenericBoxHolder<T>;

public static class GenericBoxHolderExtensions
{
    public static string Fmt<T>(this GenericBoxHolder<T> box, int value)
    {
        return "CLR-EXT";
    }
}

public class GenericTypeTranslatorRegistrationTests
{
    [Fact]
    public void EachRegisteredOverloadAppliesToItsOwnCall()
    {
        MethodInfo stringOverload = typeof(GenericPairFunctions<>).GetMethods()
            .Single(m => m.Name == "Tag" && m.GetParameters()[0].ParameterType == typeof(string));
        MethodInfo intOverload = typeof(GenericPairFunctions<>).GetMethods()
            .Single(m => m.Name == "Tag" && m.GetParameters()[0].ParameterType == typeof(int));

        using TestDatabase db = new(b =>
        {
            b.MemberTranslators[stringOverload] = SimpleTranslator.AsSimple((_, _) => "'SQL-S'");
            b.MemberTranslators[intOverload] = SimpleTranslator.AsSimple((_, _) => "'SQL-I'");
        });
        db.Table<GenericTranslatorRegistrationRow>().Schema.CreateTable();
        db.Table<GenericTranslatorRegistrationRow>().Add(new GenericTranslatorRegistrationRow { Id = 1, Value = 5, Name = "n" });

        List<string> fromInt = db.Table<GenericTranslatorRegistrationRow>()
            .Select(r => GenericPairFunctions<int>.Tag(r.Value))
            .ToList();
        List<string> fromString = db.Table<GenericTranslatorRegistrationRow>()
            .Select(r => GenericPairFunctions<int>.Tag(r.Name))
            .ToList();

        Assert.Equal(["SQL-I"], fromInt);
        Assert.Equal(["SQL-S"], fromString);
    }

    [Fact]
    public void GenericMethodOnGenericTypeUsesRegisteredTranslator()
    {
        MethodInfo apply = typeof(GenericSuffixFunctions<>).GetMethods()
            .Single(m => m.Name == "Apply");

        using TestDatabase db = new(b =>
        {
            b.MemberTranslators[apply] = SimpleTranslator.AsSimple((_, _) => "'SQL-APPLY'");
        });
        db.Table<GenericTranslatorRegistrationRow>().Schema.CreateTable();
        db.Table<GenericTranslatorRegistrationRow>().Add(new GenericTranslatorRegistrationRow { Id = 1, Value = 5, Name = "n" });

        List<string> values = db.Table<GenericTranslatorRegistrationRow>()
            .Select(r => GenericSuffixFunctions<long>.Apply(r.Value))
            .ToList();

        Assert.Equal(["SQL-APPLY"], values);
    }

    [Fact]
    public void ExtensionMethodOnConstructedGenericReceiverUsesRegisteredTranslator()
    {
        MethodInfo fmt = typeof(GenericBoxHolderExtensions).GetMethod(nameof(GenericBoxHolderExtensions.Fmt))!;

        using TestDatabase db = new(b =>
        {
            b.MemberTranslators[fmt] = ctx => SQLiteExpression.Leaf(
                typeof(string), ctx.Counters.NextIdentifier(), "'SQL-EXT'", (SQLiteParameter[]?)null);
        });
        db.Table<GenericTranslatorRegistrationRow>().Schema.CreateTable();
        db.Table<GenericTranslatorRegistrationRow>().Add(new GenericTranslatorRegistrationRow { Id = 1, Value = 5, Name = "n" });

        GenericBoxHolder<int> box = new();
        List<string> values = db.Table<GenericTranslatorRegistrationRow>()
            .Select(r => box.Fmt(r.Value))
            .ToList();

        Assert.Equal(["SQL-EXT"], values);
    }
}
