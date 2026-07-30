using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H25iLabelledBase
{
    public string Label { get; set; } = "";
}

[Table("H25iLabelledTargets")]
public class H25iLabelledTarget : H25iLabelledBase
{
    [Key]
    public int Id { get; set; }

    public int SourceId { get; set; }
}

[Table("H25iLabelledSources")]
public class H25iLabelledSource
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class UpdateFromInheritedColumnTests
{
    [Fact]
    public void UpdateFromCopiesTheJoinedValueIntoAnInheritedColumn()
    {
        using TestDatabase db = Setup(nameof(UpdateFromCopiesTheJoinedValueIntoAnInheritedColumn));

        int affected = db.Table<H25iLabelledTarget>()
            .Join(db.Table<H25iLabelledSource>(), t => t.SourceId, s => s.Id, (t, s) => new { t, s })
            .ExecuteUpdate(x => x.Set(p => p.t.Label, p => p.s.Name));

        List<string> expected = Targets()
            .Join(Sources(), t => t.SourceId, s => s.Id, (t, s) => s.Name)
            .ToList();

        Assert.Equal(expected.Count, affected);
        Assert.Equal("beta", db.ExecuteScalar<string>("SELECT \"Label\" FROM \"H25iLabelledTargets\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void UpdateFromSetsAnInheritedColumnToAConstant()
    {
        using TestDatabase db = Setup(nameof(UpdateFromSetsAnInheritedColumnToAConstant));

        db.Table<H25iLabelledTarget>()
            .Join(db.Table<H25iLabelledSource>(), t => t.SourceId, s => s.Id, (t, s) => new { t, s })
            .ExecuteUpdate(x => x.Set(p => p.t.Label, "marked"));

        Assert.Equal("marked", db.ExecuteScalar<string>("SELECT \"Label\" FROM \"H25iLabelledTargets\" WHERE \"Id\" = 1"));
    }

    private static List<H25iLabelledTarget> Targets()
    {
        return
        [
            new H25iLabelledTarget { Id = 1, SourceId = 2, Label = "old" }
        ];
    }

    private static List<H25iLabelledSource> Sources()
    {
        return
        [
            new H25iLabelledSource { Id = 2, Name = "beta" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25iLabelledSource>().Schema.CreateTable();
        db.Table<H25iLabelledTarget>().Schema.CreateTable();
        db.Table<H25iLabelledSource>().AddRange(Sources());
        db.Table<H25iLabelledTarget>().AddRange(Targets());
        return db;
    }
}
