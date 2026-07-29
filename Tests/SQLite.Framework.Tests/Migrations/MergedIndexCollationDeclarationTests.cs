using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24dCollatedCodes")]
public class H24dCollatedCode
{
    [Key]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.Indexed]
    public string Code { get; set; } = "";
}

public class MergedIndexCollationDeclarationTests
{
    [Fact]
    public void AnAttributeIndexAndAFluentIndexWithACollationBuildOneSingleColumnIndex()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<H24dCollatedCode>().Index(r => r.Code, collation: SQLiteCollation.NoCase));

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<H24dCollatedCode>())
            .Migrate();

        List<string> indexColumns = db.Query<string>(
            "SELECT \"name\" FROM pragma_index_info('idx_H24dCollatedCodes_Code')");
        SQLiteModelValidationResult result = db.Schema.ValidateModel<H24dCollatedCode>();

        Assert.True(db.Schema.IndexExists("idx_H24dCollatedCodes_Code"));
        Assert.Single(indexColumns);
        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }
}
