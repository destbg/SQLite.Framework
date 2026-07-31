using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CompositeSetNullParents")]
public class CompositeSetNullParent
{
    [Key]
    public int Id { get; set; }

    public int X { get; set; }

    public int Y { get; set; }
}

[Table("CompositeSetNullChildren")]
public class CompositeSetNullChild
{
    [Key]
    public int Id { get; set; }

    public int? A { get; set; }

    public int? B { get; set; }

    public int? C { get; set; }
}

public sealed class CompositeSetNullThenRequiredDatabase : TestDatabase
{
    public CompositeSetNullThenRequiredDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<CompositeSetNullChild>()
            .ForeignKey<CompositeSetNullParent>(c => new { c.A, c.B }, p => new { p.X, p.Y }, onDelete: SQLiteForeignKeyAction.SetNull)
            .IsRequired(c => c.A);
    }
}

public sealed class CompositeSetNullUnrelatedRequiredDatabase : TestDatabase
{
    public CompositeSetNullUnrelatedRequiredDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<CompositeSetNullChild>()
            .ForeignKey<CompositeSetNullParent>(c => new { c.A, c.B }, p => new { p.X, p.Y }, onDelete: SQLiteForeignKeyAction.SetNull)
            .IsRequired(c => c.C);
    }
}

public class CompositeSetNullForeignKeyRequiredOrderTests
{
    [Fact]
    public void ACompositeSetNullMemberMadeRequiredAfterwardsIsRejected()
    {
        using CompositeSetNullThenRequiredDatabase db = new();

        Assert.Throws<InvalidOperationException>(() => db.Schema.CreateTable<CompositeSetNullChild>());
    }

    [Fact]
    public void AColumnOutsideTheCompositeSetNullKeyCanBecomeRequired()
    {
        using CompositeSetNullUnrelatedRequiredDatabase db = new();

        db.Schema.CreateTable<CompositeSetNullChild>();

        Assert.Equal(0, db.Table<CompositeSetNullChild>().Count());
    }
}
