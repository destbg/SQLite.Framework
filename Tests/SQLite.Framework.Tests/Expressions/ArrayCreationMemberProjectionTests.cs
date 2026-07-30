using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26hArrayMemberRows")]
public class H26hArrayMemberRow
{
    [Key]
    public int Id { get; set; }
}

public class ArrayCreationMemberProjectionTests
{
    [Fact]
    public void LongLengthOfAnArrayBuiltInTheProjectionMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(LongLengthOfAnArrayBuiltInTheProjectionMatchesLinq));

        List<long> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new long[] { r.Id, 5 }.LongLength)
            .ToList();

        Assert.Equal([2, 2], expected);

        List<long> actual = db.Table<H26hArrayMemberRow>()
            .OrderBy(r => r.Id)
            .Select(r => new long[] { r.Id, 5 }.LongLength)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26hArrayMemberRow> Rows()
    {
        return
        [
            new H26hArrayMemberRow { Id = 1 },
            new H26hArrayMemberRow { Id = 2 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26hArrayMemberRow>().Schema.CreateTable();
        db.Table<H26hArrayMemberRow>().AddRange(Rows());
        return db;
    }
}
