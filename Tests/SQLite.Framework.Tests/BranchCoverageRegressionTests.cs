using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
file enum Permission
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = 3
}

[Table("PermissionRows")]
file sealed class PermissionRow
{
    [Key]
    public int Id { get; set; }

    public Permission Access { get; set; }
}

public class BranchCoverageRegressionTests
{
    [Fact]
    public void DecimalModuloKeepsFraction()
    {
        using TestDatabase db = new();
        db.Table<ProductLine>().Schema.CreateTable();
        db.Table<ProductLine>().Add(new ProductLine { Id = 1, Price = 5.5m, Quantity = 1, Total = 0m });

        decimal actual = db.Table<ProductLine>().Select(p => p.Price % 2m).Single();

        Assert.Equal(5.5m % 2m, actual);
    }

    [Fact]
    public void FloatModuloKeepsFraction()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, FloatValue = 5.5f });

        float actual = db.Table<NumericType>().Select(n => n.FloatValue % 2f).Single();

        Assert.Equal(5.5f % 2f, actual);
    }

    [Fact]
    public void NullableColumnOnRightOfEqualityKeepsNullSafeSemantics()
    {
        using TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.CreateCommand("INSERT INTO NullableEntity (\"Id\",\"Value\") VALUES (1,NULL),(2,5),(3,7)", []).ExecuteNonQuery();

        List<int> actual = db.Table<NullableEntity>().Where(x => 5 == x.Value).Select(x => x.Id).ToList();

        Assert.Equal(new[] { 2 }, actual);
    }

    [Fact]
    public void FlagsEnumWithSingleAndCombinedMembersToStrings()
    {
        using TestDatabase db = new();
        db.Table<PermissionRow>().Schema.CreateTable();
        db.Table<PermissionRow>().Add(new PermissionRow { Id = 1, Access = Permission.ReadWrite });

        string actual = db.Table<PermissionRow>().Select(p => p.Access.ToString()).Single();

        Assert.Equal(Permission.ReadWrite.ToString(), actual);
    }
}
