using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class NumListRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

file enum CStatus
{
    Active = 0,
    Archived = 1
}

file sealed class CStatusConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    public object? ToDatabase(object? value) => (CStatus)value! == CStatus.Active ? "A" : "X";

    public object? FromDatabase(object? value) => (string?)value == "A" ? CStatus.Active : CStatus.Archived;
}

file sealed class CStatusRow
{
    [Key]
    public int Id { get; set; }

    public CStatus Value { get; set; }
}

public class JsonConverterProbeBugTests
{
    private static TestDatabase CreateNumListDb()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<NumListRow>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public void DistinctBeforeCountRemovesDuplicates()
    {
        using TestDatabase db = CreateNumListDb();
        db.Table<NumListRow>().Add(new NumListRow { Id = 1, Numbers = [1, 1, 2, 2, 3] });

        int distinctCount = db.Table<NumListRow>().Select(r => r.Numbers.Distinct().Count()).First();

        Assert.Equal(3, distinctCount);
    }

    [Fact]
    public void DistinctBeforeSumRemovesDuplicates()
    {
        using TestDatabase db = CreateNumListDb();
        db.Table<NumListRow>().Add(new NumListRow { Id = 1, Numbers = [1, 1, 2, 2, 3] });

        int distinctSum = db.Table<NumListRow>().Select(r => r.Numbers.Distinct().Sum()).First();

        Assert.Equal(6, distinctSum);
    }

    [Fact]
    public void SelectBeforeSumKeepsProjection()
    {
        using TestDatabase db = CreateNumListDb();
        db.Table<NumListRow>().Add(new NumListRow { Id = 1, Numbers = [1, 2, 3] });

        int sum = db.Table<NumListRow>().Select(r => r.Numbers.Select(x => x + x).Sum()).First();

        Assert.Equal(12, sum);
    }

    [Fact]
    public void CustomEnumConverterIsUsedOnRead()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(CStatus)] = new CStatusConverter());
        db.Table<CStatusRow>().Schema.CreateTable();
        db.Table<CStatusRow>().Add(new CStatusRow { Id = 1, Value = CStatus.Archived });

        CStatus back = db.Table<CStatusRow>().First().Value;

        Assert.Equal(CStatus.Archived, back);
    }
}
