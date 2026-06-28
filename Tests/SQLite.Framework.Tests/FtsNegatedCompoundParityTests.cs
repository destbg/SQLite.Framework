using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
internal sealed class FtsNegatedDoc
{
    [FullTextRowId] public int Id { get; set; }
    [FullTextIndexed] public string Body { get; set; } = "";
}

public class FtsNegatedCompoundParityTests
{
    [Fact]
    public void NotInsideNot_MatchesWordSetLogic()
    {
        using TestDatabase db = new();
        db.Table<FtsNegatedDoc>().Schema.CreateTable();
        List<FtsNegatedDoc> rows = new()
        {
            new() { Id = 1, Body = "c b" },
            new() { Id = 2, Body = "c a" },
            new() { Id = 3, Body = "c a b" },
        };
        foreach (FtsNegatedDoc r in rows)
        {
            db.Table<FtsNegatedDoc>().Add(r);
        }

        List<int> expected = rows.Where(d =>
        {
            HashSet<string> w = d.Body.Split(' ').ToHashSet();
            return w.Contains("c") && !(w.Contains("a") && !w.Contains("b"));
        }).Select(d => d.Id).OrderBy(x => x).ToList();

        List<int> actual = db.Table<FtsNegatedDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, f => f.Term("c") && !(f.Term("a") && !f.Term("b"))))
            .Select(d => d.Id).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }
}
