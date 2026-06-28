using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

[Table("SvDirRow")]
public sealed class SvDirRow
{
    [Key]
    public int Id { get; set; }

    public required string Code { get; set; }
}

public class SchemaValidatorIndexDirectionParityTests
{
    [Fact]
    public void AscendingIndexWhereModelDeclaresDescending_ReportsDrift()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<SvDirRow>().Index(
                r => r.Code,
                name: "IX_SvDir_Code",
                direction: SQLiteIndexDirection.Descending));
        db.Schema.CreateTable<SvDirRow>();

        db.Schema.DropIndex("IX_SvDir_Code");
        db.Execute("CREATE INDEX \"IX_SvDir_Code\" ON \"SvDirRow\" (\"Code\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<SvDirRow>();

        Assert.False(result.IsValid, string.Join("; ", result.Issues));
    }
}
