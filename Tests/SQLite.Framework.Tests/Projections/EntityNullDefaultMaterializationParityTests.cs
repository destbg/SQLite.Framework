using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NullDefaultRows")]
internal sealed class NullDefaultRow
{
    [Key]
    public int Id { get; set; }

    public int? Score { get; set; } = 99;

    public byte[]? Data { get; set; } = [1, 2, 3];

    public short Small { get; set; } = 50;
}

internal sealed class OrphanLeftRow
{
    [Key]
    public int Id { get; set; }

    public int RightId { get; set; }
}

internal sealed class OrphanRightRow
{
    [Key]
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public Guid Token { get; set; }

    public DateTime When { get; set; }
}

public class EntityNullDefaultMaterializationParityTests
{
    [Fact]
    public void StoredNull_OverwritesNonNullPropertyDefaults_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<NullDefaultRow>();
        db.Table<NullDefaultRow>().Add(new NullDefaultRow { Id = 1, Score = null, Data = null, Small = 0 });

        NullDefaultRow oracle = new List<NullDefaultRow> { new() { Id = 1, Score = null, Data = null, Small = 0 } }.First();

        NullDefaultRow actual = db.Table<NullDefaultRow>().First();

        Assert.Equal(oracle.Score, actual.Score);
        Assert.Equal(oracle.Data, actual.Data);
        Assert.Equal(oracle.Small, actual.Small);
    }

    [Fact]
    public void LeftJoinOrphanValueTypeColumns_MaterializeAsNullEntity_MatchesLinqToObjects()
    {
        Guid token = new("11111111-1111-1111-1111-111111111111");
        DateTime when = new(2020, 5, 6, 7, 8, 9);

        using TestDatabase db = new();
        db.Table<OrphanLeftRow>().Schema.CreateTable();
        db.Table<OrphanRightRow>().Schema.CreateTable();
        db.Table<OrphanLeftRow>().Add(new OrphanLeftRow { Id = 1, RightId = 10 });
        db.Table<OrphanLeftRow>().Add(new OrphanLeftRow { Id = 2, RightId = 99 });
        db.Table<OrphanRightRow>().Add(new OrphanRightRow { Id = 10, Amount = 5.5m, Token = token, When = when });

        OrphanLeftRow[] lefts =
        [
            new OrphanLeftRow { Id = 1, RightId = 10 },
            new OrphanLeftRow { Id = 2, RightId = 99 }
        ];
        OrphanRightRow[] rights = [new OrphanRightRow { Id = 10, Amount = 5.5m, Token = token, When = when }];

        List<(int leftId, bool rightNull, decimal amount)> expected = (from l in lefts
                join r in rights on l.RightId equals r.Id into g
                from r in g.DefaultIfEmpty()
                select new { l.Id, R = r })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.R == null, x.R?.Amount ?? 0m))
            .ToList();

        List<(int leftId, bool rightNull, decimal amount)> actual = (from l in db.Table<OrphanLeftRow>()
                join r in db.Table<OrphanRightRow>() on l.RightId equals r.Id into g
                from r in g.DefaultIfEmpty()
                select new { l.Id, R = r })
            .ToList()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.R == null, x.R?.Amount ?? 0m))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
