using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("VmvLowerRow")]
public class VmvLowerRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

[Table("VmvVarcharRow")]
public class VmvVarcharRow
{
    [Key]
    public int Id { get; set; }

    public required string Code { get; set; }

    public required string Notes { get; set; }
}

[Table("VmvBlobRow")]
public class VmvBlobRow
{
    [Key]
    public int Id { get; set; }

    public required byte[] Data { get; set; }
}

[Table("VmvDecimalRow")]
public class VmvDecimalRow
{
    [Key]
    public int Id { get; set; }

    public int Age { get; set; }
}

[Table("VmvCompParent")]
public class VmvCompParent
{
    public int X { get; set; }

    public int Y { get; set; }
}

[Table("VmvCompChild")]
public class VmvCompChild
{
    [Key]
    public int Id { get; set; }

    public int AId { get; set; }

    public int BId { get; set; }
}

[Table("VmvNoKeyParent")]
public class VmvNoKeyParent
{
    [Key]
    public int Id { get; set; }
}

[Table("VmvNoKeyChild")]
public class VmvNoKeyChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

[Table("VmvCaseParent")]
public class VmvCaseParent
{
    [Key]
    public int Id { get; set; }
}

[Table("VmvCaseChild")]
public class VmvCaseChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

[Table("VmvIdxRow")]
public class VmvIdxRow
{
    [Key]
    public int Id { get; set; }

    public required string Code { get; set; }
}

[Table("VmvTrigSource")]
public class VmvTrigSource
{
    [Key]
    public int Id { get; set; }
}

[Table("VmvTrigAudit")]
public class VmvTrigAudit
{
    [Key]
    public int Id { get; set; }

    public int ItemId { get; set; }
}

file sealed class VmvTriggerDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<VmvTrigAudit>().HasKey(a => a.Id);
        builder.Entity<VmvTrigSource>()
            .Trigger("trg_vmv", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<VmvTrigAudit>(), s => s.Set(a => a.ItemId, _ => t.New.Id)));
    }
}

public class ValidateModelEquivalentSchemaVariantTests
{
    [Fact]
    public void LowercaseLiveTableNameIsValid()
    {
        using TestDatabase db = new();
        db.Execute("create table vmvlowerrow (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvLowerRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void VarcharAndClobColumnsWithTextAffinityAreValid()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"VmvVarcharRow\" (\"Id\" INTEGER PRIMARY KEY, \"Code\" VARCHAR(40) NOT NULL, \"Notes\" CLOB NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvVarcharRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void UntypedLiveColumnForBlobColumnIsValid()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"VmvBlobRow\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvBlobRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void NumericTypeNameForIntegerColumnIsReported()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"VmvDecimalRow\" (\"Id\" INTEGER PRIMARY KEY, \"Age\" DECIMAL NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvDecimalRow>();

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Age") && i.Contains("has type"));
    }

    [Fact]
    public void ImplicitCompositeForeignKeyTargetIsValid()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<VmvCompParent>().HasKey(p => new { p.X, p.Y });
            model.Entity<VmvCompChild>().ForeignKey<VmvCompParent>(c => new { c.AId, c.BId }, p => new { p.X, p.Y });
        });
        db.Execute("CREATE TABLE \"VmvCompParent\" (\"X\" INTEGER NOT NULL, \"Y\" INTEGER NOT NULL, PRIMARY KEY (\"X\", \"Y\"))");
        db.Execute("CREATE TABLE \"VmvCompChild\" (\"Id\" INTEGER PRIMARY KEY, \"AId\" INTEGER NOT NULL, \"BId\" INTEGER NOT NULL, FOREIGN KEY (\"AId\", \"BId\") REFERENCES \"VmvCompParent\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvCompChild>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void ImplicitForeignKeyToParentWithoutPrimaryKeyIsReported()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<VmvNoKeyChild>().ForeignKey<VmvNoKeyParent>(c => c.ParentId, p => p.Id));
        db.Execute("CREATE TABLE \"VmvNoKeyParent\" (\"Id\" INTEGER NOT NULL)");
        db.Execute("CREATE TABLE \"VmvNoKeyChild\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL REFERENCES \"VmvNoKeyParent\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvNoKeyChild>();

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Foreign key") && i.Contains("ParentId"));
    }

    [Fact]
    public void ForeignKeyNameCaseDifferencesAreValid()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<VmvCaseChild>().ForeignKey<VmvCaseParent>(c => c.ParentId, p => p.Id));
        db.Execute("CREATE TABLE \"vmvcaseparent\" (\"ID\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"VmvCaseChild\" (\"Id\" INTEGER PRIMARY KEY, \"parentid\" INTEGER NOT NULL REFERENCES \"vmvcaseparent\"(\"ID\"))");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvCaseChild>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void BracketQuotedLiveIndexIsValid()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<VmvIdxRow>().Index(r => r.Code, name: "IX_VmvIdxCode"));
        db.Schema.CreateTable<VmvIdxRow>();
        db.Schema.DropIndex("IX_VmvIdxCode");
        db.Execute("CREATE INDEX [IX_VmvIdxCode] ON [VmvIdxRow] ([Code])");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvIdxRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void LowercaseLiveIndexAndColumnNamesAreValid()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<VmvIdxRow>().Index(r => r.Code, name: "IX_VmvIdxCode"));
        db.Execute("create table vmvidxrow (\"id\" INTEGER PRIMARY KEY, \"code\" TEXT NOT NULL)");
        db.Execute("create index ix_vmvidxcode on vmvidxrow (code)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvIdxRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void IndexCreatedWithIfNotExistsIsValid()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<VmvIdxRow>().Index(r => r.Code, name: "IX_VmvIdxCode"));
        db.Schema.CreateTable<VmvIdxRow>();
        db.Schema.DropIndex("IX_VmvIdxCode");
        db.Execute("CREATE INDEX IF NOT EXISTS \"IX_VmvIdxCode\" ON \"VmvIdxRow\" (\"Code\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvIdxRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void CollationChangeOnLiveIndexIsReported()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<VmvIdxRow>().Index(r => r.Code, name: "IX_VmvIdxCode", collation: SQLiteCollation.NoCase));
        db.Schema.CreateTable<VmvIdxRow>();
        db.Schema.DropIndex("IX_VmvIdxCode");
        db.Execute("CREATE INDEX \"IX_VmvIdxCode\" ON \"VmvIdxRow\" (\"Code\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvIdxRow>();

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("IX_VmvIdxCode") && i.Contains("definition"));
    }

    [Fact]
    public void UnquotedLiveTriggerWithSameStructureIsValid()
    {
        using VmvTriggerDb db = new();
        db.Schema.CreateTable<VmvTrigAudit>();
        db.Schema.CreateTable<VmvTrigSource>();
        string sql = db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = 'trg_vmv'")!;
        db.Schema.DropTrigger("trg_vmv");
        db.Execute(sql.Replace("\"", "").Replace("trg_vmv", "TRG_VMV"));

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmvTrigSource>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }
}
