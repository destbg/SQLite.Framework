using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

[Table("SvFilterRow")]
public sealed class SvFilterRow
{
    [Key]
    public int Id { get; set; }

    public required string Code { get; set; }

    public bool Active { get; set; }
}

public class SchemaValidatorPartialIndexFilterParityTests
{
    [Fact]
    public void FullIndexWhereModelDeclaresPartialIndex_ReportsDrift()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<SvFilterRow>().Index(
                r => r.Code,
                name: "IX_SvFilter_Code",
                unique: true,
                filter: r => r.Active));
        db.Schema.CreateTable<SvFilterRow>();

        db.Schema.DropIndex("IX_SvFilter_Code");
        db.Execute("CREATE UNIQUE INDEX \"IX_SvFilter_Code\" ON \"SvFilterRow\" (\"Code\")");

        SQLiteModelValidationResult result = db.Schema.ValidateModel<SvFilterRow>();

        Assert.False(result.IsValid, string.Join("; ", result.Issues));
    }
}
