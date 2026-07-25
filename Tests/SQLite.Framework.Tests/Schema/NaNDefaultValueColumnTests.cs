using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NanDefaultRow
{
    [Key]
    public int Id { get; set; }

    [DefaultValue(double.NaN)]
    public double? Reading { get; set; }
}

public class NaNDefaultValueColumnTests
{
    [Fact]
    public void NaNDefaultStoresNullLikeTheWritePath()
    {
        using TestDatabase db = new();
        db.Table<NanDefaultRow>().Schema.CreateTable();

        db.Execute("INSERT INTO \"NanDefaultRow\" (\"Id\") VALUES (1)");
        db.Table<NanDefaultRow>().Add(new NanDefaultRow { Id = 2, Reading = double.NaN });

        string defaultedType = db.ExecuteScalar<string>("SELECT typeof(\"Reading\") FROM \"NanDefaultRow\" WHERE \"Id\" = 1")!;
        string writtenType = db.ExecuteScalar<string>("SELECT typeof(\"Reading\") FROM \"NanDefaultRow\" WHERE \"Id\" = 2")!;

        Assert.Equal(writtenType, defaultedType);

        List<int> nullReadingIds = db.Table<NanDefaultRow>()
            .Where(r => r.Reading == null)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 2], nullReadingIds);
    }
}
