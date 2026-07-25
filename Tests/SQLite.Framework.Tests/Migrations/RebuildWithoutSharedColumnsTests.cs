using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SwapAllColumns")]
public class SwapAllColumnsRow
{
    public int? Fresh { get; set; }
}

public class RebuildWithoutSharedColumnsTests
{
    [Fact]
    public void RebuildKeepsTheSameRowCountAsInPlaceWhenNoColumnIsShared()
    {
        long expected;
        using (TestDatabase inPlace = new())
        {
            inPlace.Execute("CREATE TABLE \"SwapAllColumns\" (\"Legacy\" TEXT)");
            inPlace.Execute("INSERT INTO \"SwapAllColumns\" (\"Legacy\") VALUES ('x'), ('y')");
            inPlace.Schema.Migrations()
                .Version(1, m => m.TableChanged<SwapAllColumnsRow>())
                .Migrate();
            expected = inPlace.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SwapAllColumns\"");
        }

        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"SwapAllColumns\" (\"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"SwapAllColumns\" (\"Legacy\") VALUES ('x'), ('y')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SwapAllColumnsRow>(rebuild: true))
            .Migrate();

        long count = db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"SwapAllColumns\"");
        Assert.Equal(expected, count);
    }
}
