using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23oJoinLeftRows")]
public class H23oJoinLeftRow
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

[Table("H23oJoinRightRows")]
public class H23oJoinRightRow
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

public static class H23oJoinFns
{
    public static string Normalize(string value)
    {
        return value.Trim();
    }
}

public class JoinKeyThroughUserMethodTests
{
    [Fact]
    public void ScalarJoinKeyThroughAUserMethodReportsATranslationError()
    {
        using TestDatabase db = Setup(nameof(ScalarJoinKeyThroughAUserMethodReportsATranslationError));

        Exception? failure = Record.Exception(() => db.Table<H23oJoinLeftRow>()
            .Join(
                db.Table<H23oJoinRightRow>(),
                l => H23oJoinFns.Normalize(l.Code),
                r => r.Code,
                (l, r) => l.Id)
            .ToList());

        Assert.NotNull(failure);
        Assert.NotEqual(typeof(InvalidCastException), failure.GetType());
    }

    [Fact]
    public void CompositeJoinKeyThroughAUserMethodReportsATranslationError()
    {
        using TestDatabase db = Setup(nameof(CompositeJoinKeyThroughAUserMethodReportsATranslationError));

        Exception? failure = Record.Exception(() => db.Table<H23oJoinLeftRow>()
            .Join(
                db.Table<H23oJoinRightRow>(),
                l => new { Key = H23oJoinFns.Normalize(l.Code), l.Id },
                r => new { Key = r.Code, r.Id },
                (l, r) => l.Id)
            .ToList());

        Assert.NotNull(failure);
        Assert.NotEqual(typeof(InvalidCastException), failure.GetType());
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23oJoinLeftRow>().Schema.CreateTable();
        db.Table<H23oJoinRightRow>().Schema.CreateTable();
        db.Table<H23oJoinLeftRow>().Add(new H23oJoinLeftRow { Id = 1, Code = "a" });
        db.Table<H23oJoinRightRow>().Add(new H23oJoinRightRow { Id = 1, Code = "a" });
        return db;
    }
}
