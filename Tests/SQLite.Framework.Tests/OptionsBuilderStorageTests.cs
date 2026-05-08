using System.Reflection;
using SQLite.Framework.Enums;

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
namespace SQLite.Framework.Tests;

internal sealed class TestPassThroughConverterForOptions : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;
    public object? ToDatabase(object? value) => value;
    public object? FromDatabase(object? value) => value;
}

public class OptionsBuilderStorageTests
{
    [Fact]
    public void UseOpenFlags_SetsFlagsAndReturnsSameBuilder()
    {
        SQLiteOptionsBuilder b = new("test.db");
        SQLiteOptionsBuilder result = b.UseOpenFlags(SQLiteOpenFlags.ReadOnly);

        Assert.Same(b, result);
        Assert.Equal(SQLiteOpenFlags.ReadOnly, b.OpenFlags);
    }

    [Fact]
    public void UseEnumStorage_SetsStorageAndReturnsSameBuilder()
    {
        SQLiteOptionsBuilder b = new("test.db");
        SQLiteOptionsBuilder result = b.UseEnumStorage(EnumStorageMode.Text);

        Assert.Same(b, result);
        Assert.Equal(EnumStorageMode.Text, b.EnumStorage);
    }

    [Fact]
    public void AddTypeConverter_NonGeneric_RegistersAndReturnsSameBuilder()
    {
        SQLiteOptionsBuilder b = new("test.db");
        TestPassThroughConverterForOptions converter = new();

        SQLiteOptionsBuilder result = b.AddTypeConverter(typeof(string), converter);

        Assert.Same(b, result);
        Assert.Same(converter, b.TypeConverters[typeof(string)]);
    }

    [Fact]
    public void AddMethodTranslator_RegistersAndReturnsSameBuilder()
    {
        SQLiteOptionsBuilder b = new("test.db");
        MethodInfo method = typeof(string).GetMethod(nameof(string.Trim), Type.EmptyTypes)!;

        SQLiteOptionsBuilder result = b.AddMethodTranslator(method, _ => null!);

        Assert.Same(b, result);
        Assert.True(b.MemberTranslators.ContainsKey(method));
    }

    [Fact]
    public void AddPropertyTranslator_AppendsAndReturnsSameBuilder()
    {
        SQLiteOptionsBuilder b = new("test.db");
        int countBefore = b.PropertyTranslators.Count;

        SQLiteOptionsBuilder result = b.AddPropertyTranslator((_, _) => null);

        Assert.Same(b, result);
        Assert.Equal(countBefore + 1, b.PropertyTranslators.Count);
    }

    [Fact]
    public void UseDateTimeStorage_BothBranches()
    {
        SQLiteOptionsBuilder a = new("test.db");
        a.UseDateTimeStorage(DateTimeStorageMode.TextTicks);
        Assert.Equal(DateTimeStorageMode.TextTicks, a.DateTimeStorage);

        SQLiteOptionsBuilder b = new("test.db");
        b.UseDateTimeStorage(DateTimeStorageMode.TextFormatted, "yyyy-MM-dd");
        Assert.Equal("yyyy-MM-dd", b.DateTimeFormat);
    }

    [Fact]
    public void UseDateTimeOffsetStorage_BothBranches()
    {
        SQLiteOptionsBuilder a = new("test.db");
        a.UseDateTimeOffsetStorage(DateTimeOffsetStorageMode.UtcTicks);
        Assert.Equal(DateTimeOffsetStorageMode.UtcTicks, a.DateTimeOffsetStorage);

        SQLiteOptionsBuilder b = new("test.db");
        b.UseDateTimeOffsetStorage(DateTimeOffsetStorageMode.TextFormatted, "O");
        Assert.Equal("O", b.DateTimeOffsetFormat);
    }

    [Fact]
    public void UseTimeSpanStorage_BothBranches()
    {
        SQLiteOptionsBuilder a = new("test.db");
        a.UseTimeSpanStorage(TimeSpanStorageMode.Text);
        Assert.Equal(TimeSpanStorageMode.Text, a.TimeSpanStorage);

        SQLiteOptionsBuilder b = new("test.db");
        b.UseTimeSpanStorage(TimeSpanStorageMode.Text, "c");
        Assert.Equal("c", b.TimeSpanFormat);
    }

    [Fact]
    public void UseDateOnlyStorage_BothBranches()
    {
        SQLiteOptionsBuilder a = new("test.db");
        a.UseDateOnlyStorage(DateOnlyStorageMode.Text);
        Assert.Equal(DateOnlyStorageMode.Text, a.DateOnlyStorage);

        SQLiteOptionsBuilder b = new("test.db");
        b.UseDateOnlyStorage(DateOnlyStorageMode.Text, "yyyy-MM-dd");
        Assert.Equal("yyyy-MM-dd", b.DateOnlyFormat);
    }

    [Fact]
    public void UseTimeOnlyStorage_BothBranches()
    {
        SQLiteOptionsBuilder a = new("test.db");
        a.UseTimeOnlyStorage(TimeOnlyStorageMode.Text);
        Assert.Equal(TimeOnlyStorageMode.Text, a.TimeOnlyStorage);

        SQLiteOptionsBuilder b = new("test.db");
        b.UseTimeOnlyStorage(TimeOnlyStorageMode.Text, "HH:mm:ss");
        Assert.Equal("HH:mm:ss", b.TimeOnlyFormat);
    }

    [Fact]
    public void UseDecimalStorage_BothBranches()
    {
        SQLiteOptionsBuilder a = new("test.db");
        a.UseDecimalStorage(DecimalStorageMode.Text);
        Assert.Equal(DecimalStorageMode.Text, a.DecimalStorage);

        SQLiteOptionsBuilder b = new("test.db");
        b.UseDecimalStorage(DecimalStorageMode.Text, "G29");
        Assert.Equal("G29", b.DecimalFormat);
    }
}
#endif
