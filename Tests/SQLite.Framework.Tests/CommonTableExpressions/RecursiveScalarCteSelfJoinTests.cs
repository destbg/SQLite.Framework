using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RecScalarStep")]
public class RecScalarStepRow
{
    [Key]
    public int Id { get; set; }
}

public class RecursiveScalarCteSelfJoinTests
{
    [Fact]
    public void ScalarRecursiveCteJoinedWithTableWalks()
    {
        using TestDatabase db = new();
        db.Table<RecScalarStepRow>().Schema.CreateTable();
        db.Table<RecScalarStepRow>().Add(new RecScalarStepRow { Id = 1 });
        db.Table<RecScalarStepRow>().Add(new RecScalarStepRow { Id = 2 });

        SQLiteCte<int> nums = db.WithRecursive<int>(self =>
            db.Values(1).Concat(
                from b in db.Table<RecScalarStepRow>()
                join n in self on b.Id equals n
                select n + 1));

        List<int> actual = (from n in nums select n).ToList().OrderBy(x => x).ToList();

        Assert.Equal([1, 2, 3], actual);
    }
}
