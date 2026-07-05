using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CrossDbItem")]
public class CrossDbItemRow
{
    [Key]
    public int Id { get; set; }
}

public class CrossDatabaseCapturedQueryableTests
{
    [Fact]
    public void ContainsSubqueryOverOtherDatabaseDoesNotReadCurrentDatabase()
    {
        using TestDatabase db1 = new();
        using TestDatabase db2 = new();
        db1.Table<CrossDbItemRow>().Schema.CreateTable();
        db2.Table<CrossDbItemRow>().Schema.CreateTable();
        db1.Table<CrossDbItemRow>().Add(new CrossDbItemRow { Id = 1 });
        db1.Table<CrossDbItemRow>().Add(new CrossDbItemRow { Id = 2 });
        db2.Table<CrossDbItemRow>().Add(new CrossDbItemRow { Id = 2 });
        db2.Table<CrossDbItemRow>().Add(new CrossDbItemRow { Id = 3 });

        Assert.Throws<NotSupportedException>(() => db1.Table<CrossDbItemRow>()
            .Where(b => db2.Table<CrossDbItemRow>().Select(f => f.Id).Contains(b.Id))
            .Select(b => b.Id)
            .ToList());
    }
}
