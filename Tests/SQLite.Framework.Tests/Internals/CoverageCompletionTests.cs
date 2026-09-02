using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Internals.Visitors.Member;
using SQLite.Framework.Internals.Visitors.SQL;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CoverageCompletionTests
{
    [Fact]
    public void RegisteredJsonCollectionReaderHandlesARawQueryColumn()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.JsonCollectionMaterializers[typeof(List<int>)] = (_, _) => new List<int> { 42 };
        using SQLiteDatabase db = new(builder.Build());
        SQLiteCommand command = db.CreateCommand("SELECT '[1,2,3]'", []);
        using SQLiteDataReader reader = command.ExecuteReader();
        reader.Read();

        List<int> values = (List<int>)CommandHelpers.ReadColumnValue(
            reader.Statement!,
            0,
            SQLiteColumnType.Text,
            typeof(List<int>),
            db.Options)!;

        Assert.Equal([42], values);
    }

    [Fact]
    public void DeclaredCteDayOfWeekLeafKeepsItsMarker()
    {
        SQLiteExpression source = SQLiteExpression.Leaf(typeof(DayOfWeek), 0, "day").WithDayOfWeekInteger();

        SQLiteExpression leaf = CteColumnMapper.BuildDeclaredBodyLeaf(source, "Day", "c0", new SQLiteCounters());

        Assert.True(leaf.IsDayOfWeekInteger);
    }

    [Fact]
    public void LocalCollectionConstantClientPredicateDeclinesSqlTranslation()
    {
        using TestDatabase db = new(nameof(LocalCollectionConstantClientPredicateDeclinesSqlTranslation));
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0) { ClientEvalAllowed = true };
        ParameterExpression element = Expression.Parameter(typeof(int), "element");
        Expression predicate = Expression.Invoke(Expression.Constant((Func<bool>)(() => true)));
        MethodInfo method = typeof(QueryableMemberVisitor).GetMethod(
            "BuildLocalCollectionExpression",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        object? result = method.Invoke(null, [visitor, typeof(bool), element, predicate, new List<object?> { 1 }, true]);

        Assert.Null(result);
    }

    [Fact]
    public void JsonSourceDetectionHandlesMarkedAndPlainSqlLeaves()
    {
        using TestDatabase db = new(nameof(JsonSourceDetectionHandlesMarkedAndPlainSqlLeaves));
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        ParameterExpression row = Expression.Parameter(typeof(JsonMarkerRow), "row");
        ParameterExpression element = Expression.Parameter(typeof(int), "element");
        visitor.MethodArguments[row] = new Dictionary<string, Expression>
        {
            [nameof(JsonMarkerRow.Id)] = SQLiteExpression.Leaf(typeof(int), 0, "r.Id"),
            [nameof(JsonMarkerRow.Json)] = SQLiteExpression.Leaf(typeof(string), 1, "r.Json").WithJsonSource()
        };
        MethodInfo method = typeof(QueryableMemberVisitor).GetMethod(
            "IsJsonSourceExpression",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        bool plain = (bool)method.Invoke(null,
            [visitor, Expression.Property(row, nameof(JsonMarkerRow.Id)), element])!;
        bool json = (bool)method.Invoke(null,
            [visitor, Expression.Property(row, nameof(JsonMarkerRow.Json)), element])!;
        visitor.ClientEvalAllowed = true;
        Expression clientExpression = Expression.Invoke(Expression.Constant((Func<int>)(() => 1)));
        bool client = (bool)method.Invoke(null, [visitor, clientExpression, element])!;

        Assert.False(plain);
        Assert.True(json);
        Assert.False(client);
    }

    [Fact]
    public void QueryCompilerSpanContainsCoversNullListAndEnumerableSources()
    {
        Assert.False(CompileSpanContains(Expression.Constant(null, typeof(SpanEnumerable)), 1));
        Assert.True(CompileSpanContains(Expression.Constant(new[] { 1, 2 }), 2));
        Assert.True(CompileSpanContains(Expression.Constant(new SpanEnumerable(1, 2)), 2));
        Assert.False(CompileSpanContains(Expression.Constant(new SpanEnumerable(1, 2)), 3));
    }

    [Fact]
    public void SpanSourceDetectionHandlesUnaryConversionAndUnrelatedExpression()
    {
        MethodInfo method = typeof(QueryCompilerVisitor).GetMethod(
            "TryGetSpanSource",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        UnaryExpression conversion = Expression.Convert(
            Expression.Constant(new SpanEnumerable(1)),
            typeof(ReadOnlySpan<int>));
        object?[] convertedArguments = [conversion, null];
        object?[] plainArguments = [Expression.Constant(1), null];

        Assert.True((bool)method.Invoke(null, convertedArguments)!);
        Assert.IsType<ConstantExpression>(convertedArguments[1]);
        Assert.False((bool)method.Invoke(null, plainArguments)!);
        Assert.Null(plainArguments[1]);
    }

    [Fact]
    public void ByteArrayContainsDetectionCoversGenericDefinitionsAndSourceShapes()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod(
            "IsByteArrayContainsMethod",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo enumerable = FindContainsDefinition(typeof(Enumerable), typeof(IEnumerable<>));
        MethodInfo readOnlySpan = FindContainsDefinition(typeof(MemoryExtensions), typeof(ReadOnlySpan<>));
        MethodInfo span = FindContainsDefinition(typeof(MemoryExtensions), typeof(Span<>));
        MethodInfo arraySource = typeof(CoverageCompletionTests).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == nameof(Contains)
                && candidate.GetParameters()[0].ParameterType == typeof(byte[]));
        MethodInfo listSource = typeof(CoverageCompletionTests).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == nameof(Contains)
                && candidate.GetParameters()[0].ParameterType.IsGenericType);

        Assert.True((bool)method.Invoke(null, [enumerable])!);
        Assert.True((bool)method.Invoke(null, [readOnlySpan])!);
        Assert.True((bool)method.Invoke(null, [span])!);
        Assert.False((bool)method.Invoke(null, [arraySource])!);
        Assert.False((bool)method.Invoke(null, [listSource])!);
    }

    [Fact]
    public void CustomOrderComparerIsRejectedByTheQueryableOrderVisitor()
    {
        using TestDatabase db = new(nameof(CustomOrderComparerIsRejectedByTheQueryableOrderVisitor));
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);
        ParameterExpression row = Expression.Parameter(typeof(H25pTick), "row");
        sqlVisitor.MethodArguments[row] = new Dictionary<string, Expression>
        {
            [nameof(H25pTick.Id)] = SQLiteExpression.Leaf(typeof(int), 0, "h0.Id")
        };
        SQLite.Framework.Internals.Visitors.Queryable.QueryableVisitor queryableVisitor = new(db, sqlVisitor);
        MethodInfo fakeOrder = typeof(CoverageCompletionTests).GetMethod(
            nameof(OrderByMarker),
            BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(H25pTick), typeof(int));
        MethodCallExpression call = Expression.Call(
            fakeOrder,
            Expression.Constant(Array.Empty<H25pTick>().AsQueryable()),
            Expression.Quote(Expression.Lambda(Expression.Property(row, nameof(H25pTick.Id)), row)),
            Expression.Constant(1));
        MethodInfo visitOrder = queryableVisitor.GetType().GetMethod(
            "VisitOrder",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => visitOrder.Invoke(queryableVisitor, [call]));

        Assert.Contains("custom IComparer", exception.InnerException!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorEvaluationDetectionCoversEveryConstructedNodeShape()
    {
        using TestDatabase db = new(nameof(ConstructorEvaluationDetectionCoversEveryConstructedNodeShape));
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        Dictionary<string, Expression> columns = [];
        ConstructorInfo constructor = typeof(H26qConstructorMemberOuter).GetConstructors().Single();
        NewExpression node = Expression.New(constructor, Expression.New(typeof(H26qConstructorMemberChild)));
        visitor.ConstructedProjectionNodes[columns] = new Dictionary<string, Expression> { [string.Empty] = node };
        MethodInfo method = typeof(SQLVisitor).GetMethod(
            "RequiresConstructorEvaluation",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.True((bool)method.Invoke(visitor, [columns, nameof(H26qConstructorMemberOuter.Child)])!);

        visitor.ConstructedProjectionNodes[columns] = new Dictionary<string, Expression>
        {
            [string.Empty] = Expression.Constant(new object())
        };
        Assert.False((bool)method.Invoke(visitor, [columns, nameof(H26qConstructorMemberOuter.Child)])!);
        visitor.ConstructedProjectionNodes[columns] = new Dictionary<string, Expression>
        {
            [string.Empty] = Expression.New(typeof(ValueTuple<int>).GetConstructor([typeof(int)])!, Expression.Constant(1))
        };
        Assert.False((bool)method.Invoke(visitor, [columns, nameof(ValueTuple<int>.Item1)])!);
        Assert.False((bool)method.Invoke(visitor, [columns, "Missing.Value"])!);
    }

    private static bool CompileSpanContains(Expression source, int item)
    {
        MethodInfo contains = FindContainsDefinition(typeof(MemoryExtensions), typeof(ReadOnlySpan<>))
            .MakeGenericMethod(typeof(int));
        UnaryExpression conversion = Expression.Convert(source, typeof(ReadOnlySpan<int>));
        MethodCallExpression call = Expression.Call(contains, conversion, Expression.Constant(item));
        QueryCompilerVisitor visitor = new(new SQLiteOptionsBuilder(":memory:").Build());
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(call);
        return (bool)compiled.Call(new SQLiteQueryContext())!;
    }

    private static MethodInfo FindContainsDefinition(Type declaringType, Type sourceType)
    {
        return declaringType.GetMethods()
            .Where(method => method.Name == nameof(Enumerable.Contains)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 2)
            .Single(method =>
            {
                Type first = method.GetParameters()[0].ParameterType;
                return first.IsGenericType && first.GetGenericTypeDefinition() == sourceType;
            });
    }

    private static bool Contains<T>(byte[] source, T value) => false;

    private static bool Contains<T>(List<T> source, T value) => source.Contains(value);

    private static IOrderedQueryable<T> OrderByMarker<T, TKey>(
        IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        int marker) => throw new NotSupportedException();

    private sealed class JsonMarkerRow
    {
        public int Id { get; set; }

        public string Json { get; set; } = "";
    }

    private sealed class SpanEnumerable : IEnumerable<int>
    {
        private readonly int[] values;

        public SpanEnumerable(params int[] values)
        {
            this.values = values;
        }

        public static implicit operator ReadOnlySpan<int>(SpanEnumerable? source) => source?.values;

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
