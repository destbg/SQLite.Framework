using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CteNotMappedRows")]
public class CteNotMappedRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    [NotMapped]
    public string Extra { get; set; } = "default";
}

public class CteNotMappedElementParityTests
{
    [Fact]
    public void DirectQueryOverEntityWithNotMappedProperty_RoundTripsMappedColumns()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<CteNotMappedRow>();
        db.Table<CteNotMappedRow>().Add(new CteNotMappedRow { Id = 1, Title = "first" });

        List<string> actual = db.Table<CteNotMappedRow>().Select(c => c.Title).ToList();

        Assert.Equal(new List<string> { "first" }, actual);
    }

    [Fact]
    public void NonRecursiveCteOverEntityWithNotMappedProperty_RoundTripsMappedColumns()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<CteNotMappedRow>();
        db.Table<CteNotMappedRow>().Add(new CteNotMappedRow { Id = 1, Title = "first" });

        List<CteNotMappedRow> seed = new()
        {
            new CteNotMappedRow { Id = 1, Title = "first" }
        };
        List<string> oracle = (from c in seed select c).Select(c => c.Title).ToList();

        SQLiteCte<CteNotMappedRow> cte = db.With(() => db.Table<CteNotMappedRow>());
        List<string> actual = (from c in cte select c).ToList().Select(c => c.Title).ToList();

        Assert.Equal(oracle, actual);
    }
}
