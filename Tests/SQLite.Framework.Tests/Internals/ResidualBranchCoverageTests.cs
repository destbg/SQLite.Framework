using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Internals.Visitors.Member;
using SQLite.Framework.Internals.Visitors.SQL;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ResidualCtorOnlyDto
{
    public ResidualCtorOnlyDto(int num, string text)
    {
        Num = num;
        Text = text;
    }

    public int Num { get; }

    public string Text { get; }

    public int? Maybe { get; }
}

public class ResidualFieldHolder
{
    public int Slot;
}

public struct ResidualValueDto
{
    public int Amount { get; set; }
}

public class ResidualPathLeaf
{
    public int Val { get; set; }
}

public class ResidualPathPair
{
    public ResidualPathLeaf First { get; set; } = new();

    public int Num { get; set; }
}

public class ResidualPathBox
{
    public ResidualPathLeaf B { get; set; } = new();

    public int N { get; set; }
}

public sealed class ResidualNullToString
{
    public override string? ToString()
    {
        return null;
    }
}

public sealed class ResidualOrphanMethod : MethodInfo
{
    public override MethodAttributes Attributes => MethodAttributes.Static;

    public override Type? DeclaringType => null;

    public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

    public override string Name => "Orphan";

    public override Type? ReflectedType => null;

    public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotSupportedException();

    public override MethodInfo GetBaseDefinition()
    {
        return this;
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
        return [];
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return [];
    }

    public override MethodImplAttributes GetMethodImplementationFlags()
    {
        return MethodImplAttributes.IL;
    }

    public override ParameterInfo[] GetParameters()
    {
        return [];
    }

    public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture)
    {
        throw new NotSupportedException();
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        return false;
    }
}

public class ResidualBranchCoverageTests
{
    [Fact]
    public void EmittedColumnNamesReturnsNullForEmptySelectList()
    {
        Assert.Null(CteColumnMapper.EmittedColumnNames(null, []));
    }

    [Fact]
    public void EmittedColumnNamesReturnsNullForUnnamedSelect()
    {
        SQLiteExpression leaf = SQLiteExpression.Leaf(typeof(int), 1, "t.\"A\"");
        leaf.IdentifierText = "";

        Assert.Null(CteColumnMapper.EmittedColumnNames(null, [leaf]));
    }

    [Fact]
    public void EmittedColumnNamesPrefersDeclaredNames()
    {
        SQLiteExpression leaf = SQLiteExpression.Leaf(typeof(int), 1, "t.\"A\"");

        Assert.Equal(["X"], CteColumnMapper.EmittedColumnNames(["X"], [leaf])!);
    }

    [Fact]
    public void DeclaredColumnNamesFallsBackToPlaceholdersOnLengthMismatch()
    {
        using TestDatabase db = new(null, nameof(DeclaredColumnNamesFallsBackToPlaceholdersOnLengthMismatch));
        SQLiteExpression first = SQLiteExpression.Leaf(typeof(int), 1, "t.\"A\"");
        SQLiteExpression second = SQLiteExpression.Leaf(typeof(int), 2, "t.\"B\"");
        Dictionary<string, Expression> bodyColumns = new() { ["A"] = first, ["B"] = second };

        string[]? names = CteColumnMapper.DeclaredColumnNames(typeof(int), bodyColumns, [first, second], db.Options);

        Assert.NotNull(names);
        Assert.Equal(2, names!.Length);
    }

    [Fact]
    public void BodyColumnNamesWithPlaceholdersMatchesByIdentifierText()
    {
        SQLiteExpression bodyLeaf = SQLiteExpression.Leaf(typeof(int), 1, "t.\"A\"");
        SQLiteExpression select = SQLiteExpression.Leaf(typeof(int), 2, "t.\"A\"");
        select.IdentifierText = "X";
        Dictionary<string, Expression> bodyColumns = new() { ["X"] = bodyLeaf };

        Assert.Equal(["X"], CteColumnMapper.BodyColumnNamesWithPlaceholders(bodyColumns, [select]));
    }

    [Fact]
    public void BodyColumnNamesWithPlaceholdersFallsBackForUnknownIdentifier()
    {
        SQLiteExpression bodyLeaf = SQLiteExpression.Leaf(typeof(int), 1, "t.\"A\"");
        SQLiteExpression select = SQLiteExpression.Leaf(typeof(int), 2, "t.\"B\"");
        select.IdentifierText = "Missing";
        Dictionary<string, Expression> bodyColumns = new() { ["X"] = bodyLeaf };

        string[] names = CteColumnMapper.BodyColumnNamesWithPlaceholders(bodyColumns, [select]);

        Assert.Single(names);
        Assert.NotEqual("Missing", names[0]);
    }

    [Fact]
    public void BodyColumnNamesWithPlaceholdersSkipsClientBodyMember()
    {
        SQLiteExpression select = SQLiteExpression.Leaf(typeof(int), 2, "t.\"A\"");
        select.IdentifierText = "X";
        Dictionary<string, Expression> bodyColumns = new() { ["X"] = Expression.Constant(1) };

        string[] names = CteColumnMapper.BodyColumnNamesWithPlaceholders(bodyColumns, [select]);

        Assert.Single(names);
        Assert.NotEqual("X", names[0]);
    }

    [Fact]
    public void BodyColumnNamesWithPlaceholdersUsesEachIdentifierOnce()
    {
        SQLiteExpression bodyLeaf = SQLiteExpression.Leaf(typeof(int), 1, "t.\"A\"");
        SQLiteExpression first = SQLiteExpression.Leaf(typeof(int), 2, "t.\"A\"");
        first.IdentifierText = "X";
        SQLiteExpression second = SQLiteExpression.Leaf(typeof(int), 3, "t.\"A\"");
        second.IdentifierText = "X";
        Dictionary<string, Expression> bodyColumns = new() { ["X"] = bodyLeaf };

        string[] names = CteColumnMapper.BodyColumnNamesWithPlaceholders(bodyColumns, [first, second]);

        Assert.Equal("X", names[0]);
        Assert.NotEqual("X", names[1]);
    }

    [Fact]
    public void CanonicalSqlEncodesEveryParameterValueKind()
    {
        SQLiteParameter[] parameters =
        [
            new() { Name = "@a0", Value = null },
            new() { Name = "@a1", Value = new DateTime(2026, 7, 26, 1, 2, 3, DateTimeKind.Utc) },
            new() { Name = "@a2", Value = new DateTimeOffset(2026, 7, 26, 1, 2, 3, TimeSpan.FromHours(2)) },
            new() { Name = "@a3", Value = TimeSpan.FromMinutes(90) },
            new() { Name = "@a4", Value = new TimeOnly(10, 30) },
            new() { Name = "@a5", Value = new DateOnly(2026, 7, 26) },
            new() { Name = "@a6", Value = 1.5d },
            new() { Name = "@a7", Value = 2.5f },
            new() { Name = "@a8", Value = new byte[] { 1, 2, 3 } },
            new() { Name = "@a9", Value = 42 }
        ];
        SQLiteExpression node = SQLiteExpression.Leaf(
            typeof(int), 1, "@a0 @a1 @a2 @a3 @a4 @a5 @a6 @a7 @a8 @a9", parameters);

        string canonical = CteSqlCanonicalizer.Canonicalize(node);

        Assert.Contains("010203", canonical);
        Assert.Contains("null", canonical);
        Assert.Contains("42", canonical);
    }

    [Fact]
    public void ResolveDirectoryReturnsMissingDirectoryUnchanged()
    {
        MethodInfo method = typeof(DatabaseFilePath).GetMethod(
            "ResolveDirectory", BindingFlags.NonPublic | BindingFlags.Static)!;
        string missing = Path.Combine(Path.GetTempPath(), "sqlitefw-missing-" + Guid.NewGuid().ToString("N"));

        Assert.Equal(missing, method.Invoke(null, [missing]));
    }

    [Fact]
    public void MethodOverrideCacheReusesTheCachedAnswer()
    {
        bool first = MethodOverrideCache.IsOverridden(
            typeof(ResidualCtorOnlyDto), typeof(object), nameof(ToString));
        bool second = MethodOverrideCache.IsOverridden(
            typeof(ResidualCtorOnlyDto), typeof(object), nameof(ToString));

        Assert.Equal(first, second);
    }

    [Fact]
    public void TableOptionClauseReturnsEmptyForMissingSql()
    {
        MethodInfo method = typeof(ModelValidator).GetMethod(
            "TableOptionClause", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.Equal(string.Empty, method.Invoke(null, [null]));
        Assert.Equal(string.Empty, method.Invoke(null, ["no parenthesis"]));
    }

    [Fact]
    public void UnsetConstructedMemberValueUsesClrDefaultsWithoutParameterlessConstructor()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod(
            "UnsetConstructedMemberValue", BindingFlags.NonPublic | BindingFlags.Static)!;
        ParameterExpression parameter = Expression.Parameter(typeof(ResidualCtorOnlyDto), "d");

        object? numValue = method.Invoke(null, [Expression.Property(parameter, nameof(ResidualCtorOnlyDto.Num))]);
        object? textValue = method.Invoke(null, [Expression.Property(parameter, nameof(ResidualCtorOnlyDto.Text))]);
        object? maybeValue = method.Invoke(null, [Expression.Property(parameter, nameof(ResidualCtorOnlyDto.Maybe))]);

        Assert.Equal(0, numValue);
        Assert.Null(textValue);
        Assert.Null(maybeValue);
    }

    [Fact]
    public void UnsetConstructedMemberValueHandlesFieldMembers()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod(
            "UnsetConstructedMemberValue", BindingFlags.NonPublic | BindingFlags.Static)!;
        ParameterExpression parameter = Expression.Parameter(typeof(ResidualFieldHolder), "h");

        object? slotValue = method.Invoke(null, [Expression.Field(parameter, nameof(ResidualFieldHolder.Slot))]);

        Assert.Equal(0, slotValue);
    }

    [Fact]
    public void MakeConstructedMemberConstantSupportsStructNewExpressions()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod(
            "MakeConstructedMemberConstant", BindingFlags.NonPublic | BindingFlags.Static)!;

        ConstantExpression constant = (ConstantExpression)method.Invoke(
            null, [Expression.New(typeof(ResidualValueDto)), nameof(ResidualValueDto.Amount), typeof(int)])!;

        Assert.Equal(0, constant.Value);
    }

    [Fact]
    public void GetConstantValueEvaluatesTypeAsForBothOutcomes()
    {
        Expression matching = Expression.TypeAs(Expression.Constant("x", typeof(object)), typeof(string));
        Expression mismatched = Expression.TypeAs(Expression.Constant(5, typeof(object)), typeof(string));

        Assert.Equal("x", ExpressionHelpers.GetConstantValue(matching));
        Assert.Null(ExpressionHelpers.GetConstantValue(mismatched));
    }

    [Fact]
    public void GetConstantValueTruncatesFloatConversionsToIntegers()
    {
        Expression conversion = Expression.Convert(Expression.Constant(2.9f), typeof(int));

        Assert.Equal(2, ExpressionHelpers.GetConstantValue(conversion));
    }

    [Fact]
    public void MethodOverrideCacheDetectsOverriddenMethods()
    {
        Assert.True(MethodOverrideCache.IsOverridden(
            typeof(ResidualNullToString), typeof(object), nameof(ToString)));
    }

    [Fact]
    public void MethodOverrideCacheTreatsMissingMetadataAsOverridden()
    {
        Assert.True(MethodOverrideCache.IsOverridden(
            typeof(ResidualCtorOnlyDto), typeof(object), "NoSuchResidualMethod"));
    }

    [Fact]
    public void IsSystemMethodReturnsFalseForGlobalNamespaceMethods()
    {
        Assert.False(QueryableMemberVisitor.IsSystemMethod(
            typeof(GlobalResidualHelper).GetMethod(nameof(GlobalResidualHelper.Fragment))!));
    }

    [Fact]
    public void InlineTextStorageEnumConversionCarriesOperandParameters()
    {
        using TestDatabase db = new(null, nameof(InlineTextStorageEnumConversionCarriesOperandParameters));
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        SQLiteParameter[] parameters = [new() { Name = "@e0", Value = "One" }];
        SQLiteExpression operand = SQLiteExpression.Leaf(typeof(string), 1, "@e0", parameters);

        SQLiteExpression result = EnumMemberVisitor.BuildTextStorageEnumToNumber(
            visitor, typeof(int), typeof(H22vTextFlags), operand, inlineOperand: true);

        Assert.Contains(result.Parameters!, p => p.Name == "@e0");
    }

    [Fact]
    public void InlineTextStorageEnumConversionAcceptsEmptyOperandParameters()
    {
        using TestDatabase db = new(null, nameof(InlineTextStorageEnumConversionAcceptsEmptyOperandParameters));
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        SQLiteExpression operand = SQLiteExpression.Leaf(typeof(string), 1, "t.\"Flags\"", Array.Empty<SQLiteParameter>());

        SQLiteExpression result = EnumMemberVisitor.BuildTextStorageEnumToNumber(
            visitor, typeof(int), typeof(H22vTextFlags), operand, inlineOperand: true);

        Assert.NotNull(result.Parameters);
    }

    [Fact]
    public void IsSystemMethodReturnsFalseForMethodsWithoutDeclaringType()
    {
        Assert.False(QueryableMemberVisitor.IsSystemMethod(new ResidualOrphanMethod()));
    }

    [Fact]
    public void ResolveMemberFoldsNestedConstructedPrefixes()
    {
        using TestDatabase db = new(null, nameof(ResolveMemberFoldsNestedConstructedPrefixes));
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        ParameterExpression pe = Expression.Parameter(typeof(ResidualPathPair), "p");
        MemberInitExpression constructed = Expression.MemberInit(
            Expression.New(typeof(ResidualPathLeaf)),
            Expression.Bind(typeof(ResidualPathLeaf).GetProperty(nameof(ResidualPathLeaf.Val))!, Expression.Constant(7)));
        visitor.MethodArguments[pe] = new Dictionary<string, Expression> { ["First"] = constructed };

        Expression result = visitor.ResolveMember(
            Expression.Property(Expression.Property(pe, nameof(ResidualPathPair.First)), nameof(ResidualPathLeaf.Val)));

        Assert.IsAssignableFrom<SQLiteExpression>(result);
    }

    [Fact]
    public void ResolveMemberFoldsConstructedRootsOfEveryShape()
    {
        using TestDatabase db = new(null, nameof(ResolveMemberFoldsConstructedRootsOfEveryShape));
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression initParam = Expression.Parameter(typeof(ResidualPathLeaf), "l");
        MemberInitExpression initRoot = Expression.MemberInit(
            Expression.New(typeof(ResidualPathLeaf)),
            Expression.Bind(typeof(ResidualPathLeaf).GetProperty(nameof(ResidualPathLeaf.Val))!, Expression.Constant(9)));
        visitor.MethodArguments[initParam] = new Dictionary<string, Expression> { [string.Empty] = initRoot };
        Expression initResult = visitor.ResolveMember(Expression.Property(initParam, nameof(ResidualPathLeaf.Val)));
        Assert.IsAssignableFrom<SQLiteExpression>(initResult);

        ParameterExpression newParam = Expression.Parameter(typeof(ResidualPathLeaf), "v");
        visitor.MethodArguments[newParam] = new Dictionary<string, Expression> { [string.Empty] = Expression.New(typeof(ResidualPathLeaf)) };
        Expression newResult = visitor.ResolveMember(Expression.Property(newParam, nameof(ResidualPathLeaf.Val)));
        Assert.IsAssignableFrom<SQLiteExpression>(newResult);
    }

    [Fact]
    public void VisitUnaryKeepsNonEvaluableConstantOperand()
    {
        using TestDatabase db = new(null, nameof(VisitUnaryKeepsNonEvaluableConstantOperand));
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);

        Expression result = visitor.Visit(Expression.UnaryPlus(Expression.Constant(5)));

        Assert.IsAssignableFrom<SQLiteExpression>(result);
    }

    [Fact]
    public void FindMemberPathResolvesEveryRenameShape()
    {
        MethodInfo method = typeof(SQLiteExpression).Assembly
            .GetType("SQLite.Framework.Internals.Visitors.Queryable.QueryableVisitor")!
            .GetMethod("FindMemberPath", BindingFlags.NonPublic | BindingFlags.Static)!;
        ParameterExpression other = Expression.Parameter(typeof(ResidualPathPair), "other");

        (Expression body1, ParameterExpression p1) = Shape(p => new { A = p.First.Val });
        Assert.Equal("A", method.Invoke(null, [body1, p1, "First.Val", ""]));

        (Expression body2, ParameterExpression p2) = Shape(p => new ResidualPathBox { B = p.First });
        Assert.Equal("B.Val", method.Invoke(null, [body2, p2, "First.Val", ""]));

        (Expression body3, ParameterExpression p3) = Shape(p => new ResidualPathBox { N = p.Num });
        Assert.Null(method.Invoke(null, [body3, p3, "First.Val", ""]));

        (Expression body4, ParameterExpression p4) = Shape(p => p.First);
        Assert.Null(method.Invoke(null, [body4, p4, "First.Val", ""]));

        (Expression body5, ParameterExpression p5) = Shape(p => new { P = p });
        Assert.Equal("P.First.Val", method.Invoke(null, [body5, p5, "First.Val", ""]));

        (Expression body6, ParameterExpression p6) = Shape(p => new { A = p.Num });
        Assert.Null(method.Invoke(null, [body6, p6, "First.Val", ""]));

        (Expression body7, _) = Shape(p => new { A = p.First.Val });
        Assert.Null(method.Invoke(null, [body7, other, "First.Val", ""]));
    }

    [Fact]
    public void CanonicalSqlFallsBackForNullRenderingValues()
    {
        SQLiteParameter[] parameters = [new() { Name = "@b0", Value = new ResidualNullToString() }];
        SQLiteExpression node = SQLiteExpression.Leaf(typeof(int), 1, "@b0", parameters);

        string canonical = CteSqlCanonicalizer.Canonicalize(node);

        Assert.Contains("null", canonical);
    }

    [Fact]
    public void UnsetConstructedMemberValueReadsDefaultsThroughParameterlessConstructors()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod(
            "UnsetConstructedMemberValue", BindingFlags.NonPublic | BindingFlags.Static)!;
        ParameterExpression parameter = Expression.Parameter(typeof(ResidualPathLeaf), "l");

        object? value = method.Invoke(null, [Expression.Property(parameter, nameof(ResidualPathLeaf.Val))]);

        Assert.Equal(0, value);
    }

    private static (Expression Body, ParameterExpression Parameter) Shape<T>(Expression<Func<ResidualPathPair, T>> lambda)
    {
        return (lambda.Body, lambda.Parameters[0]);
    }

    [Fact]
    public void RequireKeyExpressionThrowsForRowShapedKeys()
    {
        MethodInfo method = typeof(SQLiteExpression).Assembly
            .GetType("SQLite.Framework.Internals.Visitors.Member.WindowFunctionsMemberVisitor")!
            .GetMethod("RequireKeyExpression", BindingFlags.NonPublic | BindingFlags.Static)!;
        ResolvedModel arg = new()
        {
            IsConstant = false,
            Constant = null,
            SQLiteExpression = null,
            Expression = Expression.Constant(1)
        };

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [arg]));
        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void CompletePendingSavepointCleanupSkipsWhenNotInTransaction()
    {
        using TestDatabase db = new(null, nameof(CompletePendingSavepointCleanupSkipsWhenNotInTransaction));
        db.Table<ResidualProbeRow>().Schema.CreateTable();
        FieldInfo field = typeof(SQLiteDatabase).GetField(
            "pendingForcedSavepoint", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(db, "sp_residual");

        db.CompletePendingSavepointCleanup();

        Assert.Null(field.GetValue(db));
    }

    [Fact]
    public void CompletePendingSavepointCleanupSkipsWhenHandleIsGone()
    {
        TestDatabase db = new(null, nameof(CompletePendingSavepointCleanupSkipsWhenHandleIsGone));
        db.Table<ResidualProbeRow>().Schema.CreateTable();
        db.Dispose();
        FieldInfo field = typeof(SQLiteDatabase).GetField(
            "pendingForcedSavepoint", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(db, "sp_residual");

        db.CompletePendingSavepointCleanup();

        Assert.Null(field.GetValue(db));
    }
}

[System.ComponentModel.DataAnnotations.Schema.Table("ResidualProbeRows")]
public class ResidualProbeRow
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
}
