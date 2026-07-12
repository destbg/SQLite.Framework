using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("PcvRow")]
public class PcvRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    public DateTime When { get; set; }
}

public class PcvObjDto
{
    public object? Tag { get; set; }
}

[Table("PcvRoRows")]
public class PcvRoRow
{
    public PcvRoRow(int id)
    {
        Id = id;
    }

    [Key]
    public int Id { get; }

    public int? Score { get; }

    public DayOfWeek Kind { get; }

    public string? Missing { get; }

    public int this[int index] => index;
}

public class ProjectionCoercionVariantTests
{
    private static List<PcvRow> Rows() =>
    [
        new PcvRow { Id = 1, Name = "a", When = new DateTime(2024, 1, 1) },
        new PcvRow { Id = 2, Name = null, When = new DateTime(2024, 1, 2) },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<PcvRow>().Schema.CreateTable();
        db.Table<PcvRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ConditionalDtoWithBoxedObjectMemberReadsStorageType()
    {
        using TestDatabase db = Setup();

        List<object?> actual = db.Table<PcvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Dto = r.Id > 1 ? new PcvObjDto { Tag = r.Id } : null })
            .Select(x => x.Dto == null ? null : x.Dto.Tag)
            .ToList();

        List<object?> expected = db.Options.ReflectionFallbackDisabled ? [null, 2] : [null, 2L];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReadOnlyPropertiesReadThroughBackingFields()
    {
        using TestDatabase db = new();
        db.Table<PcvRoRow>().Schema.CreateTable();
        db.Execute("INSERT INTO \"PcvRoRows\" (\"Id\", \"Score\", \"Kind\", \"Missing\") VALUES (1, 5, 2, NULL)");
        db.Execute("INSERT INTO \"PcvRoRows\" (\"Id\", \"Score\", \"Kind\", \"Missing\") VALUES (2, NULL, 0, 'kept')");

        List<PcvRoRow> rows = db.Table<PcvRoRow>().OrderBy(r => r.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal(5, rows[0].Score);
        Assert.Equal(DayOfWeek.Tuesday, rows[0].Kind);
        Assert.Null(rows[0].Missing);
        Assert.Equal(2, rows[1].Id);
        Assert.Null(rows[1].Score);
        Assert.Equal(DayOfWeek.Sunday, rows[1].Kind);
        Assert.Equal("kept", rows[1].Missing);
    }

    [Fact]
    public void GetSelectValueTypeReadsRecordedTypes()
    {
        SQLiteQueryContext withTypes = new()
        {
            SelectValueTypes = new Dictionary<string, Type> { ["Tag"] = typeof(int) }
        };
        SQLiteQueryContext withoutTypes = new();

        Assert.Equal(typeof(int), withTypes.GetSelectValueType("Tag"));
        Assert.Null(withTypes.GetSelectValueType("Other"));
        Assert.Null(withoutTypes.GetSelectValueType("Tag"));
    }
}
