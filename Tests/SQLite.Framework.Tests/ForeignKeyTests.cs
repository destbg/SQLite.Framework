using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ForeignKeyTests
{
    [Fact]
    public void Pragma_ForeignKeys_OnByDefault()
    {
        using TestDatabase db = new();

        Assert.True(db.Pragmas.ForeignKeys);
    }

    [Fact]
    public void UseForeignKeys_False_DisablesEnforcement()
    {
        using TestDatabase db = new(b => b.UseForeignKeys(false));

        Assert.False(db.Pragmas.ForeignKeys);
    }

    [Fact]
    public void Pragmas_ForeignKeys_CanBeFlippedAtRuntime()
    {
        using TestDatabase db = new();

        Assert.True(db.Pragmas.ForeignKeys);

        db.Pragmas.ForeignKeys = false;
        Assert.False(db.Pragmas.ForeignKeys);

        db.Pragmas.ForeignKeys = true;
        Assert.True(db.Pragmas.ForeignKeys);
    }

    [Fact]
    public void ReferencesTable_InferredPk_EmitsInlineReferences()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBook>();

        TableMapping mapping = db.TableMapping<FkBook>();
        TableColumn fkColumn = mapping.Columns.First(c => c.Name == "AuthorId");

        Assert.NotNull(fkColumn.ForeignKey);
        Assert.Equal("FkAuthor", fkColumn.ForeignKey!.TargetTable);
        Assert.Equal(["Id"], fkColumn.ForeignKey.TargetColumns);
    }

    [Fact]
    public void ReferencesTable_InsertWithoutParent_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBook>();

        Assert.Throws<SQLiteException>(() =>
            db.Table<FkBook>().Add(new FkBook { Id = 1, Title = "x", AuthorId = 999 }));
    }

    [Fact]
    public void ReferencesTable_InsertWithParent_Succeeds()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBook>();
        db.Table<FkAuthor>().Add(new FkAuthor { Id = 1, Name = "Alice" });

        db.Table<FkBook>().Add(new FkBook { Id = 1, Title = "x", AuthorId = 1 });

        Assert.Single(db.Table<FkBook>().ToList());
    }

    [Fact]
    public void ReferencesTable_OnDeleteCascade_RemovesChildren()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBookCascade>();
        db.Table<FkAuthor>().Add(new FkAuthor { Id = 1, Name = "Alice" });
        db.Table<FkBookCascade>().Add(new FkBookCascade { Id = 1, Title = "x", AuthorId = 1 });
        db.Table<FkBookCascade>().Add(new FkBookCascade { Id = 2, Title = "y", AuthorId = 1 });

        db.Table<FkAuthor>().Remove(new FkAuthor { Id = 1, Name = "Alice" });

        Assert.Empty(db.Table<FkBookCascade>().ToList());
    }

    [Fact]
    public void ReferencesTable_OnDeleteSetNull_NullsChildColumn()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBookSetNull>();
        db.Table<FkAuthor>().Add(new FkAuthor { Id = 1, Name = "Alice" });
        db.Table<FkBookSetNull>().Add(new FkBookSetNull { Id = 1, Title = "x", AuthorId = 1 });

        db.Table<FkAuthor>().Remove(new FkAuthor { Id = 1, Name = "Alice" });

        FkBookSetNull child = db.Table<FkBookSetNull>().First();
        Assert.Null(child.AuthorId);
    }

    [Fact]
    public void ReferencesTable_SetNullOnNonNullableColumn_ThrowsAtMappingTime()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.TableMapping<FkBookBrokenSetNull>());
        Assert.Contains("ON DELETE SET NULL", ex.Message);
    }

    [Fact]
    public void ReferencesTable_TargetMissingKey_ThrowsAtMappingTime()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.TableMapping<FkBookMissingTargetKey>());
        Assert.Contains("[Key]", ex.Message);
    }

    [Fact]
    public void ReferencesTable_TargetCompositePk_ThrowsAtMappingTime()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.TableMapping<FkBookCompositeTarget>());
        Assert.Contains("composite primary key", ex.Message);
    }

    [Fact]
    public void ReferencesTable_NamedTargetColumn_ResolvesByName()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBookByName>();

        TableColumn fkColumn = db.TableMapping<FkBookByName>().Columns.First(c => c.Name == "AuthorId");

        Assert.Equal(["Id"], fkColumn.ForeignKey!.TargetColumns);
    }

    [Fact]
    public void ReferencesTable_TargetColumnMissing_ThrowsAtMappingTime()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.TableMapping<FkBookUnknownColumn>());
        Assert.Contains("Nope", ex.Message);
    }

    [Fact]
    public void ReferencesTable_Deferred_StoresDeferredFlag()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBookDeferred>();

        TableColumn fkColumn = db.TableMapping<FkBookDeferred>().Columns.First(c => c.Name == "AuthorId");
        Assert.True(fkColumn.ForeignKey!.Deferred);
    }

    [Fact]
    public void EfForeignKey_StringTarget_ResolvesByClassName()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBookEf>();

        TableColumn fkColumn = db.TableMapping<FkBookEf>().Columns.First(c => c.Name == "AuthorId");

        Assert.NotNull(fkColumn.ForeignKey);
        Assert.Equal("FkAuthor", fkColumn.ForeignKey!.TargetTable);
        Assert.Equal(["Id"], fkColumn.ForeignKey.TargetColumns);
        Assert.Equal(SQLiteForeignKeyAction.NoAction, fkColumn.ForeignKey.OnDelete);
        Assert.Equal(SQLiteForeignKeyAction.NoAction, fkColumn.ForeignKey.OnUpdate);
    }

    [Fact]
    public void EfForeignKey_StringTarget_TableAttributeMatch_Resolves()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBookEfByTableName>();

        TableColumn fkColumn = db.TableMapping<FkBookEfByTableName>().Columns.First(c => c.Name == "AuthorId");
        Assert.Equal("FkAuthor", fkColumn.ForeignKey!.TargetTable);
    }

    [Fact]
    public void EfForeignKey_UnknownClass_ThrowsAtMappingTime()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.TableMapping<FkBookEfMissing>());
        Assert.Contains("NoSuchType", ex.Message);
    }

    [Fact]
    public void EfForeignKey_AmbiguousClassName_ThrowsAtMappingTime()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.TableMapping<FkBookEfAmbiguous>());
        Assert.Contains("matched", ex.Message);
    }

    [Fact]
    public void BothAttributesOnSameProperty_ThrowsAtMappingTime()
    {
        using TestDatabase db = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => db.TableMapping<FkBookBothAttributes>());
        Assert.Contains("[ReferencesTable]", ex.Message);
    }

    [Fact]
    public void Fluent_SingleColumn_InferredPk_EmitsInlineReferences()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkBookFluent>()
            .ForeignKey<FkAuthor>(b => b.AuthorId, onDelete: SQLiteForeignKeyAction.Cascade));
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBookFluent>();

        TableColumn fkColumn = db.TableMapping<FkBookFluent>().Columns.First(c => c.Name == "AuthorId");
        Assert.NotNull(fkColumn.ForeignKey);
        Assert.Equal(SQLiteForeignKeyAction.Cascade, fkColumn.ForeignKey!.OnDelete);

        db.Table<FkAuthor>().Add(new FkAuthor { Id = 1, Name = "x" });
        db.Table<FkBookFluent>().Add(new FkBookFluent { Id = 1, AuthorId = 1 });
        db.Table<FkAuthor>().Remove(new FkAuthor { Id = 1, Name = "x" });
        Assert.Empty(db.Table<FkBookFluent>().ToList());
    }

    [Fact]
    public void Fluent_NamedTarget_ResolvesByName()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkBookFluentNamed>()
            .ForeignKey<FkAuthor>(b => b.AuthorId, a => a.Id));
        db.Schema.CreateTable<FkAuthor>();
        db.Schema.CreateTable<FkBookFluentNamed>();

        TableColumn fkColumn = db.TableMapping<FkBookFluentNamed>().Columns.First(c => c.Name == "AuthorId");
        Assert.Equal(["Id"], fkColumn.ForeignKey!.TargetColumns);
    }

    [Fact]
    public void Fluent_Composite_EmitsTableLevelConstraint()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkOrderLine>()
            .ForeignKey<FkOrder>(
                l => new { l.OrderId, l.OrderVersion },
                o => new { o.Id, o.Version },
                onDelete: SQLiteForeignKeyAction.Cascade));
        db.Schema.CreateTable<FkOrder>();
        db.Schema.CreateTable<FkOrderLine>();

        db.Table<FkOrder>().Add(new FkOrder { Id = 1, Version = 1, Name = "o" });
        db.Table<FkOrderLine>().Add(new FkOrderLine { Id = 1, OrderId = 1, OrderVersion = 1, Sku = "a" });

        Assert.Throws<SQLiteException>(() =>
            db.Table<FkOrderLine>().Add(new FkOrderLine { Id = 2, OrderId = 9, OrderVersion = 9, Sku = "b" }));

        Assert.Single(db.TableMapping<FkOrderLine>().CompositeForeignKeys);
    }

    [Fact]
    public void Fluent_DuplicateOnSameColumn_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkBook>()
            .ForeignKey<FkAuthor>(b => b.AuthorId));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<FkBook>());
        Assert.Contains("already has a foreign key", ex.Message);
    }

    [Fact]
    public void Fluent_UnknownLocalProperty_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkBookFluentBad>()
            .ForeignKey<FkAuthor>(b => b.MissingProp));

        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<FkBookFluentBad>());
    }

    [Fact]
    public void Fluent_NonMemberExpression_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkBookFluentNamed>()
            .ForeignKey<FkAuthor>(b => b.AuthorId + 1));

        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<FkBookFluentNamed>());
    }

    [Fact]
    public void Fluent_CompositeArityMismatch_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkOrderLine>()
            .ForeignKey<FkOrder>(
                l => new { l.OrderId, l.OrderVersion },
                o => o.Id));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<FkOrderLine>());
        Assert.Contains("Local and target column counts must match", ex.Message);
    }

    [Fact]
    public void Fluent_CompositeWithNonMemberArg_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkOrderLine>()
            .ForeignKey<FkOrder>(
                l => new { Bad = l.OrderId + 1, l.OrderVersion },
                o => new { o.Id, o.Version }));

        Assert.Throws<ArgumentException>(() => db.Schema.CreateTable<FkOrderLine>());
    }

    [Fact]
    public void Fluent_SetNullOnNonNullable_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkBookFluentNamed>()
            .ForeignKey<FkAuthor>(b => b.AuthorId, onDelete: SQLiteForeignKeyAction.SetNull));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<FkBookFluentNamed>());
        Assert.Contains("ON DELETE SET NULL", ex.Message);
    }

    [Fact]
    public void Fluent_OnUpdateSetNullOnNonNullable_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkBookFluentNamed>()
            .ForeignKey<FkAuthor>(b => b.AuthorId, onUpdate: SQLiteForeignKeyAction.SetNull));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            db.Schema.CreateTable<FkBookFluentNamed>());
        Assert.Contains("ON UPDATE SET NULL", ex.Message);
    }

    [Fact]
    public void Sql_AllActions_AreEmittedCorrectly()
    {
        ForeignKeyInfo info = new(
            columns: ["A"],
            targetTable: "Parent",
            targetColumns: ["Id"],
            onDelete: SQLiteForeignKeyAction.Restrict,
            onUpdate: SQLiteForeignKeyAction.SetDefault,
            deferred: true);
        StringBuilder sb = new();
        ForeignKeySql.WriteSql(info, sb, inline: true);
        string sql = sb.ToString();
        Assert.Equal("REFERENCES \"Parent\"(\"Id\") ON DELETE RESTRICT ON UPDATE SET DEFAULT DEFERRABLE INITIALLY DEFERRED", sql);
        Assert.Equal("REFERENCES \"Parent\"(\"Id\") ON DELETE RESTRICT ON UPDATE SET DEFAULT DEFERRABLE INITIALLY DEFERRED", sql);
        Assert.Equal("REFERENCES \"Parent\"(\"Id\") ON DELETE RESTRICT ON UPDATE SET DEFAULT DEFERRABLE INITIALLY DEFERRED", sql);
    }

    [Fact]
    public void Sql_NoActions_OmitsOnDeleteOnUpdate()
    {
        ForeignKeyInfo info = new(
            columns: ["A"],
            targetTable: "Parent",
            targetColumns: ["Id"],
            onDelete: SQLiteForeignKeyAction.NoAction,
            onUpdate: SQLiteForeignKeyAction.NoAction,
            deferred: false);
        StringBuilder sb = new();
        ForeignKeySql.WriteSql(info, sb, inline: false);
        string sql = sb.ToString();
        Assert.Equal("FOREIGN KEY (\"A\") REFERENCES \"Parent\"(\"Id\")", sql);
        Assert.Equal("FOREIGN KEY (\"A\") REFERENCES \"Parent\"(\"Id\")", sql);
        Assert.Equal("FOREIGN KEY (\"A\") REFERENCES \"Parent\"(\"Id\")", sql);
    }

    [Fact]
    public void Sql_CascadeAction_IsEmitted()
    {
        ForeignKeyInfo info = new(
            columns: ["A"],
            targetTable: "Parent",
            targetColumns: ["Id"],
            onDelete: SQLiteForeignKeyAction.Cascade,
            onUpdate: SQLiteForeignKeyAction.SetNull,
            deferred: false);
        StringBuilder sb = new();
        ForeignKeySql.WriteSql(info, sb, inline: true);
        string sql = sb.ToString();
        Assert.Equal("REFERENCES \"Parent\"(\"Id\") ON DELETE CASCADE ON UPDATE SET NULL", sql);
        Assert.Equal("REFERENCES \"Parent\"(\"Id\") ON DELETE CASCADE ON UPDATE SET NULL", sql);
    }

    [Fact]
    public void ForeignKeyInfo_RejectsMismatchedColumnCounts()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ForeignKeyInfo(
                columns: ["A", "B"],
                targetTable: "Parent",
                targetColumns: ["Id"],
                onDelete: SQLiteForeignKeyAction.NoAction,
                onUpdate: SQLiteForeignKeyAction.NoAction,
                deferred: false));
        Assert.Contains("column counts must match", ex.Message);
    }

    [Fact]
    public void ForeignKeyInfo_RejectsEmptyColumns()
    {
        Assert.Throws<ArgumentException>(() =>
            new ForeignKeyInfo(
                columns: [],
                targetTable: "Parent",
                targetColumns: [],
                onDelete: SQLiteForeignKeyAction.NoAction,
                onUpdate: SQLiteForeignKeyAction.NoAction,
                deferred: false));
    }

    [Fact]
    public void ForeignKeyInfo_UnknownActionEnum_ThrowsOnWrite()
    {
        ForeignKeyInfo info = new(
            columns: ["A"],
            targetTable: "Parent",
            targetColumns: ["Id"],
            onDelete: (SQLiteForeignKeyAction)999,
            onUpdate: SQLiteForeignKeyAction.NoAction,
            deferred: false);
        StringBuilder sb = new();
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => ForeignKeySql.WriteSql(info, sb, inline: true));
        Assert.Contains("Unknown foreign key action", ex.Message);
    }

    [Fact]
    public void Fluent_CompositeWithConvertWrappedMember_Unwraps()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkOrderLine>()
            .ForeignKey<FkOrder>(
                l => new { Wrapped = (long)l.OrderId, l.OrderVersion },
                o => new { o.Id, o.Version }));
        db.Schema.CreateTable<FkOrder>();
        db.Schema.CreateTable<FkOrderLine>();

        Assert.Single(db.TableMapping<FkOrderLine>().CompositeForeignKeys);
    }

    [Fact]
    public void Schema_CreateTable_EmitsCompositeForeignKeyOnMapping()
    {
        using ModelTestDatabase db = new(model => model.Entity<FkOrderLine>()
            .ForeignKey<FkOrder>(
                l => new { l.OrderId, l.OrderVersion },
                o => new { o.Id, o.Version }));
        db.Schema.CreateTable<FkOrder>();
        db.Schema.CreateTable<FkOrderLine>();

        db.Table<FkOrder>().Add(new FkOrder { Id = 1, Version = 1, Name = "o" });
        Assert.Throws<SQLiteException>(() =>
            db.Table<FkOrderLine>().Add(new FkOrderLine { Id = 1, OrderId = 9, OrderVersion = 9, Sku = "x" }));
    }

    [Fact]
    public void ReferencesTable_TargetWithColumnAttribute_UsesCustomName()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<FkParentWithColumn>();
        db.Schema.CreateTable<FkChildToColumn>();

        TableColumn fkColumn = db.TableMapping<FkChildToColumn>().Columns.First(c => c.Name == "ParentId");
        Assert.Equal(["ParentKey"], fkColumn.ForeignKey!.TargetColumns);
    }
}

public class FkAuthor
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
}

[Table("FkAuthorNoKeyTable")]
public class FkAuthorNoKey
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class FkBook
{
    [Key]
    public int Id { get; set; }
    public required string Title { get; set; }

    [ReferencesTable(typeof(FkAuthor))]
    public int AuthorId { get; set; }
}

public class FkBookCascade
{
    [Key]
    public int Id { get; set; }
    public required string Title { get; set; }

    [ReferencesTable(typeof(FkAuthor), OnDelete = SQLiteForeignKeyAction.Cascade)]
    public int AuthorId { get; set; }
}

public class FkBookSetNull
{
    [Key]
    public int Id { get; set; }
    public required string Title { get; set; }

    [ReferencesTable(typeof(FkAuthor), OnDelete = SQLiteForeignKeyAction.SetNull)]
    public int? AuthorId { get; set; }
}

public class FkBookBrokenSetNull
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkAuthor), OnDelete = SQLiteForeignKeyAction.SetNull)]
    public int AuthorId { get; set; }
}

public class FkBookMissingTargetKey
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkAuthorNoKey))]
    public int AuthorId { get; set; }
}

public class FkCompositeTarget
{
    [Key]
    public int A { get; set; }
    [Key]
    public int B { get; set; }
}

public class FkBookCompositeTarget
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkCompositeTarget))]
    public int A { get; set; }
}

public class FkBookByName
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkAuthor), nameof(FkAuthor.Id))]
    public int AuthorId { get; set; }
}

public class FkBookUnknownColumn
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkAuthor), "Nope")]
    public int AuthorId { get; set; }
}

public class FkBookDeferred
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkAuthor), Deferred = true)]
    public int AuthorId { get; set; }
}

public class FkBookEf
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("FkAuthor")]
    public int AuthorId { get; set; }
}

[Table("FkBookEfByTableName")]
public class FkBookEfByTableName
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("FkAuthor")]
    public int AuthorId { get; set; }
}

public class FkBookEfMissing
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("NoSuchType")]
    public int AuthorId { get; set; }
}

public class FkAmbiguous
{
    [Key]
    public int Id { get; set; }
}

[Table("FkAmbiguous")]
public class FkAmbiguousAlias
{
    [Key]
    public int Other { get; set; }
}

public class FkBookEfAmbiguous
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("FkAmbiguous")]
    public int Aid { get; set; }
}

public class FkBookBothAttributes
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkAuthor))]
    [ForeignKey("FkAuthor")]
    public int AuthorId { get; set; }
}

public class FkBookFluent
{
    [Key]
    public int Id { get; set; }
    public int AuthorId { get; set; }
}

public class FkBookFluentNamed
{
    [Key]
    public int Id { get; set; }
    public int AuthorId { get; set; }
}

public class FkBookFluentBad
{
    [Key]
    public int Id { get; set; }

    [NotMapped]
    public int MissingProp { get; set; }
}

public class FkOrder
{
    [Key]
    public int Id { get; set; }
    [Key]
    public int Version { get; set; }
    public required string Name { get; set; }
}

public class FkOrderLine
{
    [Key]
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int OrderVersion { get; set; }
    public required string Sku { get; set; }
}

public class FkParentWithColumn
{
    [Key]
    [Column("ParentKey")]
    public int Id { get; set; }
}

public class FkChildToColumn
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(FkParentWithColumn), nameof(FkParentWithColumn.Id))]
    public int ParentId { get; set; }
}
