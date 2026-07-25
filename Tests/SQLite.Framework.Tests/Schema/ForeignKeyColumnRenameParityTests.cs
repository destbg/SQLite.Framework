using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class FkrParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

internal sealed class FkrChild
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkrParent))]
    public int ParentId { get; set; }
}

internal sealed class FkrOrder
{
    public int Id { get; set; }

    public int Version { get; set; }
}

internal sealed class FkrOrderLine
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int OrderVersion { get; set; }
}

public class ForeignKeyColumnRenameParityTests
{
    [Fact]
    public void AttributeForeignKey_ParentKeyRenamedAfterDeclaration_ReferencesStaleColumn()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<FkrParent>().HasColumnName(p => p.Id, "parent_id"));
        db.Execute("PRAGMA foreign_keys = ON");
        db.Schema.CreateTable<FkrParent>();
        db.Schema.CreateTable<FkrChild>();
        db.Table<FkrParent>().Add(new FkrParent { Id = 1, Name = "p" });

        Assert.Throws<SQLiteException>(() => db.Table<FkrChild>().Add(new FkrChild { Id = 1, ParentId = 1 }));
    }

    [Fact]
    public void AttributeForeignKey_SourceColumnRenamedAfterDeclaration_UsesRenamedColumn()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<FkrChild>().HasColumnName(c => c.ParentId, "fk_parent"));
        db.Execute("PRAGMA foreign_keys = ON");
        db.Schema.CreateTable<FkrParent>();
        db.Schema.CreateTable<FkrChild>();

        db.Table<FkrParent>().Add(new FkrParent { Id = 1, Name = "p" });
        db.Table<FkrChild>().Add(new FkrChild { Id = 1, ParentId = 1 });

        Assert.Equal(1, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"FkrChild\""));
    }

    [Fact]
    public void FluentCompositeForeignKey_SourceColumnRenamedAfterDeclaration_UsesRenamedColumn()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<FkrOrder>().HasKey(o => new { o.Id, o.Version });
            model.Entity<FkrOrderLine>().ForeignKey<FkrOrder>(l => new { l.OrderId, l.OrderVersion }, o => new { o.Id, o.Version });
            model.Entity<FkrOrderLine>().HasColumnName(l => l.OrderId, "order_id");
            model.Entity<FkrOrderLine>().HasColumnName(l => l.Id, "line_id");
        });
        db.Execute("PRAGMA foreign_keys = ON");
        db.Schema.CreateTable<FkrOrder>();
        db.Schema.CreateTable<FkrOrderLine>();

        db.Table<FkrOrder>().Add(new FkrOrder { Id = 5, Version = 1 });
        db.Table<FkrOrderLine>().Add(new FkrOrderLine { Id = 1, OrderId = 5, OrderVersion = 1 });

        Assert.Equal(1, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"FkrOrderLine\""));
    }
}
