using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("TotaledGroup")]
public class TotaledGroupRow
{
    [Key]
    public int Id { get; set; }

    public int GroupNo { get; set; }

    public int Total { get; set; }
}

public class ExecuteUpdateSetterParameterReservationTests
{
    [Fact]
    public void UserParameterInsideASetterSubqueryKeepsItsValue()
    {
        using TestDatabase db = new();
        db.Table<TotaledGroupRow>().Schema.CreateTable();
        db.Table<TotaledGroupRow>().Add(new TotaledGroupRow { Id = 1, GroupNo = 9, Total = 0 });
        db.Execute("CREATE TABLE \"CountedSrc\" (\"K\" INTEGER)");
        db.Execute("INSERT INTO \"CountedSrc\" (\"K\") VALUES (5), (5), (5), (9)");

        int g = 9;
        db.Table<TotaledGroupRow>().Where(t => t.GroupNo == g).ExecuteUpdate(s => s.Set(
            t => t.Total,
            t => db.FromSql<int>("SELECT COUNT(*) FROM \"CountedSrc\" WHERE \"K\" = @p0", new SQLiteParameter { Name = "@p0", Value = 5 }).First()));

        Assert.Equal(3, db.Table<TotaledGroupRow>().Single().Total);
    }
}
