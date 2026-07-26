using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22bIdxCaseRows")]
public class H22bIdxCaseRow
{
    [Key]
    public int Id { get; set; }

    [Indexed("IXH22bIdxCase", 0)]
    public required string Code { get; set; }

    [Indexed("ixh22bidxcase", 1)]
    public required string Kind { get; set; }
}

public class IndexNameCasingDeclarationTests
{
    [Fact]
    public void TwoIndexNamesThatDifferOnlyInCaseCoverBothColumns()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<H22bIdxCaseRow>();

        Assert.Equal(
            "CREATE INDEX \"IXH22bIdxCase\" ON \"H22bIdxCaseRows\" (\"Code\", \"Kind\")",
            db.ExecuteScalar<string>("SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IXH22bIdxCase'"));
    }

    [Fact]
    public void ValidateModelReadsAModelWithTwoIndexNamesThatDifferOnlyInCase()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<H22bIdxCaseRow>();

        Exception? failure = Record.Exception(() => db.Schema.ValidateModel<H22bIdxCaseRow>());

        Assert.Null(failure);
    }
}
