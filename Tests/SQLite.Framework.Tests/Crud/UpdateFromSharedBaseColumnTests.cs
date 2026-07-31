using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H26oSharedLabelBase
{
    public string Label { get; set; } = "";
}

[Table("H26oSharedLabelTargets")]
public class H26oSharedLabelTarget : H26oSharedLabelBase
{
    [Key]
    public int Id { get; set; }

    public int SourceId { get; set; }
}

[Table("H26oSharedLabelSources")]
public class H26oSharedLabelSource : H26oSharedLabelBase
{
    [Key]
    public int Id { get; set; }
}

public class UpdateFromSharedBaseColumnTests
{
    [Fact]
    public void SettingTheJoinedRowsInheritedColumnIsRejectedAndLeavesTheTargetUntouched()
    {
        using TestDatabase db = Setup(nameof(SettingTheJoinedRowsInheritedColumnIsRejectedAndLeavesTheTargetUntouched));

        Assert.Throws<ArgumentException>(() =>
            db.Table<H26oSharedLabelTarget>()
                .Join(db.Table<H26oSharedLabelSource>(), t => t.SourceId, s => s.Id, (t, s) => new { t, s })
                .ExecuteUpdate(x => x.Set(p => p.s.Label, "written")));

        Assert.Equal("target-old", db.ExecuteScalar<string>("SELECT \"Label\" FROM \"H26oSharedLabelTargets\" WHERE \"Id\" = 1"));
        Assert.Equal("source-old", db.ExecuteScalar<string>("SELECT \"Label\" FROM \"H26oSharedLabelSources\" WHERE \"Id\" = 2"));
    }

    private static List<H26oSharedLabelTarget> Targets()
    {
        return
        [
            new H26oSharedLabelTarget { Id = 1, SourceId = 2, Label = "target-old" }
        ];
    }

    private static List<H26oSharedLabelSource> Sources()
    {
        return
        [
            new H26oSharedLabelSource { Id = 2, Label = "source-old" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26oSharedLabelSource>().Schema.CreateTable();
        db.Table<H26oSharedLabelTarget>().Schema.CreateTable();
        db.Table<H26oSharedLabelSource>().AddRange(Sources());
        db.Table<H26oSharedLabelTarget>().AddRange(Targets());
        return db;
    }
}
