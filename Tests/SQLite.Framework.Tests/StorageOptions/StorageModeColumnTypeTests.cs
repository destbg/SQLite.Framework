using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum SchemaStatusKind
{
    Active,
    Closed
}

[StrictTable]
[Table("StrictEnumRows")]
file sealed class StrictEnumRow
{
    [Key]
    public int Id { get; set; }
    public SchemaStatusKind Status { get; set; }
}

[StrictTable]
[Table("StrictDateTimeRows")]
file sealed class StrictDateTimeRow
{
    [Key]
    public int Id { get; set; }
    public DateTime When { get; set; }
}

[Table("EnumDefaultRows")]
file sealed class EnumDefaultRow
{
    [Key]
    public int Id { get; set; }
    [DefaultValue(SchemaStatusKind.Active)]
    public SchemaStatusKind Status { get; set; }
}

public class StorageModeColumnTypeTests
{
    [Fact]
    public void StrictTableWithTextEnumStorageRoundTrips()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<StrictEnumRow>().Schema.CreateTable();
        db.Table<StrictEnumRow>().Add(new StrictEnumRow { Id = 1, Status = SchemaStatusKind.Active });

        StrictEnumRow row = db.Table<StrictEnumRow>().First();

        Assert.Equal(SchemaStatusKind.Active, row.Status);
    }

    [Fact]
    public void StrictTableWithTextFormattedDateTimeRoundTrips()
    {
        using TestDatabase db = new(b => b.UseDateTimeStorage(DateTimeStorageMode.TextFormatted));
        db.Table<StrictDateTimeRow>().Schema.CreateTable();
        db.Table<StrictDateTimeRow>().Add(new StrictDateTimeRow { Id = 1, When = new DateTime(2000, 2, 3, 4, 5, 6) });

        StrictDateTimeRow row = db.Table<StrictDateTimeRow>().First();

        Assert.Equal(new DateTime(2000, 2, 3, 4, 5, 6), row.When);
    }

    [Fact]
    public void EnumDefaultValueAttributeIsSupported()
    {
        using TestDatabase db = new();

        db.Table<EnumDefaultRow>().Schema.CreateTable();

        Assert.NotNull(db.Table<EnumDefaultRow>().ToList());
    }
}
