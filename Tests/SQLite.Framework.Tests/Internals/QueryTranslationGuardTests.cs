using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors.Queryable;
using SQLite.Framework.Internals.Visitors.Rewriting;
using SQLite.Framework.Internals.Visitors.SQL;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QueryTranslationGuardTests
{
    [Fact]
    public void ByteArrayContainsTranslatorReturnsNullForClientOperands()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        MethodInfo translate = typeof(SQLVisitor).GetMethod(
            "TryTranslateByteArrayContains", BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo contains = typeof(Enumerable).GetMethods()
            .Single(m => m.Name == nameof(Enumerable.Contains)
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(byte));
        MethodInfo returnBytes = typeof(QueryTranslationGuardTests).GetMethod(
            nameof(ReturnBytes), BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo returnByte = typeof(QueryTranslationGuardTests).GetMethod(
            nameof(ReturnByte), BindingFlags.Static | BindingFlags.NonPublic)!;

        MethodCallExpression clientSource = Expression.Call(
            returnBytes,
            Expression.Constant(0),
            Expression.Constant(new byte[] { 1 }));
        MethodCallExpression sourceCall = Expression.Call(
            contains,
            clientSource,
            Expression.Constant((byte)1));

        SQLiteExpression sqlSource = SQLiteExpression.Leaf(typeof(byte[]), 1, "t0.\"Data\"");
        MethodCallExpression clientValue = Expression.Call(
            returnByte,
            Expression.Constant(0),
            Expression.Constant((byte)1));
        MethodCallExpression valueCall = Expression.Call(contains, sqlSource, clientValue);

        Assert.Null(translate.Invoke(visitor, [sourceCall]));
        Assert.Null(translate.Invoke(visitor, [valueCall]));
    }

    [Fact]
    public void ByteArrayContainsTranslatorRejectsUnrelatedContainsMethod()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        MethodInfo translate = typeof(SQLVisitor).GetMethod(
            "TryTranslateByteArrayContains", BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo contains = typeof(QueryTranslationGuardTests).GetMethod(
            nameof(UnrelatedContains), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(byte));
        SQLiteExpression source = SQLiteExpression.Leaf(typeof(byte[]), 1, "t0.\"Data\"");
        MethodCallExpression call = Expression.Call(contains, source, Expression.Constant((byte)1));

        Assert.Null(translate.Invoke(visitor, [call]));
    }

    [Fact]
    public void CollectionParameterWithoutAWholeValueHasClearError()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        QueryableVisitor queryable = new(db, visitor);
        Dictionary<string, Expression> columns = new()
        {
            ["Value"] = Expression.Constant(1)
        };
        visitor.TableColumns = columns;

        ParameterExpression parameter = Expression.Parameter(typeof(List<int>), "values");
        visitor.MethodArguments[parameter] = columns;
        LambdaExpression selector = Expression.Lambda(parameter, parameter);
        IQueryable<List<int>> source = new[] { new List<int>() }.AsQueryable();
        MethodCallExpression select = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Select),
            [typeof(List<int>), typeof(List<int>)],
            source.Expression,
            Expression.Quote(selector));
        MethodInfo visitSelect = typeof(QueryableVisitor).GetMethod(
            "VisitSelect", BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(
            () => visitSelect.Invoke(queryable, [select]));
        NotSupportedException exception = Assert.IsType<NotSupportedException>(wrapper.InnerException);

        Assert.Contains("Cannot read a query result into the collection type", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CteClientColumnRewriterCarriesColumnKinds()
    {
        SQLiteCounters counters = new();
        SQLiteExpression known = SQLiteExpression
            .Leaf(typeof(DayOfWeek), counters.NextIdentifier(), "t0.\"Value\"")
            .WithDayOfWeekInteger()
            .WithJsonSource();
        known.IdentifierText = "Value";
        CteClientColumnRewriter rewriter = new([known], null, "c0", counters);

        SQLiteExpression rewritten = Assert.IsAssignableFrom<SQLiteExpression>(rewriter.Rewrite(known));

        Assert.True(rewritten.IsDayOfWeekInteger);
        Assert.True(rewritten.IsJsonSource);
    }

    [Fact]
    public void DayOfWeekReconcileFindsAnEquivalentUnnamedColumn()
    {
        using TestDatabase db = new(b => b.EnumStorage = SQLite.Framework.Enums.EnumStorageMode.Text);
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        QueryableVisitor queryable = new(db, visitor);
        SQLiteExpression main = SQLiteExpression.Leaf(
            typeof(int), visitor.Counters.NextIdentifier(), "t0.\"Day\"");
        queryable.Selects.Add(main);

        SQLTranslator operand = new(db, visitor.Counters, 0, true);
        FieldInfo operandVisitorField = typeof(SQLTranslator).GetField(
            "queryableMethodVisitor", BindingFlags.Instance | BindingFlags.NonPublic)!;
        QueryableVisitor operandVisitor = (QueryableVisitor)operandVisitorField.GetValue(operand)!;
        operandVisitor.Selects.Add(SQLiteExpression
            .Leaf(typeof(int), visitor.Counters.NextIdentifier(), "t1.\"Day\"")
            .WithDayOfWeekInteger());

        visitor.TableColumns = new Dictionary<string, Expression>
        {
            ["Other"] = SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(), "t0.\"Other\""),
            ["Match"] = main
        };
        MethodInfo reconcile = typeof(QueryableVisitor).GetMethod(
            "ReconcileDayOfWeekSelects", BindingFlags.Instance | BindingFlags.NonPublic)!;

        reconcile.Invoke(queryable, [operand]);

        Assert.Same(queryable.Selects[0], visitor.TableColumns["Match"]);
        Assert.True(queryable.Selects[0].IsDayOfWeekInteger);
    }

    [Fact]
    public void JsonElementColumnsKeepAWholeValueWithoutTypeMetadata()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        QueryableVisitor queryable = new(db, visitor);
        MethodInfo build = typeof(QueryableVisitor).GetMethod(
            "BuildJsonElementColumns", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Dictionary<string, Expression> columns = Assert.IsType<Dictionary<string, Expression>>(
            build.Invoke(queryable, [typeof(QueryTranslationGuardTests), "j0"]));

        KeyValuePair<string, Expression> column = Assert.Single(columns);
        Assert.Equal(string.Empty, column.Key);
        Assert.True(Assert.IsAssignableFrom<SQLiteExpression>(column.Value).IsJsonSource);
    }

    private static byte ReturnByte(int marker, byte value)
    {
        return marker == 0 ? value : default;
    }

    private static byte[] ReturnBytes(int marker, byte[] value)
    {
        return marker == 0 ? value : [];
    }

    private static bool UnrelatedContains<T>(T[] values, T value)
    {
        return values.Contains(value);
    }
}
