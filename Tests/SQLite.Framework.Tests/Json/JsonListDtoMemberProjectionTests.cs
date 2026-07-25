using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("j19df_docs")]
public sealed class Json19dfDocRow
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public List<int> Numbers { get; set; } = [];
}

[Table("j19df_parents")]
public sealed class Json19dfParentRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("j19df_children")]
public sealed class Json19dfChildRow
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Name { get; set; } = "";

    public List<int> Tags { get; set; } = [];
}

public sealed class Json19dfChildInfo
{
    public string Name { get; set; } = "";

    public List<int> Doubled { get; set; } = [];
}

[JsonSerializable(typeof(List<int>))]
internal partial class Json19dfNestedContext : JsonSerializerContext;

public class JsonListDtoMemberProjectionTests
{
    private static TestDatabase CreateDocDb(out List<Json19dfDocRow> docs)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(Json19dfNestedContext.Default.ListInt32));
        db.Table<Json19dfDocRow>().Schema.CreateTable();
        docs =
        [
            new Json19dfDocRow { Id = 1, Title = "t1", Numbers = [1, 2, 3] },
            new Json19dfDocRow { Id = 2, Title = "t2", Numbers = [] },
            new Json19dfDocRow { Id = 3, Title = "t3", Numbers = [7] }
        ];
        foreach (Json19dfDocRow d in docs)
        {
            db.Table<Json19dfDocRow>().Add(d);
        }

        return db;
    }

    private static TestDatabase CreateJoinDb(out List<Json19dfParentRow> parents, out List<Json19dfChildRow> children)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] =
                new SQLiteJsonConverter<List<int>>(Json19dfNestedContext.Default.ListInt32));
        db.Table<Json19dfParentRow>().Schema.CreateTable();
        db.Table<Json19dfChildRow>().Schema.CreateTable();
        parents =
        [
            new Json19dfParentRow { Id = 1, Name = "p1" },
            new Json19dfParentRow { Id = 2, Name = "p2" },
            new Json19dfParentRow { Id = 3, Name = "p3" }
        ];
        children =
        [
            new Json19dfChildRow { Id = 1, ParentId = 1, Name = "c1", Tags = [1, 2] },
            new Json19dfChildRow { Id = 2, ParentId = 1, Name = "c2", Tags = [] },
            new Json19dfChildRow { Id = 3, ParentId = 2, Name = "c3", Tags = [5] }
        ];
        foreach (Json19dfParentRow p in parents)
        {
            db.Table<Json19dfParentRow>().Add(p);
        }

        foreach (Json19dfChildRow c in children)
        {
            db.Table<Json19dfChildRow>().Add(c);
        }

        return db;
    }

    [Fact]
    public void AnonymousDtoWithProjectedJsonList()
    {
        using TestDatabase db = CreateDocDb(out List<Json19dfDocRow> docs);

        var expected = docs
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Doubled = r.Numbers.Select(x => x * 2).ToList() })
            .ToList();
        var actual = db.Table<Json19dfDocRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Doubled = r.Numbers.Select(x => x * 2).ToList() })
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Id, actual[i].Id);
            Assert.Equal(expected[i].Doubled, actual[i].Doubled);
        }
    }

    [Fact]
    public void AnonymousDtoWithListAndScalarAggregate()
    {
        using TestDatabase db = CreateDocDb(out List<Json19dfDocRow> docs);

        var expected = docs
            .OrderBy(r => r.Id)
            .Select(r => new { Total = r.Numbers.Sum(), Kept = r.Numbers.Where(x => x > 1).ToList() })
            .ToList();
        var actual = db.Table<Json19dfDocRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { Total = r.Numbers.Sum(), Kept = r.Numbers.Where(x => x > 1).ToList() })
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Total, actual[i].Total);
            Assert.Equal(expected[i].Kept, actual[i].Kept);
        }
    }

    [Fact]
    public void LeftJoinConditionalProjectedList()
    {
        using TestDatabase db = CreateJoinDb(out List<Json19dfParentRow> parents, out List<Json19dfChildRow> children);

        var expected = (from p in parents
            join c in children on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            orderby p.Id, c == null ? -1 : c.Id
            select new { p.Id, Doubled = c == null ? null : c.Tags.Select(x => x * 2).ToList() }).ToList();
        var actual = (from p in db.Table<Json19dfParentRow>()
            join c in db.Table<Json19dfChildRow>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            orderby p.Id, c == null ? -1 : c.Id
            select new { p.Id, Doubled = c == null ? null : c.Tags.Select(x => x * 2).ToList() }).ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Id, actual[i].Id);
            Assert.Equal(expected[i].Doubled, actual[i].Doubled);
        }
    }

    [Fact]
    public void LeftJoinNestedDtoWithListMember()
    {
        using TestDatabase db = CreateJoinDb(out List<Json19dfParentRow> parents, out List<Json19dfChildRow> children);

        var expected = (from p in parents
            join c in children on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            orderby p.Id, c == null ? -1 : c.Id
            select new
            {
                p.Id,
                Info = c == null ? null : new Json19dfChildInfo { Name = c.Name, Doubled = c.Tags.Select(t => t * 2).ToList() }
            }).ToList();
        var actual = (from p in db.Table<Json19dfParentRow>()
            join c in db.Table<Json19dfChildRow>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            orderby p.Id, c == null ? -1 : c.Id
            select new
            {
                p.Id,
                Info = c == null ? null : new Json19dfChildInfo { Name = c.Name, Doubled = c.Tags.Select(t => t * 2).ToList() }
            }).ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Id, actual[i].Id);
            Assert.Equal(expected[i].Info == null, actual[i].Info == null);
            if (expected[i].Info != null)
            {
                Assert.Equal(expected[i].Info!.Name, actual[i].Info!.Name);
                Assert.Equal(expected[i].Info!.Doubled, actual[i].Info!.Doubled);
            }
        }
    }
}
