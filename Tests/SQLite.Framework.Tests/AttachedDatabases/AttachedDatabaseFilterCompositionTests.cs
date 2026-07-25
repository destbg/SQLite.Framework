using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20AttLocal")]
public class H20AttLocal
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Total { get; set; }
}

[Table("H20AttRemote")]
public class H20AttRemote
{
    [Key]
    public int Id { get; set; }

    public string Label { get; set; } = "";

    public bool IsDeleted { get; set; }
}

[Table("H20AttShared")]
public class H20AttShared
{
    [Key]
    public int Id { get; set; }

    public string Label { get; set; } = "";

    public bool IsDeleted { get; set; }

    public int Tenant { get; set; }
}

public class AttachedDatabaseFilterCompositionTests
{
    [Fact]
    public void TwoAttachedDatabasesApplyTheirOwnFiltersToSameEntityType()
    {
        using TestDatabase main = new();
        main.Table<H20AttLocal>().Schema.CreateTable();
        H20AttLocal[] locals =
        [
            new() { Id = 1, Name = "l1" },
            new() { Id = 2, Name = "l2" },
            new() { Id = 3, Name = "l3" },
        ];
        main.Table<H20AttLocal>().AddRange(locals);

        using TestDatabase aux1 = new(b => b.AddQueryFilter<H20AttShared>(r => !r.IsDeleted), useFile: true, "shared1");
        aux1.Table<H20AttShared>().Schema.CreateTable();
        H20AttShared[] rows1 =
        [
            new() { Id = 1, Label = "a", IsDeleted = false, Tenant = 2 },
            new() { Id = 2, Label = "b", IsDeleted = true, Tenant = 1 },
            new() { Id = 3, Label = "c", IsDeleted = false, Tenant = 1 },
        ];
        aux1.Table<H20AttShared>().AddRange(rows1);

        using TestDatabase aux2 = new(b => b.AddQueryFilter<H20AttShared>(r => r.Tenant == 1), useFile: true, "shared2");
        aux2.Table<H20AttShared>().Schema.CreateTable();
        H20AttShared[] rows2 =
        [
            new() { Id = 1, Label = "x", IsDeleted = true, Tenant = 1 },
            new() { Id = 2, Label = "y", IsDeleted = false, Tenant = 1 },
            new() { Id = 3, Label = "z", IsDeleted = false, Tenant = 2 },
        ];
        aux2.Table<H20AttShared>().AddRange(rows2);

        main.AttachDatabase(aux1, "a1");
        main.AttachDatabase(aux2, "a2");

        H20AttShared[] visible1 = rows1.Where(r => !r.IsDeleted).ToArray();
        H20AttShared[] visible2 = rows2.Where(r => r.Tenant == 1).ToArray();
        List<string> expected = (
            from l in locals
            join x in visible1 on l.Id equals x.Id
            join y in visible2 on l.Id equals y.Id
            select l.Name + x.Label + y.Label)
            .OrderBy(v => v)
            .ToList();

        List<string> actual = (
            from l in main.Table<H20AttLocal>()
            join x in aux1.Table<H20AttShared>() on l.Id equals x.Id
            join y in aux2.Table<H20AttShared>() on l.Id equals y.Id
            select l.Name + x.Label + y.Label)
            .ToList()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AttachedTableInsideCteBodyUsesAttachedFilters()
    {
        using TestDatabase main = new();
        main.Table<H20AttLocal>().Schema.CreateTable();
        H20AttLocal[] locals =
        [
            new() { Id = 1, Name = "l1" },
            new() { Id = 2, Name = "l2" },
        ];
        main.Table<H20AttLocal>().AddRange(locals);

        using TestDatabase aux = new(b => b.AddQueryFilter<H20AttRemote>(r => !r.IsDeleted), useFile: true, "cteaux");
        aux.Table<H20AttRemote>().Schema.CreateTable();
        H20AttRemote[] remotes =
        [
            new() { Id = 1, Label = "live", IsDeleted = false },
            new() { Id = 2, Label = "gone", IsDeleted = true },
        ];
        aux.Table<H20AttRemote>().AddRange(remotes);

        main.AttachDatabase(aux, "aux");

        H20AttRemote[] visible = remotes.Where(r => !r.IsDeleted).ToArray();
        List<string> expected = (
            from l in locals
            join r in visible on l.Id equals r.Id
            select l.Name + ":" + r.Label)
            .OrderBy(x => x)
            .ToList();

        SQLiteCte<H20AttRemote> cte = main.With(() => aux.Table<H20AttRemote>());
        List<string> actual = (
            from l in main.Table<H20AttLocal>()
            join r in cte on l.Id equals r.Id
            select l.Name + ":" + r.Label)
            .ToList()
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExecuteDeleteWithAttachedSubqueryUsesAttachedFilters()
    {
        using TestDatabase main = new();
        main.Table<H20AttLocal>().Schema.CreateTable();
        H20AttLocal[] locals =
        [
            new() { Id = 1, Name = "l1" },
            new() { Id = 2, Name = "l2" },
        ];
        main.Table<H20AttLocal>().AddRange(locals);

        using TestDatabase aux = new(b => b.AddQueryFilter<H20AttRemote>(r => !r.IsDeleted), useFile: true, "delaux");
        aux.Table<H20AttRemote>().Schema.CreateTable();
        H20AttRemote[] remotes =
        [
            new() { Id = 1, Label = "live", IsDeleted = false },
            new() { Id = 2, Label = "gone", IsDeleted = true },
        ];
        aux.Table<H20AttRemote>().AddRange(remotes);

        main.AttachDatabase(aux, "aux");

        H20AttRemote[] visible = remotes.Where(r => !r.IsDeleted).ToArray();
        List<int> expected = locals
            .Where(l => !visible.Any(r => r.Id == l.Id))
            .Select(l => l.Id)
            .OrderBy(i => i)
            .ToList();

        main.Table<H20AttLocal>().ExecuteDelete(l => aux.Table<H20AttRemote>().Any(r => r.Id == l.Id));

        List<int> actual = main.Table<H20AttLocal>().Select(l => l.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExecuteUpdateSetterWithAttachedSubqueryUsesAttachedFilters()
    {
        using TestDatabase main = new();
        main.Table<H20AttLocal>().Schema.CreateTable();
        H20AttLocal[] locals =
        [
            new() { Id = 1, Name = "l1", Total = 0 },
            new() { Id = 2, Name = "l2", Total = 0 },
        ];
        main.Table<H20AttLocal>().AddRange(locals);

        using TestDatabase aux = new(b => b.AddQueryFilter<H20AttRemote>(r => !r.IsDeleted), useFile: true, "setaux");
        aux.Table<H20AttRemote>().Schema.CreateTable();
        H20AttRemote[] remotes =
        [
            new() { Id = 1, Label = "live", IsDeleted = false },
            new() { Id = 2, Label = "gone", IsDeleted = true },
        ];
        aux.Table<H20AttRemote>().AddRange(remotes);

        main.AttachDatabase(aux, "aux");

        int visibleCount = remotes.Count(r => !r.IsDeleted);
        List<int> expected = locals.OrderBy(l => l.Id).Select(l => visibleCount).ToList();

        main.Table<H20AttLocal>().ExecuteUpdate(s => s.Set(l => l.Total, l => aux.Table<H20AttRemote>().Count()));

        List<int> actual = main.Table<H20AttLocal>().OrderBy(l => l.Id).Select(l => l.Total).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AttachedFilterBodyOverOtherAttachedSameTypeTableKeepsBothFilters()
    {
        using TestDatabase main = new();
        main.Table<H20AttLocal>().Schema.CreateTable();
        H20AttLocal[] locals =
        [
            new() { Id = 1, Name = "l1" },
            new() { Id = 2, Name = "l2" },
        ];
        main.Table<H20AttLocal>().AddRange(locals);

        using TestDatabase aux2 = new(b => b.AddQueryFilter<H20AttShared>(r => !r.IsDeleted), useFile: true, "bodyaux2");
        aux2.Table<H20AttShared>().Schema.CreateTable();
        H20AttShared[] rows2 =
        [
            new() { Id = 1, Label = "x", IsDeleted = false, Tenant = 1 },
            new() { Id = 2, Label = "y", IsDeleted = true, Tenant = 1 },
        ];
        aux2.Table<H20AttShared>().AddRange(rows2);

        using TestDatabase aux1 = new(b => b.AddQueryFilter<H20AttShared>(r => aux2.Table<H20AttShared>().Any(y => y.Id == r.Id)), useFile: true, "bodyaux1");
        aux1.Table<H20AttShared>().Schema.CreateTable();
        H20AttShared[] rows1 =
        [
            new() { Id = 1, Label = "a", IsDeleted = false, Tenant = 1 },
            new() { Id = 2, Label = "b", IsDeleted = false, Tenant = 1 },
        ];
        aux1.Table<H20AttShared>().AddRange(rows1);

        main.AttachDatabase(aux1, "a1");
        main.AttachDatabase(aux2, "a2");

        H20AttShared[] visible2 = rows2.Where(r => !r.IsDeleted).ToArray();
        List<int> expected = locals
            .Where(l => rows1.Where(r => visible2.Any(y => y.Id == r.Id)).Any(r => r.Id == l.Id))
            .Select(l => l.Id)
            .OrderBy(i => i)
            .ToList();

        List<int> actual = main.Table<H20AttLocal>()
            .Where(l => aux1.Table<H20AttShared>().Any(r => r.Id == l.Id))
            .Select(l => l.Id)
            .ToList()
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
