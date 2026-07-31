using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H26fStampedBase
{
    public string Stamp { get; set; } = "";
}

[Table("H26fStampTargets")]
public class H26fStampTarget : H26fStampedBase
{
    [Key]
    public int Id { get; set; }

    public int SourceId { get; set; }
}

[Table("H26fStampSources")]
public class H26fStampSource : H26fStampedBase
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class UpdateFromJoinedSourceInheritedColumnTests
{
    [Fact]
    public void SettingAnInheritedColumnOnTheJoinedSourceIsRejected()
    {
        using TestDatabase db = Setup(nameof(SettingAnInheritedColumnOnTheJoinedSourceIsRejected));

        Assert.Throws<ArgumentException>(() => db.Table<H26fStampTarget>()
            .Join(db.Table<H26fStampSource>(), t => t.SourceId, s => s.Id, (t, s) => new { t, s })
            .ExecuteUpdate(x => x.Set(p => p.s.Stamp, "written")));
    }

    [Fact]
    public void SettingAnInheritedColumnOnTheJoinedSourceLeavesTheTargetRowAlone()
    {
        using TestDatabase db = Setup(nameof(SettingAnInheritedColumnOnTheJoinedSourceLeavesTheTargetRowAlone));

        try
        {
            db.Table<H26fStampTarget>()
                .Join(db.Table<H26fStampSource>(), t => t.SourceId, s => s.Id, (t, s) => new { t, s })
                .ExecuteUpdate(x => x.Set(p => p.s.Stamp, "written"));
        }
        catch (ArgumentException)
        {
        }

        Assert.Equal("target", db.ExecuteScalar<string>("SELECT \"Stamp\" FROM \"H26fStampTargets\" WHERE \"Id\" = 1"));
    }

    private static List<H26fStampTarget> Targets()
    {
        return
        [
            new H26fStampTarget { Id = 1, SourceId = 2, Stamp = "target" }
        ];
    }

    private static List<H26fStampSource> Sources()
    {
        return
        [
            new H26fStampSource { Id = 2, Name = "beta", Stamp = "source" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26fStampSource>().Schema.CreateTable();
        db.Table<H26fStampTarget>().Schema.CreateTable();
        db.Table<H26fStampSource>().AddRange(Sources());
        db.Table<H26fStampTarget>().AddRange(Targets());
        return db;
    }
}
