using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReflectedBindingsCollectorCoverageTests
{
    [Fact]
    public void Select_PrivateInstanceMethodOnCapturedReceiver_RoundTrips()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<RbcSimpleEntity>();
        db.Table<RbcSimpleEntity>().Add(new RbcSimpleEntity { Id = 1, Tag = "alpha" });

        PrivateInstanceUtil util = new();
        var rows = db.Table<RbcSimpleEntity>()
            .Select(x => new { x.Id, Stamped = util.Stamp(x.Tag) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal("[alpha]", rows[0].Stamped);
    }

    [Fact]
    public void Select_InternalInstanceMethodOnCapturedPublicReceiver_RoundTrips()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<RbcSimpleEntity>();
        db.Table<RbcSimpleEntity>().Add(new RbcSimpleEntity { Id = 1, Tag = "alpha" });

        RbcStamper util = new();
        var rows = db.Table<RbcSimpleEntity>()
            .Select(x => new { x.Id, Stamped = util.StampInternal(x.Tag) })
            .ToList();

        Assert.Single(rows);
        Assert.Equal("[alpha]", rows[0].Stamped);
    }

    [Fact]
    public void Collector_VisitsNonConstantListInit_CollectsNothingAndDoesNotFail()
    {
        ParameterExpression p = Expression.Parameter(typeof(int), "x");
        ListInitExpression listInit = Expression.ListInit(
            Expression.New(typeof(List<int>)),
            Expression.ElementInit(typeof(List<int>).GetMethod("Add", new[] { typeof(int) })!, p),
            Expression.ElementInit(typeof(List<int>).GetMethod("Add", new[] { typeof(int) })!, Expression.Constant(99)));

        Internals.Helpers.ReflectedBindingsCollector collector = new();
        collector.Visit(listInit);

        Assert.Empty(collector.CapturedValues);
    }

    [Fact]
    public void Collector_VisitsMemberInitOnPrivateTypeWithMemberMemberBinding_RecursesIntoSubBindings()
    {
        MemberInfo innerMember = typeof(RbcCollectorPrivateOuter).GetProperty(nameof(RbcCollectorPrivateOuter.Inner))!;
        MemberInfo xMember = typeof(RbcCollectorPrivateInner).GetProperty(nameof(RbcCollectorPrivateInner.X))!;

        MemberInitExpression init = Expression.MemberInit(
            Expression.New(typeof(RbcCollectorPrivateOuter)),
            Expression.MemberBind(
                innerMember,
                Expression.Bind(xMember, Expression.Constant(7))));

        Internals.Helpers.ReflectedBindingsCollector collector = new();
        collector.Visit(init);

        Assert.Contains(collector.Members, m => m.Name == nameof(RbcCollectorPrivateOuter.Inner));
        Assert.Contains(collector.Members, m => m.Name == nameof(RbcCollectorPrivateInner.X));
    }

    private sealed class PrivateInstanceUtil
    {
        public string Stamp(string s) => "[" + s + "]";
    }
}

public class RbcStamper
{
    internal string StampInternal(string s) => "[" + s + "]";
}

public class RbcSimpleEntity
{
    [Key]
    public int Id { get; set; }

    public string Tag { get; set; } = string.Empty;
}

public class RbcRowList
{
    public List<int> Items { get; } = [];
}

public class RbcOuter
{
    public RbcInner Inner { get; } = new();
}

public class RbcInner
{
    public List<int> Items { get; } = [];
}

internal class RbcCollectorPrivateOuter
{
    public RbcCollectorPrivateInner Inner { get; set; } = new();
}

internal class RbcCollectorPrivateInner
{
    public int X { get; set; }
}
