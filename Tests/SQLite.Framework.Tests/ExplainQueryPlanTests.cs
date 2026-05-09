using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExplainQueryPlanTests
{
    [Fact]
    public void ExplainQueryPlan_SimpleScan_ReturnsScanNode()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteQueryPlan plan = db.Table<Book>().ExplainQueryPlan();

        Assert.NotEmpty(plan.Roots);
        Assert.Contains(plan.Roots, r => r.Detail.Contains("SCAN"));
    }

    [Fact]
    public void ExplainQueryPlan_IndexedWhere_ReturnsSearchNode()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteQueryPlan plan = db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .ExplainQueryPlan();

        Assert.Contains(plan.Roots, r =>
            r.Detail.Contains("SEARCH") && r.Detail.Contains("IX_Book_AuthorId"));
    }

    [Fact]
    public void ExplainQueryPlan_Join_ReturnsTwoNodes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        SQLiteQueryPlan plan = (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            select new { b.Title, a.Name }
        ).ExplainQueryPlan();

        Assert.True(plan.Roots.Count >= 2);
    }

    [Fact]
    public void ExplainQueryPlan_CorrelatedSubquery_ProducesNestedNode()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        SQLiteQueryPlan plan = db.Table<Book>()
            .Where(b => db.Table<Author>().Any(a => a.Id == b.AuthorId))
            .ExplainQueryPlan();

        bool hasNested = plan.Roots.Any(r => r.Children.Count > 0)
            || plan.Roots.Any(r => HasNested(r));
        Assert.True(hasNested);

        static bool HasNested(SQLiteQueryPlanNode n) => n.Children.Count > 0 || n.Children.Any(HasNested);
    }

    [Fact]
    public void ExplainQueryPlan_ToString_RendersTree()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteQueryPlan plan = db.Table<Book>().Where(b => b.AuthorId == 1).ExplainQueryPlan();
        string text = plan.ToString();

        Assert.StartsWith("QUERY PLAN", text);
        Assert.Contains("> ", text);
        Assert.DoesNotContain('─', text);
        Assert.DoesNotContain('├', text);
    }

    [Fact]
    public void ExplainQueryPlan_NodeIdsAreUnique()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        SQLiteQueryPlan plan = (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            select new { b.Title, a.Name }
        ).ExplainQueryPlan();

        HashSet<int> ids = [];
        AddIds(plan.Roots, ids);
        Assert.Equal(plan.Roots.Sum(CountAll), ids.Count);

        static int CountAll(SQLiteQueryPlanNode node) => 1 + node.Children.Sum(CountAll);
        static void AddIds(IReadOnlyList<SQLiteQueryPlanNode> nodes, HashSet<int> ids)
        {
            foreach (SQLiteQueryPlanNode n in nodes)
            {
                ids.Add(n.Id);
                AddIds(n.Children, ids);
            }
        }
    }

    [Fact]
    public void ExplainQueryPlan_WrongQueryType_Throws()
    {
        using TestDatabase db = new();
        IQueryable<Book> nonFramework = new List<Book>().AsQueryable();

        Assert.Throws<InvalidOperationException>(() => nonFramework.ExplainQueryPlan());
    }

    [Fact]
    public async Task ExplainQueryPlanAsync_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLiteQueryPlan plan = await db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .ExplainQueryPlanAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(plan.Roots);
    }

    [Fact]
    public async Task ExplainQueryPlanAsync_WrongQueryType_Throws()
    {
        IQueryable<Book> nonFramework = new List<Book>().AsQueryable();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nonFramework.ExplainQueryPlanAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void QueryPlan_ToString_NestedNodes_AreIndented()
    {
        SQLiteQueryPlan plan = new()
        {
            Roots =
            [
                new SQLiteQueryPlanNode
                {
                    Id = 1,
                    ParentId = 0,
                    Detail = "OUTER",
                    Children =
                    [
                        new SQLiteQueryPlanNode { Id = 2, ParentId = 1, Detail = "INNER", Children = [] },
                    ],
                },
            ],
        };

        string text = plan.ToString().Replace("\r\n", "\n");

        Assert.Equal("QUERY PLAN\n> OUTER\n  > INNER", text);
    }
}
