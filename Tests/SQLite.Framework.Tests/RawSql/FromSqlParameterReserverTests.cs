using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class FromSqlParameterReserverTests
{
    private static MethodInfo Method(Type secondParameterType)
    {
        return typeof(FromSqlParameterReserverTests)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .First(m => m.Name == "FromSql"
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType == secondParameterType);
    }

    private static string FromSql(string sql, SQLiteParameter[] parameters)
    {
        return sql + parameters.Length;
    }

    private static string FromSql(string sql, int count)
    {
        return sql + count;
    }

    private static string FromSql(string sql)
    {
        return sql;
    }

    [Fact]
    public void ReservesUserParameterNames()
    {
        SQLiteCounters counters = new("@p");
        MethodCallExpression call = Expression.Call(
            Method(typeof(SQLiteParameter[])),
            Expression.Constant("SELECT 1"),
            Expression.Constant(new SQLiteParameter[] { new() { Name = "@p0", Value = 1 } }));

        FromSqlParameterReserver.Reserve(call, counters);

        Assert.Equal("@p1", counters.NextParamName());
    }

    [Fact]
    public void SkipsNonConstantParameterArgument()
    {
        SQLiteCounters counters = new("@p");
        ParameterExpression args = Expression.Parameter(typeof(SQLiteParameter[]), "args");
        LambdaExpression lambda = Expression.Lambda(
            Expression.Call(Method(typeof(SQLiteParameter[])), Expression.Constant("SELECT 1"), args), args);

        FromSqlParameterReserver.Reserve(lambda, counters);

        Assert.Equal("@p0", counters.NextParamName());
    }

    [Fact]
    public void SkipsNonEnumerableConstantArgument()
    {
        SQLiteCounters counters = new("@p");
        MethodCallExpression call = Expression.Call(
            Method(typeof(int)),
            Expression.Constant("SELECT 1"),
            Expression.Constant(5));

        FromSqlParameterReserver.Reserve(call, counters);

        Assert.Equal("@p0", counters.NextParamName());
    }

    [Fact]
    public void SkipsSingleArgumentOverload()
    {
        SQLiteCounters counters = new("@p");
        MethodInfo single = typeof(FromSqlParameterReserverTests)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .First(m => m.Name == "FromSql" && m.GetParameters().Length == 1);
        MethodCallExpression call = Expression.Call(single, Expression.Constant("SELECT 1"));

        FromSqlParameterReserver.Reserve(call, counters);

        Assert.Equal("@p0", counters.NextParamName());
    }

    [Fact]
    public void VisitsCteBodyOnce()
    {
        using TestDatabase db = new();
        SQLiteCte<int> cte = db.With(() => db.FromSql<int>("SELECT 5", new SQLiteParameter { Name = "@p0", Value = 1 }));

        SQLiteCounters counters = new("@p");
        Expression pair = Expression.NewArrayInit(typeof(object), Expression.Constant(cte), Expression.Constant(cte));

        FromSqlParameterReserver.Reserve(pair, counters);

        Assert.Equal("@p1", counters.NextParamName());
    }

    [Fact]
    public void VisitsCapturedQueryableOnce()
    {
        using TestDatabase db = new();
        IQueryable<int> query = db.FromSql<int>("SELECT 5", new SQLiteParameter { Name = "@p0", Value = 1 });

        SQLiteCounters counters = new("@p");
        Expression pair = Expression.NewArrayInit(typeof(object), Expression.Constant(query), Expression.Constant(query));

        FromSqlParameterReserver.Reserve(pair, counters);

        Assert.Equal("@p1", counters.NextParamName());
    }
}
