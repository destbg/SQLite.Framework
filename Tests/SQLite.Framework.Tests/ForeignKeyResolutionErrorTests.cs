using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class KeylessFkParent
{
    public int Code { get; set; }
}

internal sealed class ChildToKeylessParent
{
    [Key]
    public int Id { get; set; }

    public int ParentCode { get; set; }
}

internal sealed class CompositeKeyFkParent
{
    [Key]
    public int A { get; set; }

    [Key]
    public int B { get; set; }
}

internal sealed class ChildToCompositeParent
{
    [Key]
    public int Id { get; set; }

    public int ParentRef { get; set; }
}

internal sealed class ChildWithCompositeFk
{
    [Key]
    public int Id { get; set; }

    public int RefA { get; set; }

    public int RefB { get; set; }
}

public class ForeignKeyResolutionErrorTests
{
    [Fact]
    public void ForeignKeyToParentWithoutPrimaryKeyThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using ModelTestDatabase db = new(model =>
                model.Entity<ChildToKeylessParent>().ForeignKey<KeylessFkParent>(c => c.ParentCode));
            db.Schema.CreateTable<ChildToKeylessParent>();
        });
    }

    [Fact]
    public void SingleColumnForeignKeyToCompositeKeyParentThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using ModelTestDatabase db = new(model =>
                model.Entity<ChildToCompositeParent>().ForeignKey<CompositeKeyFkParent>(c => c.ParentRef));
            db.Schema.CreateTable<ChildToCompositeParent>();
        });
    }

    [Fact]
    public void CompositeForeignKeyToCompositeKeyParentResolves()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<ChildWithCompositeFk>().ForeignKey<CompositeKeyFkParent>(c => new { c.RefA, c.RefB }));

        db.Schema.CreateTable<CompositeKeyFkParent>();
        db.Schema.CreateTable<ChildWithCompositeFk>();

        Assert.Empty(db.Table<ChildWithCompositeFk>().ToList());
    }
}
