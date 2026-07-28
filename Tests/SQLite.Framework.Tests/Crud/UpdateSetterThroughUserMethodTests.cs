using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23oUpdateSetterRows")]
public class H23oUpdateSetterRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23oUpdateSetterFns
{
    public static string Decorate(string value)
    {
        return "[" + value + "]";
    }
}

public class UpdateSetterThroughUserMethodTests
{
    [Fact]
    public void SetterThroughAUserMethodReportsATranslationError()
    {
        using TestDatabase db = Setup(nameof(SetterThroughAUserMethodReportsATranslationError));

        Exception? failure = Record.Exception(() => db.Table<H23oUpdateSetterRow>()
            .Where(r => r.Id > 0)
            .ExecuteUpdate(s => s.Set(r => r.Name, r => H23oUpdateSetterFns.Decorate(r.Name))));

        Assert.NotNull(failure);
        Assert.NotEqual(typeof(InvalidCastException), failure.GetType());
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23oUpdateSetterRow>().Schema.CreateTable();
        db.Table<H23oUpdateSetterRow>().Add(new H23oUpdateSetterRow { Id = 1, Name = "alpha" });
        return db;
    }
}
