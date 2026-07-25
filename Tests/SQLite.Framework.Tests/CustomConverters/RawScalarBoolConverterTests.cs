using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RawScalarBoolConverterTests
{
    private sealed class YesNoBoolConverter : ISQLiteTypeConverter
    {
        public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

        public object? ToDatabase(object? value)
        {
            return (bool)value! ? "Y" : "N";
        }

        public object? FromDatabase(object? value)
        {
            return value is string s && s == "Y";
        }
    }

    [Fact]
    public void RegisteredBoolConverter_HandlesRawScalarTextValue()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(bool)] = new YesNoBoolConverter());

        Assert.True(db.ExecuteScalar<bool>("SELECT 'Y'"));
        Assert.False(db.ExecuteScalar<bool>("SELECT 'N'"));
    }
}
