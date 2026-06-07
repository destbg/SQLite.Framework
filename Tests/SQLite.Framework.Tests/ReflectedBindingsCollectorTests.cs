using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReflectedBindingsCollectorTests
{
    [Fact]
    public void Select_PrivateInstanceMethodOnCapturedReceiver_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<RbcSimpleEntity>().Schema.CreateTable();
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
        db.Table<RbcSimpleEntity>().Schema.CreateTable();
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
    public void Collector_VisitsNonPublicStaticMethodCall_AddsNullInstance()
    {
        MethodInfo method = typeof(ReflectedBindingsCollectorTests)
            .GetMethod(nameof(StaticNonPublicEcho), BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodCallExpression call = Expression.Call(method, Expression.Constant(5));

        Internals.Helpers.ReflectedBindingsCollector collector = new();
        collector.Visit(call);

        Assert.Single(collector.Methods);
        Assert.Single(collector.Instances);
        Assert.Null(collector.Instances[0]);
    }

    [Fact]
    public void Collector_VisitsNonPublicInstanceMethodOnNonConstantReceiver_AddsNullInstance()
    {
        MethodInfo method = typeof(PrivateInstanceUtilForRbc)
            .GetMethod(nameof(PrivateInstanceUtilForRbc.Echo))!;
        ParameterExpression p = Expression.Parameter(typeof(PrivateInstanceUtilForRbc), "u");
        MethodCallExpression call = Expression.Call(p, method, Expression.Constant("a"));

        Internals.Helpers.ReflectedBindingsCollector collector = new();
        collector.Visit(call);

        Assert.Single(collector.Methods);
        Assert.Single(collector.Instances);
        Assert.Null(collector.Instances[0]);
    }

    internal static int StaticNonPublicEcho(int x) => x;

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

internal class PrivateInstanceUtilForRbc
{
    public string Echo(string s) => s;
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
