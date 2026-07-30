using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25pLabels")]
public class H25pLabel
{
    [Key]
    public int Id { get; set; }

    public string Text { get; set; } = "";
}

public class HelperClassMethodTranslatorOverrideTests
{
    [Fact]
    public void APerMethodTranslatorReplacesTheBuiltInSQLiteFunctionsTranslation()
    {
        MethodInfo instr = typeof(SQLiteFunctions).GetMethod(nameof(SQLiteFunctions.Instr))!;

        using TestDatabase db = Setup(
            b => b.AddMethodTranslator(instr, SimpleTranslator.AsSimple(
                (_, args) => $"(LENGTH({args[0]}) + LENGTH({args[1]}))")),
            nameof(APerMethodTranslatorReplacesTheBuiltInSQLiteFunctionsTranslation));

        List<int> expected = Rows().OrderBy(r => r.Id).Select(r => r.Text.Length + 1).ToList();

        List<int> actual = db.Table<H25pLabel>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteFunctions.Instr(r.Text, "b"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RemovingTheHelperTypeEntryMakesItsMethodsUntranslatable()
    {
        using TestDatabase db = Setup(
            b => b.MemberTranslators.Remove(typeof(SQLiteFunctions)),
            nameof(RemovingTheHelperTypeEntryMakesItsMethodsUntranslatable));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H25pLabel>()
            .Select(r => SQLiteFunctions.Instr(r.Text, "b"))
            .ToList());

        Assert.Contains("was removed", ex.Message);
    }

    private static List<H25pLabel> Rows()
    {
        return
        [
            new H25pLabel { Id = 1, Text = "abc" },
            new H25pLabel { Id = 2, Text = "bcde" }
        ];
    }

    private static TestDatabase Setup(Action<SQLiteOptionsBuilder> configure, string methodName)
    {
        TestDatabase db = new(configure, methodName);
        db.Table<H25pLabel>().Schema.CreateTable();
        db.Table<H25pLabel>().AddRange(Rows());
        return db;
    }
}
