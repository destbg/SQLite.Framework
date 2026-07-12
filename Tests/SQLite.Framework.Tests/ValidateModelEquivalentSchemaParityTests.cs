using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("VmeCaseRow")]
public class VmeCaseRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

[Table("VmeImpParent")]
public class VmeImpParent
{
    [Key]
    public int Id { get; set; }
}

[Table("VmeImpChild")]
public class VmeImpChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }
}

[Table("VmeIdxRow")]
public class VmeIdxRow
{
    [Key]
    public int Id { get; set; }

    public required string Code { get; set; }
}

[Table("VmeAffRow")]
public class VmeAffRow
{
    [Key]
    public int Id { get; set; }

    public int Age { get; set; }
}

public class ValidateModelEquivalentSchemaParityTests
{
    [Fact]
    public void ColumnNamesDifferingOnlyInCaseAreValid()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"VmeCaseRow\" (\"id\" INTEGER PRIMARY KEY, \"name\" TEXT NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmeCaseRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void ForeignKeyWithImplicitTargetColumnIsValid()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<VmeImpParent>().HasKey(p => p.Id);
            model.Entity<VmeImpChild>().ForeignKey<VmeImpParent>(c => c.ParentId);
        });
        db.Execute("CREATE TABLE \"VmeImpParent\" (\"Id\" INTEGER PRIMARY KEY)");
        db.Execute("CREATE TABLE \"VmeImpChild\" (\"Id\" INTEGER PRIMARY KEY, \"ParentId\" INTEGER NOT NULL REFERENCES \"VmeImpParent\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmeImpChild>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void UnquotedLiveIndexWithSameStructureIsValid()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<VmeIdxRow>().Index(r => r.Code, name: "IX_VmeIdxCode"));
        db.Schema.CreateTable<VmeIdxRow>();
        db.Schema.DropIndex("IX_VmeIdxCode");
        db.Execute("CREATE INDEX IX_VmeIdxCode ON VmeIdxRow (Code)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmeIdxRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void IntTypeNameWithIntegerAffinityIsValid()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"VmeAffRow\" (\"Id\" INTEGER PRIMARY KEY, \"Age\" INT NOT NULL)");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<VmeAffRow>();

        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }
}
