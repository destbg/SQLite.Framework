using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class YesNoNullableConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public object? ToDatabase(object? value)
    {
        return value == null ? null : (bool)value ? "yes" : "no";
    }

    public object? FromDatabase(object? value)
    {
        return value == null ? null : (string)value == "yes";
    }
}

[Table("FlaggedNote")]
public class FlaggedNoteRow
{
    [Key]
    public int Id { get; set; }

    public bool? Flag { get; set; }
}

public class NullableKeyedConverterTests
{
    [Fact]
    public void ConverterRegisteredForANullableTypeIsUsed()
    {
        using TestDatabase db = new(b => b.AddTypeConverter<bool?>(new YesNoNullableConverter()));
        db.Table<FlaggedNoteRow>().Schema.CreateTable();
        db.Table<FlaggedNoteRow>().Add(new FlaggedNoteRow { Id = 1, Flag = true });

        Assert.Equal("yes", db.ExecuteScalar<string>("SELECT \"Flag\" FROM \"FlaggedNote\" WHERE \"Id\" = 1"));
    }
}
