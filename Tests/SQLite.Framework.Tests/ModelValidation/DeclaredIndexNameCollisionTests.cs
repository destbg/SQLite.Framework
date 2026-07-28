using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23eCatalogRows")]
public class H23eCatalogRow
{
    [Key]
    public int Id { get; set; }

    [Indexed]
    public string Code { get; set; } = "";

    public string Kind { get; set; } = "";
}

[Table("H23eLedgerRows")]
public class H23eLedgerRow
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";

    public string Kind { get; set; } = "";
}

public class DeclaredIndexNameCollisionTests
{
    [Fact]
    public void ValidateModelReadsAModelWhereAnAttributeAndAFluentIndexShareTheDefaultName()
    {
        using ModelTestDatabase db = new(model => model.Entity<H23eCatalogRow>().Index(r => r.Code));
        db.Schema.CreateTable<H23eCatalogRow>();

        Exception? failure = Record.Exception(() => db.Schema.ValidateModel<H23eCatalogRow>());

        Assert.Null(failure);
    }

    [Fact]
    public void ValidateModelReadsAModelWhereTwoFluentIndexesShareAName()
    {
        using ModelTestDatabase db = new(model => model.Entity<H23eLedgerRow>()
            .Index(r => r.Code, name: "IXH23eLedger")
            .Index(r => r.Kind, name: "IXH23eLedger"));
        db.Schema.CreateTable<H23eLedgerRow>();

        Exception? failure = Record.Exception(() => db.Schema.ValidateModel<H23eLedgerRow>());

        Assert.Null(failure);
    }

    [Fact]
    public void ValidateModelReadsAModelWhereTheFirstSharedNameIndexIsUnique()
    {
        using ModelTestDatabase db = new(model => model.Entity<H23eLedgerRow>()
            .Index(r => r.Code, name: "IXH23eLedgerU", unique: true)
            .Index(r => r.Kind, name: "IXH23eLedgerU"));
        db.Schema.CreateTable<H23eLedgerRow>();

        Exception? failure = Record.Exception(() => db.Schema.ValidateModel<H23eLedgerRow>());

        Assert.Null(failure);
    }

    [Fact]
    public void ValidateModelReadsAModelWhereTheFirstSharedNameIndexHasAFilter()
    {
        using ModelTestDatabase db = new(model => model.Entity<H23eLedgerRow>()
            .Index(r => r.Code, name: "IXH23eLedgerF", filter: r => r.Id > 0)
            .Index(r => r.Kind, name: "IXH23eLedgerF"));
        db.Schema.CreateTable<H23eLedgerRow>();

        Exception? failure = Record.Exception(() => db.Schema.ValidateModel<H23eLedgerRow>());

        Assert.Null(failure);
    }
}
