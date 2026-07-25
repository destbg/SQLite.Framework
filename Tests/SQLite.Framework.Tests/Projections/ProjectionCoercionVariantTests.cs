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

public struct PcvStamp
{
    public int Value { get; set; }
}

public class PcvMixedDto
{
    public object? Tag { get; set; }

    public IComparable? Rank { get; set; }
}

public class PcvIfaceDto
{
    public IComparable? Rank { get; set; }
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

    [Fact]
    public void ConditionalDtoWithStructBoxedObjectMember()
    {
        using TestDatabase db = Setup();

        Func<List<int>> query = () => db.Table<PcvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Dto = r.Id > 1 ? new PcvObjDto { Tag = new PcvStamp { Value = r.Id } } : null })
            .ToList()
            .Select(x => x.Dto == null ? -1 : ((PcvStamp)x.Dto.Tag!).Value)
            .ToList();

        if (db.Options.ReflectionFallbackDisabled)
        {
            Assert.Equal([-1, 2], query());
            return;
        }

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => query());
        Assert.Equal("The new expression 'new PcvStamp()' is not supported.", ex.Message);
    }

    [Fact]
    public void ConditionalDtoWithInterfaceMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Dto = r.Id > 1 ? new PcvMixedDto { Rank = r.Id } : null })
            .Select(x => x.Dto == null ? -1 : Convert.ToInt32(x.Dto.Rank))
            .ToList();

        List<int> actual = db.Table<PcvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Dto = r.Id > 1 ? new PcvMixedDto { Rank = r.Id } : null })
            .ToList()
            .Select(x => x.Dto == null ? -1 : Convert.ToInt32(x.Dto.Rank))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteDtoWithObjectMemberReadsStorageType()
    {
        using TestDatabase db = Setup();

        SQLiteCte<PcvObjDto> cte = db.With(() => db.Table<PcvRow>().OrderBy(r => r.Id).Select(r => new PcvObjDto { Tag = (object)r.Id }));
        List<PcvObjDto> rows = (from d in cte select d).ToList();

        Assert.Equal([1L, 2L], rows.Select(d => d.Tag).ToList());
    }

    [Fact]
    public void CteDtoWithInterfaceMemberReadsValue()
    {
        using TestDatabase db = Setup();

        SQLiteCte<PcvIfaceDto> cte = db.With(() => db.Table<PcvRow>().OrderBy(r => r.Id).Select(r => new PcvIfaceDto { Rank = r.Id }));
        List<PcvIfaceDto> rows = (from d in cte select d).ToList();

        Assert.Equal([1, 2], rows.Select(d => Convert.ToInt32(d.Rank)).ToList());
    }

    [Fact]
    public void DtoWithCoalescedObjectMemberMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<object?> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new PcvObjDto { Tag = r.Name ?? (object)"x" })
            .Select(d => d.Tag)
            .ToList();

        List<PcvObjDto> rows = db.Table<PcvRow>()
            .OrderBy(r => r.Id)
            .Select(r => new PcvObjDto { Tag = r.Name ?? (object)"x" })
            .ToList();

        Assert.Equal(expected, rows.Select(d => d.Tag).ToList());
    }
}
