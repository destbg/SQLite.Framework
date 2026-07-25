using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class HiddenExtraBase
{
    public int Extra { get; set; }
}

[Table("HiddenExtra")]
public class HiddenExtraRow : HiddenExtraBase
{
    [Key]
    public int Id { get; set; }

    [NotMapped]
    public new int Extra { get; set; }
}

public class NotMappedHidingPropertyTests
{
    [Fact]
    public void NotMappedOnAHidingPropertyExcludesTheColumn()
    {
        using TestDatabase db = new();
        db.Table<HiddenExtraRow>().Schema.CreateTable();

        List<string> columns = db.Pragmas.TableInfo("HiddenExtra").Select(c => c.Name).ToList();

        Assert.Equal(["Id"], columns);
    }
}
