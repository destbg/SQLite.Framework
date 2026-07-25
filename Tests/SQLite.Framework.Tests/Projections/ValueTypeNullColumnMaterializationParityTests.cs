using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ValueTypeNullColumnRow
{
    [Key]
    public int Id { get; set; }

    public short ShortScore { get; set; } = -1;

    public int IntScore { get; set; } = -1;
}

public class ValueTypeNullColumnMaterializationParityTests
{
    [Fact]
    public void NonNullableValueTypePropertiesReadingSameNullColumn_AreConsistent()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"ValueTypeNullColumnRow\" (\"Id\" INTEGER PRIMARY KEY, \"ShortScore\" INTEGER, \"IntScore\" INTEGER)");
        db.Execute("INSERT INTO \"ValueTypeNullColumnRow\" (\"Id\", \"ShortScore\", \"IntScore\") VALUES (1, NULL, NULL)");

        ValueTypeNullColumnRow row = db.Table<ValueTypeNullColumnRow>().First(r => r.Id == 1);

        Assert.Equal((short)0, row.ShortScore);
        Assert.Equal(0, row.IntScore);
        Assert.Equal(row.IntScore, row.ShortScore);
    }
}
