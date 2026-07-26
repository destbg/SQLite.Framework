using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Internals.Visitors.SQL;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class GuardOnlyLeaf
{
    public int Value { get; set; }
}

public class GuardIfaceRow
{
    public GuardIfaceRow(int id, IComparable value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; init; }

    public IComparable Value { get; init; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("GuardCopyRows")]
public class GuardCopyRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public int Source { get; set; }

    public int Target { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int Ignored { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("GuardFrozenRows")]
public class GuardFrozenRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; }
}

[System.ComponentModel.DataAnnotations.Schema.Table("GuardCtorRows")]
public class GuardCtorRow
{
    public GuardCtorRow(int key)
    {
        Id = key;
    }

    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
}

public class UnreachableGuardCoverageTests
{
    [Fact]
    public void TheClientColumnRewriterLeavesAnExpressionItDoesNotRecognise()
    {
        SQLiteCounters counters = new();
        SQLiteExpression known = SQLiteExpression.Leaf(typeof(int), counters.NextIdentifier(), "t0.\"Known\"");
        known.IdentifierText = "Known";
        SQLiteExpression stranger = SQLiteExpression.Leaf(typeof(int), counters.NextIdentifier(), "t0.\"Stranger\"");

        CteClientColumnRewriter rewriter = new([known], null, "c0", counters);

        Assert.Same(stranger, rewriter.Rewrite(stranger));
        Assert.NotSame(known, rewriter.Rewrite(known));
    }

    [Fact]
    public void BodyColumnNamesFallBackToPositionalNamesForAnUnaliasedSelect()
    {
        SQLiteCounters counters = new();
        SQLiteExpression bare = SQLiteExpression.Leaf(typeof(int), counters.NextIdentifier(), "t0.\"Bare\"");
        Dictionary<string, Expression> bodyColumns = new() { ["Other"] = SQLiteExpression.Leaf(typeof(int), counters.NextIdentifier(), "t0.\"Other\"") };

        SQLiteExpression matched = SQLiteExpression.Leaf(typeof(int), counters.NextIdentifier(), "t0.\"Other\"");
        bodyColumns["Aliased"] = SQLiteExpression.Alias(typeof(int), counters.NextIdentifier(), matched, null);

        string[] names = CteColumnMapper.BodyColumnNamesWithPlaceholders(bodyColumns, [bare, matched]);

        Assert.Equal(["$c0", "Aliased"], names);
    }

    [Fact]
    public void AnUnsetConstructedMemberIsNotResolvedWhenNoAncestorWasConstructed()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        MethodInfo method = typeof(SQLVisitor).GetMethod(
            "TryResolveUnsetConstructedMember", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Dictionary<string, Expression> columns = [];
        visitor.ConstructedProjectionPaths[columns] = ["Built"];

        object?[] noAncestor = [columns, "Other.Missing", typeof(int), null];
        Assert.False((bool)method.Invoke(visitor, noAncestor)!);

        object?[] emptyPath = [columns, string.Empty, typeof(int), null];
        Assert.False((bool)method.Invoke(visitor, emptyPath)!);

        object?[] notSimple = [columns, "Built.Nested", typeof(GuardOnlyLeaf), null];
        Assert.False((bool)method.Invoke(visitor, notSimple)!);

        object?[] noConstructedEntry = [new Dictionary<string, Expression>(), "Built.Missing", typeof(int), null];
        Assert.False((bool)method.Invoke(visitor, noConstructedEntry)!);

        object?[] resolved = [columns, "Built.Missing", typeof(int), null];
        Assert.True((bool)method.Invoke(visitor, resolved)!);
    }

    [Fact]
    public void TheOptionalRowNullCheckNeedsAtLeastOneLeaf()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        MethodInfo method = typeof(SQLVisitor).GetMethod(
            "BuildOptionalRowNullCheck", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Dictionary<string, Expression> noLeaves = new() { ["Client"] = Expression.Constant(1) };
        Assert.Null(method.Invoke(visitor, [noLeaves, true]));
    }

    [Fact]
    public void MaterialisingARowWithNoWritableColumnNeedsNoNullTest()
    {
        using TestDatabase db = new();
        db.Table<GuardFrozenRow>().Schema.CreateTable();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression row = Expression.Parameter(typeof(GuardFrozenRow), "r");
        visitor.MethodArguments[row] = new Dictionary<string, Expression>
        {
            ["Id"] = SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(), "g0.\"Id\"")
        };

        Expression? materialized = visitor.TryMaterializeEntityLeaves(row);

        Assert.NotNull(materialized);
        Assert.IsNotType<ConditionalExpression>(materialized);
    }

    [Fact]
    public void AColumnCopyIsRefusedWhenTheSourceMemberIsNotAColumn()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        TableMapping mapping = db.TableMapping(typeof(GuardCopyRow));
        SQLitePropertyCalls<GuardCopyRow> calls = new(visitor, mapping);

        MethodInfo method = typeof(SQLitePropertyCalls<GuardCopyRow>).GetMethod(
            "TryReadStoredColumn", BindingFlags.Instance | BindingFlags.NonPublic)!;

        ParameterExpression row = Expression.Parameter(typeof(GuardCopyRow), "r");
        Expression unknown = Expression.Property(row, nameof(GuardCopyRow.Ignored));
        Assert.Null(method.Invoke(calls, [unknown, nameof(GuardCopyRow.Target)]));

        Expression known = Expression.Property(row, nameof(GuardCopyRow.Source));
        Assert.Null(method.Invoke(calls, [known, "NoSuchTarget"]));
        Assert.Null(method.Invoke(calls, [known, nameof(GuardCopyRow.Target)]));
    }

    [Fact]
    public void AnInlineLiteralContainsIgnoresAnArraySourceAndOtherMethods()
    {
        MethodInfo method = typeof(SQLite.Framework.Internals.JSON.JsonMethodTranslator).GetMethod(
            "IsInlineLiteralContains", BindingFlags.Static | BindingFlags.NonPublic)!;
        SQLiteOptions options = new SQLiteOptionsBuilder("guard-inline-literal.db3").Build();

        NewArrayExpression array = Expression.NewArrayInit(typeof(int), Expression.Constant(1));
        MethodInfo contains = typeof(List<int>).GetMethod(nameof(List<int>.Contains))!;
        MethodCallExpression call = Expression.Call(Expression.Constant(new List<int>()), contains, Expression.Constant(1));

        Assert.True((bool)method.Invoke(null, [call, array, options])!);
        Assert.False((bool)method.Invoke(null, [call, Expression.Constant(1), options])!);

        MethodInfo indexOf = typeof(List<int>).GetMethod(nameof(List<int>.IndexOf), [typeof(int)])!;
        MethodCallExpression other = Expression.Call(Expression.Constant(new List<int>()), indexOf, Expression.Constant(1));
        Assert.False((bool)method.Invoke(null, [other, array, options])!);
    }

    [Fact]
    public void APrefixedRowDoesNotCarryTheOptionalRowFlag()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        AliasVisitor alias = new(db, visitor);

        ParameterExpression row = Expression.Parameter(typeof(GuardCopyRow), "r");
        Dictionary<string, Expression> columns = new()
        {
            ["Id"] = SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(), "g0.\"Id\"")
        };
        visitor.MethodArguments[row] = columns;
        visitor.OptionalRowColumns.Add(columns);

        MethodInfo method = typeof(AliasVisitor).GetMethod(
            "VisitParameterExpression", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo flag = typeof(AliasVisitor).GetField(
            "carriesOptionalRow", BindingFlags.Instance | BindingFlags.NonPublic)!;

        method.Invoke(alias, [row, "N"]);
        Assert.False((bool)flag.GetValue(alias)!);

        method.Invoke(alias, [row, string.Empty]);
        Assert.True((bool)flag.GetValue(alias)!);
    }

    [Fact]
    public void ADayOfWeekReconcileSkipsAColumnMapEntryItCannotRewrite()
    {
        using TestDatabase db = new(b => b.EnumStorage = SQLite.Framework.Enums.EnumStorageMode.Text);
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        Internals.Visitors.Queryable.QueryableVisitor queryable = new(db, visitor);

        SQLiteExpression main = SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(), "t0.\"Dow\"");
        main.IdentifierText = "Dow";
        queryable.Selects.Add(main);

        SQLiteExpression unmapped = SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(), "t0.\"Gone\"");
        unmapped.IdentifierText = "Gone";
        queryable.Selects.Add(unmapped);

        SQLiteExpression operandSelect = SQLiteExpression
            .Leaf(typeof(int), visitor.Counters.NextIdentifier(), "t1.\"Dow\"")
            .WithDayOfWeekInteger();
        SQLite.Framework.Internals.SQLTranslator operand = new(db, visitor.Counters, 0, true);
        FieldInfo operandVisitorField = typeof(SQLite.Framework.Internals.SQLTranslator).GetField(
            "queryableMethodVisitor", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Internals.Visitors.Queryable.QueryableVisitor operandVisitor =
            (Internals.Visitors.Queryable.QueryableVisitor)operandVisitorField.GetValue(operand)!;
        operandVisitor.Selects.Add(operandSelect);
        operandVisitor.Selects.Add(SQLiteExpression
            .Leaf(typeof(int), visitor.Counters.NextIdentifier(), "t1.\"Gone\"")
            .WithDayOfWeekInteger());

        visitor.TableColumns = new Dictionary<string, Expression> { ["Dow"] = Expression.Constant(1) };

        MethodInfo method = typeof(Internals.Visitors.Queryable.QueryableVisitor).GetMethod(
            "ReconcileDayOfWeekSelects", BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(queryable, [operand]);

        Assert.NotSame(main, queryable.Selects[0]);
        Assert.IsType<ConstantExpression>(visitor.TableColumns["Dow"]);
    }

#if !SQLITE_FRAMEWORK_SOURCE_GENERATOR
    [Fact]
    public void AnInterfaceParameterMissingFromTheProjectedTypesReadsAsObject()
    {
        using TestDatabase db = new();
        SQLiteCommand command = db.CreateCommand("SELECT 1 AS \"Id\", 'Ann' AS \"Value\"", []);
        using SQLiteDataReader reader = command.ExecuteReader();
        Assert.True(reader.Read());

        Dictionary<string, int> columns = new(StringComparer.OrdinalIgnoreCase) { ["Id"] = 0, ["Value"] = 1 };
        SQLQuery query = new()
        {
            Sql = command.CommandText,
            Parameters = [],
            CreateObject = null,
            Reverse = false,
            ThrowOnEmpty = false,
            ElementAtSemantic = false,
            ThrowOnMoreThanOne = false,
            SelectValueTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
        };

        Func<SQLiteQueryContext, object?> materializer =
            BuildQueryObject.BuildMaterializer(reader, columns, query, typeof(GuardIfaceRow));
        SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query);

        GuardIfaceRow row = (GuardIfaceRow)materializer(context)!;

        Assert.Equal(1, row.Id);
        Assert.Equal("Ann", row.Value.ToString());
    }
#endif

    [Fact]
    public void AnEntityIsNotBuiltWhenAConstructorParameterHasNoColumn()
    {
        using TestDatabase db = new();
        MethodInfo method = typeof(SQLVisitor).GetMethod(
            "BuildEntityFromLeaves", BindingFlags.Static | BindingFlags.NonPublic)!;

        TableMapping mapping = db.TableMapping(typeof(GuardCtorRow));
        object?[] arguments = [typeof(GuardCtorRow), mapping, new Dictionary<string, SQLiteExpression>(), null];

        Assert.Null(method.Invoke(null, arguments));
    }
}
