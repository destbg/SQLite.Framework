using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("LjVariantParents")]
public class LjVariantParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("LjVariantChildren")]
public class LjVariantChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Label { get; set; } = "";
}

public class LjVariantRowDto
{
    public int Id { get; set; }

    public string Label { get; set; } = "";
}

public class LeftJoinNestedProjectionVariantsTests
{
    private static List<LjVariantParent> Parents()
    {
        return
        [
            new LjVariantParent { Id = 1, Name = "Ann" },
            new LjVariantParent { Id = 2, Name = "Bob" },
        ];
    }

    private static List<LjVariantChild> Children()
    {
        return
        [
            new LjVariantChild { Id = 1, ParentId = 1, Label = "C1" },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<LjVariantParent>().Schema.CreateTable();
        db.Table<LjVariantParent>().AddRange(Parents());
        db.Table<LjVariantChild>().Schema.CreateTable();
        db.Table<LjVariantChild>().AddRange(Children());
        return db;
    }

    [Fact]
    public void ThreeLevelNestedAnonymousAfterDefaultIfEmpty()
    {
        using TestDatabase db = Seed();
        List<LjVariantParent> parents = Parents();
        List<LjVariantChild> children = Children();

        var expected = (from p in parents
                        join c in children on p.Id equals c.ParentId into grp
                        from c in grp.DefaultIfEmpty()
                        select new { L1 = new { L2 = new { p.Id, p.Name } }, Label = c == null ? "none" : c.Label })
            .OrderBy(x => x.L1.L2.Id).ToList();
        var actual = (from p in db.Table<LjVariantParent>()
                      join c in db.Table<LjVariantChild>() on p.Id equals c.ParentId into grp
                      from c in grp.DefaultIfEmpty()
                      select new { L1 = new { L2 = new { p.Id, p.Name } }, Label = c == null ? "none" : c.Label })
            .ToList().OrderBy(x => x.L1.L2.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CrossJoinNestedAnonymousProjection()
    {
        using TestDatabase db = Seed();
        List<LjVariantParent> parents = Parents();
        List<LjVariantChild> children = Children();

        var expected = (from p in parents
                        from c in children
                        select new { Outer = new { p.Id, p.Name }, c.Label })
            .OrderBy(x => x.Outer.Id).ToList();
        var actual = (from p in db.Table<LjVariantParent>()
                      from c in db.Table<LjVariantChild>()
                      select new { Outer = new { p.Id, p.Name }, c.Label })
            .ToList().OrderBy(x => x.Outer.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DtoProjectionWithClientMethodAfterDefaultIfEmpty()
    {
        using TestDatabase db = Seed();
        List<LjVariantParent> parents = Parents();
        List<LjVariantChild> children = Children();

        List<(int, string)> expected = (from p in parents
                                        join c in children on p.Id equals c.ParentId into grp
                                        from c in grp.DefaultIfEmpty()
                                        select new LjVariantRowDto { Id = p.Id, Label = Decorate(c == null ? "none" : c.Label) })
            .OrderBy(x => x.Id).Select(x => (x.Id, x.Label)).ToList();
        List<(int, string)> actual = (from p in db.Table<LjVariantParent>()
                                      join c in db.Table<LjVariantChild>() on p.Id equals c.ParentId into grp
                                      from c in grp.DefaultIfEmpty()
                                      select new LjVariantRowDto { Id = p.Id, Label = Decorate(c == null ? "none" : c.Label) })
            .ToList().OrderBy(x => x.Id).Select(x => (x.Id, x.Label)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereOnNestedMemberAfterDefaultIfEmptyProjection()
    {
        using TestDatabase db = Seed();
        List<LjVariantParent> parents = Parents();
        List<LjVariantChild> children = Children();

        var expected = (from p in parents
                        join c in children on p.Id equals c.ParentId into grp
                        from c in grp.DefaultIfEmpty()
                        select new { Outer = new { p.Id, p.Name }, Label = c == null ? "none" : c.Label })
            .Where(x => x.Outer.Id == 2)
            .Select(x => (x.Outer.Name, x.Label)).ToList();
        var actual = (from p in db.Table<LjVariantParent>()
                      join c in db.Table<LjVariantChild>() on p.Id equals c.ParentId into grp
                      from c in grp.DefaultIfEmpty()
                      select new { Outer = new { p.Id, p.Name }, Label = c == null ? "none" : c.Label })
            .Where(x => x.Outer.Id == 2)
            .ToList().Select(x => (x.Outer.Name, x.Label)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedAnonymousWithWholeEntityAfterDefaultIfEmpty()
    {
        using TestDatabase db = Seed();
        List<LjVariantParent> parents = Parents();
        List<LjVariantChild> children = Children();

        var expected = (from p in parents
                        join c in children on p.Id equals c.ParentId into grp
                        from c in grp.DefaultIfEmpty()
                        select new { Outer = new { p.Id, Entity = p }, Label = c == null ? "none" : c.Label })
            .OrderBy(x => x.Outer.Id)
            .Select(x => (x.Outer.Id, x.Outer.Entity.Name, x.Label)).ToList();
        var actual = (from p in db.Table<LjVariantParent>()
                      join c in db.Table<LjVariantChild>() on p.Id equals c.ParentId into grp
                      from c in grp.DefaultIfEmpty()
                      select new { Outer = new { p.Id, Entity = p }, Label = c == null ? "none" : c.Label })
            .ToList().OrderBy(x => x.Outer.Id)
            .Select(x => (x.Outer.Id, x.Outer.Entity.Name, x.Label)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InnerJoinNestedAnonymousWithWholeEntity()
    {
        using TestDatabase db = Seed();
        List<LjVariantParent> parents = Parents();
        List<LjVariantChild> children = Children();

        var expected = (from p in parents
                        join c in children on p.Id equals c.ParentId
                        select new { Outer = new { p.Id, Entity = p }, c.Label })
            .OrderBy(x => x.Outer.Id)
            .Select(x => (x.Outer.Id, x.Outer.Entity.Name, x.Label)).ToList();
        var actual = (from p in db.Table<LjVariantParent>()
                      join c in db.Table<LjVariantChild>() on p.Id equals c.ParentId
                      select new { Outer = new { p.Id, Entity = p }, c.Label })
            .ToList().OrderBy(x => x.Outer.Id)
            .Select(x => (x.Outer.Id, x.Outer.Entity.Name, x.Label)).ToList();

        Assert.Equal(expected, actual);
    }

    private static string Decorate(string value)
    {
        return "<" + value + ">";
    }
}
