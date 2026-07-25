using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SealedOrder")]
public class SealedOrderRow
{
    private SealedOrderRow()
    {
    }

    public SealedOrderRow(int id, string code)
    {
        Id = id;
        Code = code;
    }

    [Key]
    public int Id { get; }

    public string Code { get; } = "";
}

public class GetOnlyPropertyMaterializationTests
{
    [Fact]
    public void GetOnlyPropertiesReadBackWhenAPrivateParameterlessConstructorExists()
    {
        using TestDatabase db = new();
        db.Table<SealedOrderRow>().Schema.CreateTable();
        db.Table<SealedOrderRow>().Add(new SealedOrderRow(7, "A"));

        SealedOrderRow row = db.Table<SealedOrderRow>().ToList().Single();

        Assert.Equal(7, row.Id);
        Assert.Equal("A", row.Code);
    }
}
