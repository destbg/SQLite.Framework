using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GenericOverloadTranslatorItem")]
public class GenericOverloadTranslatorItem
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public static class GenericOverloadFunctions<T>
{
    public static string Pick(string s)
    {
        return "CLR-STRING";
    }

    public static string Pick(int i)
    {
        return "CLR-INT";
    }
}

public class GenericTypeMethodOverloadTranslatorTests
{
    [Fact]
    public void RegisteredStringOverloadDoesNotApplyToIntOverload()
    {
        MethodInfo stringOverload = typeof(GenericOverloadFunctions<>).GetMethods()
            .Single(m => m.Name == nameof(GenericOverloadFunctions<int>.Pick)
                && m.GetParameters()[0].ParameterType == typeof(string));

        using TestDatabase db = new(b =>
        {
            b.MemberTranslators[stringOverload] = SimpleTranslator.AsSimple((_, _) => "'SQL-STRING'");
        });
        db.Table<GenericOverloadTranslatorItem>().Schema.CreateTable();
        db.Table<GenericOverloadTranslatorItem>().Add(new GenericOverloadTranslatorItem { Id = 1, Value = 5 });

        List<string> values = db.Table<GenericOverloadTranslatorItem>()
            .Select(r => GenericOverloadFunctions<int>.Pick(r.Value))
            .ToList();

        Assert.Equal(["CLR-INT"], values);
    }
}
