using System.Linq.Expressions;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Tests.Helpers;
using CommonHelpers = SQLite.Framework.Internals.Helpers.CommonHelpers;

namespace SQLite.Framework.Tests;

public class CommonHelpersCoverageTests
{
    [Fact]
    public void ResolveParameterPath_DirectParameter_ReturnsEmptyPath()
    {
        ParameterExpression p = Expression.Parameter(typeof(int), "x");
        (string path, ParameterExpression parameter) = CommonHelpers.ResolveParameterPath(p);

        Assert.Equal(string.Empty, path);
        Assert.Same(p, parameter);
    }

    [Fact]
    public void ResolveParameterPath_NonMemberNonParameter_Throws()
    {
        ConstantExpression c = Expression.Constant(7);
        Assert.Throws<NotSupportedException>(() => CommonHelpers.ResolveParameterPath(c));
    }

    [Fact]
    public void ResolveParameterPath_MemberChainNotEndingInParameter_Throws()
    {
        ConstantExpression c = Expression.Constant("hello");
        MemberExpression length = Expression.Property(c, nameof(string.Length));

        Assert.Throws<NotSupportedException>(() => CommonHelpers.ResolveParameterPath(length));
    }

    [Fact]
    public void ResolveNullableParameterPath_DirectParameter_ReturnsEmptyPath()
    {
        ParameterExpression p = Expression.Parameter(typeof(int), "x");
        (string path, ParameterExpression? parameter) = CommonHelpers.ResolveNullableParameterPath(p);

        Assert.Equal(string.Empty, path);
        Assert.Same(p, parameter);
    }

    [Fact]
    public void ResolveNullableParameterPath_MemberChainNotEndingInParameter_ReturnsEmpty()
    {
        ConstantExpression c = Expression.Constant("hello");
        MemberExpression length = Expression.Property(c, nameof(string.Length));

        (string path, ParameterExpression? parameter) = CommonHelpers.ResolveNullableParameterPath(length);

        Assert.Equal(string.Empty, path);
        Assert.Null(parameter);
    }

    [Fact]
    public void IsConstant_StaticMemberExpression_IsConstant()
    {
        MemberExpression me = Expression.Property(null, typeof(DateTime).GetProperty(nameof(DateTime.Now))!);
        Assert.True(CommonHelpers.IsConstant(me));
    }

    [Fact]
    public void IsConstant_MethodCall_IsNotConstant()
    {
        MethodCallExpression mc = Expression.Call(typeof(int).GetMethod(nameof(int.Parse), new[] { typeof(string) })!, Expression.Constant("1"));
        Assert.False(CommonHelpers.IsConstant(mc));
    }

    [Fact]
    public void IsConstant_ListInit_AllConstants_IsConstant()
    {
        ListInitExpression lie = Expression.ListInit(
            Expression.New(typeof(List<int>)),
            Expression.ElementInit(typeof(List<int>).GetMethod(nameof(List<int>.Add))!, Expression.Constant(1)),
            Expression.ElementInit(typeof(List<int>).GetMethod(nameof(List<int>.Add))!, Expression.Constant(2)));
        Assert.True(CommonHelpers.IsConstant(lie));
    }

    [Fact]
    public void IsConstant_ListInit_WithNonConstantElement_IsNotConstant()
    {
        ParameterExpression p = Expression.Parameter(typeof(int), "x");
        ListInitExpression lie = Expression.ListInit(
            Expression.New(typeof(List<int>)),
            Expression.ElementInit(typeof(List<int>).GetMethod(nameof(List<int>.Add))!, p));
        Assert.False(CommonHelpers.IsConstant(lie));
    }

    [Fact]
    public void GetConstantValue_StaticProperty_ReturnsValue()
    {
        MemberExpression nl = Expression.Property(null, typeof(Environment).GetProperty(nameof(Environment.NewLine))!);
        object? value = CommonHelpers.GetConstantValue(nl);
        Assert.Equal(Environment.NewLine, value);
    }

    [Fact]
    public void GetConstantValue_StaticField_ReturnsValue()
    {
        MemberExpression me = Expression.Field(null, typeof(int).GetField(nameof(int.MaxValue))!);
        object? value = CommonHelpers.GetConstantValue(me);
        Assert.Equal(int.MaxValue, value);
    }

    [Fact]
    public void GetConstantValue_NewListInit_BuildsList()
    {
        ListInitExpression lie = Expression.ListInit(
            Expression.New(typeof(List<int>)),
            Expression.ElementInit(typeof(List<int>).GetMethod(nameof(List<int>.Add))!, Expression.Constant(10)),
            Expression.ElementInit(typeof(List<int>).GetMethod(nameof(List<int>.Add))!, Expression.Constant(20)));

        object? value = CommonHelpers.GetConstantValue(lie);
        Assert.Equal(new List<int> { 10, 20 }, value);
    }

    [Fact]
    public void GetConstantValue_NewStructWithoutCtor_ActivatesDefault()
    {
        NewExpression ne = Expression.New(typeof(StructWithoutCtor));
        object? value = CommonHelpers.GetConstantValue(ne);
        Assert.Equal(default(StructWithoutCtor), value);
    }

    [Fact]
    public void BracketIfNeeded_RequiresBrackets_True_Wraps()
    {
        SQLExpression input = new(typeof(int), 0, "1+2", null) { RequiresBrackets = true };
        SQLExpression result = CommonHelpers.BracketIfNeeded(input);
        Assert.Equal("(1+2)", result.Sql);
    }

    [Fact]
    public void BracketIfNeeded_RequiresBrackets_False_PassesThrough()
    {
        SQLExpression input = new(typeof(int), 0, "X.Y", null);
        SQLExpression result = CommonHelpers.BracketIfNeeded(input);
        Assert.Same(input, result);
    }

    [Fact]
    public void CombineParameters_Two_AllNull_ReturnsNull()
    {
        SQLExpression a = new(typeof(int), 0, "1", null);
        SQLExpression b = new(typeof(int), 1, "2", null);
        Assert.Null(CommonHelpers.CombineParameters(a, b));
    }

    [Fact]
    public void CombineParameters_Two_OneSide_Combines()
    {
        SQLiteParameter p = new() { Name = "@p0", Value = 1 };
        SQLExpression a = new(typeof(int), 0, "@p0", new[] { p });
        SQLExpression b = new(typeof(int), 1, "2", null);
        SQLiteParameter[]? combined = CommonHelpers.CombineParameters(a, b);
        Assert.NotNull(combined);
        Assert.Single(combined);
    }

    [Fact]
    public void CombineParameters_Three_AllNull_ReturnsNull()
    {
        SQLExpression a = new(typeof(int), 0, "1", null);
        SQLExpression b = new(typeof(int), 1, "2", null);
        SQLExpression c = new(typeof(int), 2, "3", null);
        Assert.Null(CommonHelpers.CombineParameters(a, b, c));
    }

    [Fact]
    public void CombineParameters_Three_HasParameters_Combines()
    {
        SQLiteParameter p1 = new() { Name = "@p0", Value = 1 };
        SQLiteParameter p2 = new() { Name = "@p1", Value = 2 };
        SQLExpression a = new(typeof(int), 0, "@p0", new[] { p1 });
        SQLExpression b = new(typeof(int), 1, "2", null);
        SQLExpression c = new(typeof(int), 2, "@p1", new[] { p2 });
        SQLiteParameter[]? combined = CommonHelpers.CombineParameters(a, b, c);
        Assert.NotNull(combined);
        Assert.Equal(2, combined.Length);
    }

    [Fact]
    public void TypeToSQLiteType_UnsupportedType_Throws()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").Build();
        Assert.Throws<NotSupportedException>(() =>
            CommonHelpers.TypeToSQLiteType(typeof(IntPtr), options));
    }

    [Fact]
    public void GetConstantValue_ConvertExpression_AppliesConversion()
    {
        UnaryExpression conv = Expression.Convert(Expression.Constant(7), typeof(long));
        object? value = CommonHelpers.GetConstantValue(conv);
        Assert.Equal(7L, value);
    }

    [Fact]
    public void GetConstantValue_NewArrayInit_ReturnsEnumerable()
    {
        NewArrayExpression arr = Expression.NewArrayInit(typeof(int), Expression.Constant(1), Expression.Constant(2));
        object? value = CommonHelpers.GetConstantValue(arr);
        Assert.NotNull(value);
        Assert.Equal(new object?[] { 1, 2 }, ((System.Collections.IEnumerable)value).Cast<object?>().ToArray());
    }

    [Fact]
    public void GetConstantValue_MemberInit_BuildsObject()
    {
        MemberInitExpression mie = Expression.MemberInit(
            Expression.New(typeof(CommonHelpersTestTarget)),
            Expression.Bind(typeof(CommonHelpersTestTarget).GetProperty(nameof(CommonHelpersTestTarget.Value))!,
                Expression.Constant(42)));
        object? value = CommonHelpers.GetConstantValue(mie);
        Assert.IsType<CommonHelpersTestTarget>(value);
        Assert.Equal(42, ((CommonHelpersTestTarget)value).Value);
    }

    [Fact]
    public void GetConstantValue_UnsupportedNodeType_Throws()
    {
        ParameterExpression p = Expression.Parameter(typeof(int), "x");
        Assert.Throws<NotSupportedException>(() => CommonHelpers.GetConstantValue(p));
    }

    [Fact]
    public void IsConstant_ListInit_NewExpressionNotConstant_IsNotConstant()
    {
        ParameterExpression cap = Expression.Parameter(typeof(int), "cap");
        NewExpression ne = Expression.New(
            typeof(List<int>).GetConstructor(new[] { typeof(int) })!,
            cap);
        ListInitExpression lie = Expression.ListInit(
            ne,
            Expression.ElementInit(typeof(List<int>).GetMethod(nameof(List<int>.Add))!, Expression.Constant(1)));
        Assert.False(CommonHelpers.IsConstant(lie));
    }

    [Fact]
    public void IsConstant_StaticField_IsConstant()
    {
        MemberExpression me = Expression.Field(null, typeof(int).GetField(nameof(int.MaxValue))!);
        Assert.True(CommonHelpers.IsConstant(me));
    }

    [Fact]
    public void GetConstantValue_InstancePropertyOnConstant_ReturnsValue()
    {
        MemberExpression me = Expression.Property(Expression.Constant("hello"), nameof(string.Length));
        object? value = CommonHelpers.GetConstantValue(me);
        Assert.Equal(5, value);
    }

    [Fact]
    public void GetConstantValue_InstanceFieldOnConstant_ReturnsValue()
    {
        CommonHelpersFieldHolder holder = new() { Number = 99 };
        MemberExpression me = Expression.Field(Expression.Constant(holder), nameof(CommonHelpersFieldHolder.Number));
        object? value = CommonHelpers.GetConstantValue(me);
        Assert.Equal(99, value);
    }

    [Fact]
    public void CombineParameters_Three_AllHaveParameters_AllPresent()
    {
        SQLiteParameter p1 = new() { Name = "@p0", Value = 1 };
        SQLiteParameter p2 = new() { Name = "@p1", Value = 2 };
        SQLiteParameter p3 = new() { Name = "@p2", Value = 3 };
        SQLExpression a = new(typeof(int), 0, "@p0", new[] { p1 });
        SQLExpression b = new(typeof(int), 1, "@p1", new[] { p2 });
        SQLExpression c = new(typeof(int), 2, "@p2", new[] { p3 });
        SQLiteParameter[]? combined = CommonHelpers.CombineParameters(a, b, c);
        Assert.NotNull(combined);
        Assert.Equal(3, combined.Length);
    }

    [Fact]
    public void CombineParameters_Two_AllHaveParameters_AllPresent()
    {
        SQLiteParameter p1 = new() { Name = "@p0", Value = 1 };
        SQLiteParameter p2 = new() { Name = "@p1", Value = 2 };
        SQLExpression a = new(typeof(int), 0, "@p0", new[] { p1 });
        SQLExpression b = new(typeof(int), 1, "@p1", new[] { p2 });
        SQLiteParameter[]? combined = CommonHelpers.CombineParameters(a, b);
        Assert.NotNull(combined);
        Assert.Equal(2, combined.Length);
    }

    [Fact]
    public void CombineParameters_Three_OnlyMiddleHasParameters_Combines()
    {
        SQLiteParameter p = new() { Name = "@p0", Value = 1 };
        SQLExpression a = new(typeof(int), 0, "1", null);
        SQLExpression b = new(typeof(int), 1, "@p0", new[] { p });
        SQLExpression c = new(typeof(int), 2, "3", null);
        SQLiteParameter[]? combined = CommonHelpers.CombineParameters(a, b, c);
        Assert.NotNull(combined);
        Assert.Single(combined);
    }

    [Fact]
    public void CombineParameters_Three_OnlyLastHasParameters_Combines()
    {
        SQLiteParameter p = new() { Name = "@p0", Value = 1 };
        SQLExpression a = new(typeof(int), 0, "1", null);
        SQLExpression b = new(typeof(int), 1, "2", null);
        SQLExpression c = new(typeof(int), 2, "@p0", new[] { p });
        SQLiteParameter[]? combined = CommonHelpers.CombineParameters(a, b, c);
        Assert.NotNull(combined);
        Assert.Single(combined);
    }

    [Fact]
    public void GetConstantValue_MemberInitWithFieldBinding_SetsField()
    {
        MemberInitExpression mie = Expression.MemberInit(
            Expression.New(typeof(CommonHelpersFieldHolder)),
            Expression.Bind(typeof(CommonHelpersFieldHolder).GetField(nameof(CommonHelpersFieldHolder.Number))!,
                Expression.Constant(123)));
        object? value = CommonHelpers.GetConstantValue(mie);
        Assert.IsType<CommonHelpersFieldHolder>(value);
        Assert.Equal(123, ((CommonHelpersFieldHolder)value).Number);
    }

    [Fact]
    public void GetConstantValue_MemberInitOnStructWithoutCtor_BuildsValue()
    {
        MemberInitExpression mie = Expression.MemberInit(
            Expression.New(typeof(StructWithoutCtor)),
            Expression.Bind(typeof(StructWithoutCtor).GetField(nameof(StructWithoutCtor.Value))!,
                Expression.Constant(77)));
        object? value = CommonHelpers.GetConstantValue(mie);
        Assert.IsType<StructWithoutCtor>(value);
        Assert.Equal(77, ((StructWithoutCtor)value).Value);
    }

    [Fact]
    public void GetConstantValue_MemberInitWithMemberMemberBinding_Throws()
    {
        MemberInitExpression mie = Expression.MemberInit(
            Expression.New(typeof(CommonHelpersOuterTarget)),
            Expression.MemberBind(
                typeof(CommonHelpersOuterTarget).GetProperty(nameof(CommonHelpersOuterTarget.Inner))!,
                Expression.Bind(typeof(CommonHelpersTestTarget).GetProperty(nameof(CommonHelpersTestTarget.Value))!,
                    Expression.Constant(5))));

        Assert.Throws<NotSupportedException>(() => CommonHelpers.GetConstantValue(mie));
    }

    public struct StructWithoutCtor
    {
        public int Value;
    }
}

public class CommonHelpersTestTarget
{
    public int Value { get; set; }
}

public class CommonHelpersFieldHolder
{
    public int Number;
}

public class CommonHelpersOuterTarget
{
    public CommonHelpersTestTarget Inner { get; } = new();
}
