using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class RenamedTableParent
{
    [Key]
    public int Id { get; set; }
}

internal sealed class RenamedTableChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

internal sealed class RenamedKeyParent
{
    [Key]
    public int Id { get; set; }
}

internal sealed class RenamedKeyChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

internal sealed class FluentKeyParent
{
    public int Code { get; set; }
}

internal sealed class FluentKeyChild
{
    [Key]
    public int Id { get; set; }

    public int ParentCode { get; set; }
}

public class FluentModelForeignKeyResolutionTests
{
    [Fact]
    public void ForeignKeyToParentRenamedWithToTableReferencesTheMappedTable()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<RenamedTableParent>().ToTable("RenamedParents");
            model.Entity<RenamedTableChild>().ForeignKey<RenamedTableParent>(c => c.ParentId);
        });
        db.Schema.CreateTable<RenamedTableParent>();
        db.Schema.CreateTable<RenamedTableChild>();
        db.Pragmas.ForeignKeys = true;

        db.Table<RenamedTableParent>().Add(new RenamedTableParent { Id = 1 });
        db.Table<RenamedTableChild>().Add(new RenamedTableChild { Id = 1, ParentId = 1 });

        Assert.Equal(1, db.Table<RenamedTableChild>().Count());
    }

    [Fact]
    public void ForeignKeyToParentKeyRenamedWithHasColumnNameReferencesTheMappedColumn()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<RenamedKeyParent>().HasColumnName(p => p.Id, "parent_code");
            model.Entity<RenamedKeyChild>().ForeignKey<RenamedKeyParent>(c => c.ParentId);
        });
        db.Schema.CreateTable<RenamedKeyParent>();
        db.Schema.CreateTable<RenamedKeyChild>();
        db.Pragmas.ForeignKeys = true;

        db.Table<RenamedKeyParent>().Add(new RenamedKeyParent { Id = 1 });
        db.Table<RenamedKeyChild>().Add(new RenamedKeyChild { Id = 1, ParentId = 1 });

        Assert.Equal(1, db.Table<RenamedKeyChild>().Count());
    }

    [Fact]
    public void ForeignKeyToParentWithFluentHasKeyResolvesTheKey()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<FluentKeyParent>().HasKey(p => p.Code);
            model.Entity<FluentKeyChild>().ForeignKey<FluentKeyParent>(c => c.ParentCode);
        });
        db.Schema.CreateTable<FluentKeyParent>();
        db.Schema.CreateTable<FluentKeyChild>();
        db.Pragmas.ForeignKeys = true;

        db.Table<FluentKeyParent>().Add(new FluentKeyParent { Code = 7 });
        db.Table<FluentKeyChild>().Add(new FluentKeyChild { Id = 1, ParentCode = 7 });

        Assert.Equal(1, db.Table<FluentKeyChild>().Count());
    }
}
