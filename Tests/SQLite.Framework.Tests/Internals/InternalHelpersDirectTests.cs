using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Internals.Visitors.SQL;
using SQLite.Framework.Extensions;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;
using QueryCompilerVisitor = SQLite.Framework.Internals.Visitors.QueryCompilerVisitor;
using CommandHelpers = SQLite.Framework.Internals.Helpers.CommandHelpers;
using CommonHelpers = SQLite.Framework.Internals.Helpers.CommonHelpers;
using ExpressionHelpers = SQLite.Framework.Internals.Helpers.ExpressionHelpers;
using TypeHelpers = SQLite.Framework.Internals.Helpers.TypeHelpers;
using ParameterHelpers = SQLite.Framework.Internals.Helpers.ParameterHelpers;
using FtsRenderState = SQLite.Framework.Internals.FTS5.FtsRenderState;
using UpsertSqlBuilder = SQLite.Framework.Internals.Helpers.UpsertSqlBuilder;
using SqlLiteralHelper = SQLite.Framework.Internals.Helpers.SqlLiteralHelper;
using IdentifierGuard = SQLite.Framework.Internals.Helpers.IdentifierGuard;
using ColumnBinderFactory = SQLite.Framework.Internals.Helpers.ColumnBinderFactory;

namespace SQLite.Framework.Tests;

public class InternalHelpersDirectTests
{
    private static readonly SQLiteOptions CompilerOptions = new SQLiteOptionsBuilder("compiler-internal-direct.db3").Build();

    [Fact]
    public void SqlLiteralHelper_InlineParameters_NoParameters_ReturnsSqlUnchanged()
    {
        string sql = "SELECT 1";
        string result = SqlLiteralHelper.InlineParameters(sql, [], CompilerOptions);
        Assert.Same(sql, result);
    }

    [Fact]
    public void SqlLiteralHelper_InlineParameters_LongerNameWinsOverPrefix()
    {
        List<SQLiteParameter> parameters =
        [
            new() { Name = "@p1", Value = 1 },
            new() { Name = "@p10", Value = 10 },
        ];

        string result = SqlLiteralHelper.InlineParameters("a = @p1 AND b = @p10", parameters, CompilerOptions);

        Assert.Equal("a = 1 AND b = 10", result);
    }

    [Fact]
    public void SqlLiteralHelper_InlineParameters_DoesNotRescanAlreadyInlinedLiteral()
    {
        List<SQLiteParameter> parameters =
        [
            new() { Name = "@p0", Value = "contains @p1 token" },
            new() { Name = "@p1", Value = 42 },
        ];

        string result = SqlLiteralHelper.InlineParameters("x = @p0 AND y = @p1", parameters, CompilerOptions);

        Assert.Equal("x = 'contains @p1 token' AND y = 42", result);
    }

    [Fact]
    public void IdentifierGuard_EnsureNoQuote_PlainName_DoesNotThrow()
    {
        IdentifierGuard.EnsureNoQuote("PlainName", "Table");
    }

    [Fact]
    public void IdentifierGuard_EnsureNoQuote_NameWithQuote_Throws()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => IdentifierGuard.EnsureNoQuote("Bad\"Name", "Column"));
        Assert.Contains("Column", ex.Message);
        Assert.Contains("double-quote", ex.Message);
    }

    [Fact]
    public void CommandHelpers_ReadColumnValue_UnknownColumnType_Throws()
    {
        using TestDatabase db = new();
        SQLiteCommand cmd = db.CreateCommand("SELECT 1", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();
        reader.Read();

        Assert.Throws<NotSupportedException>(() =>
            CommandHelpers.ReadColumnValue(reader.Statement!, 0, (SQLiteColumnType)999, typeof(int), db.Options));
    }

    [Fact]
    public void CommandHelpers_ReadColumnValue_BlobToString_DecodesUtf8()
    {
        using TestDatabase db = new();
        SQLiteCommand cmd = db.CreateCommand("SELECT x'48656C6C6F'", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();
        reader.Read();

        object? result = CommandHelpers.ReadColumnValue(reader.Statement!, 0, SQLiteColumnType.Blob, typeof(string), db.Options);
        Assert.Equal("Hello", result);
    }

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void WindowFunctionsMemberVisitor_HandleWindowFunctionMethod_UnknownMethodName_Throws()
    {
        using TestDatabase db = new();
        SQLite.Framework.Internals.Visitors.SQL.SQLVisitor visitor = new(
            db,
            new SQLite.Framework.Models.SQLiteCounters(),
            level: 0);

        MethodCallExpression unknownCall = Expression.Call(
            typeof(string).GetMethod(nameof(string.IsNullOrEmpty), new[] { typeof(string) })!,
            Expression.Constant(""));

        SQLiteCallerContext ctx = (SQLiteCallerContext)Activator.CreateInstance(
            typeof(SQLiteCallerContext),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { visitor, unknownCall },
            culture: null)!;

        Type visitorType = typeof(SQLiteDatabase).Assembly
            .GetType("SQLite.Framework.Internals.Visitors.Member.WindowFunctionsMemberVisitor")!;
        MethodInfo handler = visitorType.GetMethod(
            "HandleWindowFunctionMethod",
            BindingFlags.Public | BindingFlags.Static)!;

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(
            () => handler.Invoke(null, new object?[] { ctx }));
        Assert.IsType<NotSupportedException>(ex.InnerException);
        Assert.Contains("not translatable to SQL", ex.InnerException!.Message);
    }
#endif

    [Fact]
    public void FtsRenderState_WriteFts5Call_UnknownMethodName_Throws()
    {
        using TestDatabase db = new();
        SQLite.Framework.Internals.Visitors.SQL.SQLVisitor visitor = new(
            db,
            new SQLite.Framework.Models.SQLiteCounters(),
            level: 0);

        FtsRenderState state = new(visitor);

        MethodCallExpression unknownCall = Expression.Call(
            typeof(string).GetMethod(nameof(string.IsNullOrEmpty), new[] { typeof(string) })!,
            Expression.Constant(""));

        MethodInfo method = typeof(FtsRenderState).GetMethod(
            "WriteFts5Call",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(state, new object?[] { unknownCall }));
        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void CommonHelpers_CreateNew_TypeWithoutInstance_Throws()
    {
        NewExpression ne = Expression.New(typeof(int?));
        Assert.Throws<InvalidOperationException>(() => ExpressionHelpers.GetConstantValue(ne));
    }

    [Fact]
    public void CommonHelpers_CreateMember_TypeWithoutInstance_Throws()
    {
        MemberInitExpression mie = Expression.MemberInit(
            Expression.New(typeof(int?)),
            Array.Empty<MemberBinding>());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => ExpressionHelpers.GetConstantValue(mie));
        Assert.Contains("Cannot create instance", ex.Message);
    }

    [Fact]
    public void CommonHelpers_CreateMember_BindingMemberIsEvent_Throws()
    {
        EventInfo eventInfo = typeof(InternalHelpersTestEntity).GetEvent(nameof(InternalHelpersTestEntity.SomethingHappened))!;
        ConstructorInfo maCtor = typeof(MemberAssignment).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(MemberInfo), typeof(Expression) },
            modifiers: null)!;
        MemberAssignment fakeBinding = (MemberAssignment)maCtor.Invoke(new object?[]
        {
            eventInfo,
            Expression.Constant(null, typeof(EventHandler))
        });

        MemberInitExpression mie = Expression.MemberInit(
            Expression.New(typeof(InternalHelpersTestEntity)),
            new MemberBinding[] { fakeBinding });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => ExpressionHelpers.GetConstantValue(mie));
        Assert.Contains("not found in type", ex.Message);
    }

    [Fact]
    public void UpsertSqlBuilder_Build_DoUpdateReferencesUnknownProperty_Throws()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping<Book>();

        SQLiteUpsertConflictTarget<Book> target = ConstructConflictTarget<Book>(
            new[] { "Id" },
            ConstructDoUpdateAction<Book>(new[] { "DoesNotExist" }));

        Assert.Throws<InvalidOperationException>(() =>
            UpsertSqlBuilder.Build(db, mapping, target, (_, name) => name));
    }

    [Fact]
    public void UpsertSqlBuilder_Build_UnknownActionKind_Throws()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping<Book>();

        SQLiteUpsertConflictTarget<Book> target = ConstructConflictTarget<Book>(
            new[] { "Id" },
            ConstructActionWithKind<Book>(999));

        Assert.Throws<InvalidOperationException>(() =>
            UpsertSqlBuilder.Build(db, mapping, target, (_, name) => name));
    }

    private static SQLiteUpsertConflictTarget<T> ConstructConflictTarget<T>(IReadOnlyList<string> conflictColumns, SQLiteUpsertAction<T> action)
    {
        ConstructorInfo ctor = typeof(SQLiteUpsertConflictTarget<T>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(IReadOnlyList<string>) },
            modifiers: null)!;
        SQLiteUpsertConflictTarget<T> target = (SQLiteUpsertConflictTarget<T>)ctor.Invoke(new object[] { conflictColumns });

        FieldInfo actionField = typeof(SQLiteUpsertConflictTarget<T>).GetField("action", BindingFlags.Instance | BindingFlags.NonPublic)!;
        actionField.SetValue(target, action);
        return target;
    }

    private static SQLiteUpsertAction<T> ConstructDoUpdateAction<T>(IReadOnlyList<string> columns)
    {
        ConstructorInfo ctor = typeof(SQLiteUpsertAction<T>).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        Type kindType = typeof(SQLiteUpsertAction<T>).Assembly.GetType("SQLite.Framework.Internals.Enums.UpsertActionKind", throwOnError: true)!;
        object doUpdateKind = Enum.ToObject(kindType, 1);
        return (SQLiteUpsertAction<T>)ctor.Invoke(new object?[] { doUpdateKind, columns });
    }

    private static SQLiteUpsertAction<T> ConstructActionWithKind<T>(int kindValue)
    {
        ConstructorInfo ctor = typeof(SQLiteUpsertAction<T>).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        Type kindType = typeof(SQLiteUpsertAction<T>).Assembly.GetType("SQLite.Framework.Internals.Enums.UpsertActionKind", throwOnError: true)!;
        object kind = Enum.ToObject(kindType, kindValue);
        return (SQLiteUpsertAction<T>)ctor.Invoke(new object?[] { kind, null });
    }

    [Fact]
    public void AliasVisitor_ConstructorParameterCountMismatch_Throws()
    {
        using TestDatabase db = new();

        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);
        AliasVisitor aliasVisitor = new(db, sqlVisitor);

        ConstructorInfo oneArgCtor = typeof(InternalHelpersOneArg).GetConstructor([typeof(int)])!;

        NewExpression mismatched = (NewExpression)RuntimeHelpers.GetUninitializedObject(typeof(NewExpression));
        typeof(NewExpression).GetField("<Constructor>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(mismatched, oneArgCtor);
        typeof(NewExpression).GetField("_arguments", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(mismatched, new List<Expression> { Expression.Constant(1), Expression.Constant(2) });

        ParameterExpression rowParam = Expression.Parameter(typeof(Book), "b");
        LambdaExpression lambda = Expression.Lambda(mismatched, rowParam);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => aliasVisitor.ResolveResultAlias(lambda));
        Assert.Contains("has 1 parameters", ex.Message);
        Assert.Contains("2 arguments were provided", ex.Message);
    }

    [Fact]
    public void QueryFilterRebinder_ConcreteMemberNotFound_Throws()
    {
        Expression<Func<IRebindFoo, bool>> lambda = x => x.Tag == "x";

        NotSupportedException ex = Assert.Throws<NotSupportedException>(
            () => CommonHelpers.Rebind(lambda, typeof(RebindEntityWithExplicitImpl)));
        Assert.Contains("Tag", ex.Message);
        Assert.Contains(nameof(RebindEntityWithExplicitImpl), ex.Message);
    }

    [Fact]
    public void RowParameterExpander_EmptyRowParameters_ReturnsLambdaUnchanged()
    {
        Expression<Func<int, int>> lambda = x => x + 1;
        LambdaExpression result = CommonHelpers.ExpandRowsInMethodCalls(lambda, []);
        Assert.Same(lambda, result);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitParameter_InInputParameters_ReturnsContextInput()
    {
        ParameterExpression param = Expression.Parameter(typeof(int), "x");
        QueryCompilerVisitor visitor = new(CompilerOptions, [param]);

        Expression result = visitor.Visit(param);

        SQLiteQueryContext ctx = new() { Input = 42 };
        CompiledExpression compiled = Assert.IsType<CompiledExpression>(result);
        Assert.Equal(42, compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitBinary_ArrayIndexNonArrayAtRuntime_Throws()
    {
        BinaryExpression node = Expression.ArrayIndex(
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(0));

        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        Assert.Throws<InvalidOperationException>(() => compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitBinary_DefaultArm_Throws()
    {
        ParameterExpression param = Expression.Parameter(typeof(int));
        BinaryExpression node = Expression.Assign(param, Expression.Constant(5));
        QueryCompilerVisitor visitor = new(CompilerOptions, inputParameters: [param]);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        Assert.Throws<NotSupportedException>(() => compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitNew_NullConstructorOnValueType_Throws()
    {
        NewExpression node = Expression.New(typeof(int));
        Assert.Null(node.Constructor);

        QueryCompilerVisitor visitor = new(CompilerOptions);
        Assert.Throws<NotSupportedException>(() => visitor.Visit(node));
    }

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void AliasVisitor_VisitNewExpression_ZeroArgsWithNonNullMembers_FallsThroughToMethodEnd()
    {
        using TestDatabase db = new();
        SQLite.Framework.Internals.Visitors.SQL.SQLVisitor sqlVisitor = new(
            db,
            new SQLite.Framework.Models.SQLiteCounters(),
            level: 0);

        Type aliasVisitorType = typeof(SQLite.Framework.Internals.Helpers.CommonHelpers).Assembly
            .GetType("SQLite.Framework.Internals.Visitors.AliasVisitor")!;
        object aliasVisitor = Activator.CreateInstance(aliasVisitorType, db, sqlVisitor)!;

        ConstructorInfo ctor = typeof(NoArgWithMembersHolder).GetConstructor(Type.EmptyTypes)!;
        NewExpression node = Expression.New(
            ctor,
            Array.Empty<Expression>(),
            Array.Empty<MemberInfo>());
        LambdaExpression selector = Expression.Lambda(node);

        MethodInfo method = aliasVisitorType.GetMethod(
            "VisitNewExpression",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(aliasVisitor, new object?[] { selector, node, null });
    }
#endif

    [Fact]
    public void QueryableVisitor_ReverseBeforeTheFirstDistinct_MovesBeforeDistinct()
    {
        using TestDatabase db = new();
        QueryableVisitor visitor = new(db, new SQLVisitor(db, new SQLiteCounters(), 0));
        Expression<Func<IQueryable<int>, IQueryable<int>>> distinct = s => s.Distinct();
        Expression<Func<IQueryable<int>, IQueryable<int>>> reverse = s => s.Reverse();

        visitor.Visit((MethodCallExpression)reverse.Body);
        visitor.Visit((MethodCallExpression)distinct.Body);

        Assert.False(visitor.Reverse);
        Assert.True(visitor.ReverseBeforeDistinct);

        visitor.Visit((MethodCallExpression)reverse.Body);
        visitor.Visit((MethodCallExpression)distinct.Body);

        Assert.True(visitor.Reverse);
        Assert.True(visitor.ReverseBeforeDistinct);
    }

    [Fact]
    public void QueryableVisitor_ReverseBeforeASecondDistinct_StaysATrailingReverse()
    {
        using TestDatabase db = new();
        QueryableVisitor visitor = new(db, new SQLVisitor(db, new SQLiteCounters(), 0));
        Expression<Func<IQueryable<int>, IQueryable<int>>> distinct = s => s.Distinct();
        Expression<Func<IQueryable<int>, IQueryable<int>>> reverse = s => s.Reverse();

        visitor.Visit((MethodCallExpression)distinct.Body);
        Assert.False(visitor.ReverseBeforeDistinct);

        visitor.Visit((MethodCallExpression)reverse.Body);
        visitor.Visit((MethodCallExpression)distinct.Body);

        Assert.True(visitor.Reverse);
        Assert.False(visitor.ReverseBeforeDistinct);
    }

    [Fact]
    public void ConverterSql_HasReadAndWriteWrap_UnwrapsNullableValueTypes()
    {
        using TestDatabase db = new(b => b.TypeConverters[typeof(WrapOffsetVal)] = new WrapOffsetConverter());

        Assert.True(ConverterSql.HasReadAndWriteWrap(typeof(WrapOffsetVal?), db.Options));
        Assert.True(ConverterSql.HasReadAndWriteWrap(typeof(WrapOffsetVal), db.Options));
        Assert.False(ConverterSql.HasReadAndWriteWrap(typeof(int?), db.Options));
        Assert.False(ConverterSql.HasReadAndWriteWrap(typeof(int), db.Options));
    }

    [Fact]
    public void RowParameterExpanderVisitor_IsSimpleValue_UnwrapsNullableValueTypes()
    {
        MethodInfo method = typeof(RowParameterExpanderVisitor).GetMethod(
            "IsSimpleValue", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.True((bool)method.Invoke(null, [typeof(int?)])!);
        Assert.True((bool)method.Invoke(null, [typeof(Guid?)])!);
        Assert.True((bool)method.Invoke(null, [typeof(int)])!);
        Assert.False((bool)method.Invoke(null, [typeof(object)])!);
    }

    [Fact]
    public void ConvertThroughOperator_NullToNullableTarget_ReturnsNull()
    {
        MethodInfo method = typeof(ExpressionHelpers).GetMethod("ConvertThroughOperator", BindingFlags.NonPublic | BindingFlags.Static)!;
        UnaryExpression node = Expression.Convert(
            Expression.Constant(null, typeof(ConvOperatorSource?)),
            typeof(ConvOperatorTarget?),
            typeof(ConvOperatorMethods).GetMethod(nameof(ConvOperatorMethods.ToTarget))!);

        Assert.Null(method.Invoke(null, [node]));
    }

    [Fact]
    public void ConvertThroughOperator_NullToReferenceTarget_ReturnsNull()
    {
        MethodInfo method = typeof(ExpressionHelpers).GetMethod("ConvertThroughOperator", BindingFlags.NonPublic | BindingFlags.Static)!;
        UnaryExpression node = Expression.Convert(
            Expression.Constant(null, typeof(ConvOperatorSource?)),
            typeof(ConvOperatorBox),
            typeof(ConvOperatorMethods).GetMethod(nameof(ConvOperatorMethods.ToBox))!);

        Assert.Null(method.Invoke(null, [node]));
    }

    [Fact]
    public void ConvertThroughOperator_NullToValueTarget_Throws()
    {
        MethodInfo method = typeof(ExpressionHelpers).GetMethod("ConvertThroughOperator", BindingFlags.NonPublic | BindingFlags.Static)!;
        UnaryExpression node = Expression.Convert(
            Expression.Constant(null, typeof(ConvOperatorSource?)),
            typeof(ConvOperatorTarget),
            typeof(ConvOperatorMethods).GetMethod(nameof(ConvOperatorMethods.ToTarget))!);

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [node]));
        InvalidOperationException inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("Nullable object must have a value.", inner.Message);
    }

    [Fact]
    public void ConvertThroughOperator_ValueOperand_InvokesTheOperator()
    {
        MethodInfo method = typeof(ExpressionHelpers).GetMethod("ConvertThroughOperator", BindingFlags.NonPublic | BindingFlags.Static)!;
        UnaryExpression node = Expression.Convert(
            Expression.Constant(new ConvOperatorSource { V = 7 }, typeof(ConvOperatorSource?)),
            typeof(ConvOperatorTarget),
            typeof(ConvOperatorMethods).GetMethod(nameof(ConvOperatorMethods.ToTarget))!);

        object? result = method.Invoke(null, [node]);

        Assert.Equal(7, ((ConvOperatorTarget)result!).W);
    }

    [Fact]
    public void QueryCompilerVisitor_ApplyNestedAssignment_FieldStep_SetsTheNestedProperty()
    {
        MethodInfo method = typeof(QueryCompilerVisitor).GetMethod("ApplyNestedAssignment", BindingFlags.NonPublic | BindingFlags.Static)!;
        MmbOuter instance = new();
        MemberInfo innerField = typeof(MmbOuter).GetField(nameof(MmbOuter.InnerField))!;
        MemberInfo xProp = typeof(MmbInner).GetProperty(nameof(MmbInner.X))!;

        method.Invoke(null, [instance, new[] { innerField, xProp }, 5]);

        Assert.Equal(5, instance.InnerField.X);
    }

    [Fact]
    public void QueryCompilerVisitor_ApplyNestedAssignment_FieldLast_SetsTheField()
    {
        MethodInfo method = typeof(QueryCompilerVisitor).GetMethod("ApplyNestedAssignment", BindingFlags.NonPublic | BindingFlags.Static)!;
        CompilerVisitorHolderOwner instance = new();
        MemberInfo holderProp = typeof(CompilerVisitorHolderOwner).GetProperty(nameof(CompilerVisitorHolderOwner.Holder))!;
        MemberInfo fieldX = typeof(CompilerVisitorFieldHolder).GetField(nameof(CompilerVisitorFieldHolder.FieldX))!;

        method.Invoke(null, [instance, new[] { holderProp, fieldX }, 9]);

        Assert.Equal(9, instance.Holder.FieldX);
    }

    [Fact]
    public void QueryCompilerVisitor_FlattenMemberMemberBinding_ListBinding_Throws()
    {
        QueryCompilerVisitor visitor = new(CompilerOptions);
        PropertyInfo holderProp = typeof(CompilerVisitorHolderOwner).GetProperty(nameof(CompilerVisitorHolderOwner.Holder))!;
        FieldInfo listField = typeof(CompilerVisitorFieldHolder).GetField(nameof(CompilerVisitorFieldHolder.ListField))!;
        MethodInfo add = typeof(List<int>).GetMethod(nameof(List<int>.Add))!;

        MemberMemberBinding mmb = Expression.MemberBind(
            holderProp,
            Expression.ListBind(listField, Expression.ElementInit(add, Expression.Constant(1))));

        MethodInfo method = typeof(QueryCompilerVisitor).GetMethod(
            "FlattenMemberMemberBinding",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(visitor, new object?[] { mmb, new MemberInfo[] { holderProp } }));
        NotSupportedException inner = Assert.IsType<NotSupportedException>(ex.InnerException);
        Assert.Contains("List binding", inner.Message);
    }

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void QueryCompilerVisitor_VisitMember_FieldInfo_ReturnsFieldValue()
    {
        ParameterExpression tupleParam = Expression.Parameter(typeof(ValueTuple<int>), "t");
        MemberExpression node = Expression.Field(tupleParam, "Item1");

        QueryCompilerVisitor visitor = new(CompilerOptions, [tupleParam]);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new() { Input = new ValueTuple<int>(42) };
        Assert.Equal(42, compiled.Call(ctx));
    }
#endif

    [Fact]
    public void QueryCompilerVisitor_VisitUnary_NegateWithUserDefinedOperator_InvokesMethod()
    {
        ParameterExpression p = Expression.Parameter(typeof(CompilerVisitorNegatable), "v");
        UnaryExpression node = Expression.Negate(p);
        Assert.NotNull(node.Method);

        QueryCompilerVisitor visitor = new(CompilerOptions, [p]);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new() { Input = new CompilerVisitorNegatable(5) };
        CompilerVisitorNegatable result = (CompilerVisitorNegatable)compiled.Call(ctx)!;
        Assert.Equal(-5, result.Value);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitUnary_DefaultArm_Throws()
    {
        UnaryExpression node = Expression.UnaryPlus(Expression.Constant(3));
        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        Assert.Throws<NotSupportedException>(() => compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitUnary_ArrayLength_ReturnsLength()
    {
        UnaryExpression node = Expression.ArrayLength(Expression.Constant(new[] { 1, 2, 3 }));
        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        Assert.Equal(3, compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMemberInit_FieldAssignment_Direct()
    {
        NewExpression newExpr = Expression.New(typeof(CompilerVisitorFieldHolder));
        FieldInfo fld = typeof(CompilerVisitorFieldHolder).GetField(nameof(CompilerVisitorFieldHolder.FieldX))!;
        MemberAssignment assign = Expression.Bind(fld, Expression.Constant(42));
        MemberInitExpression node = Expression.MemberInit(newExpr, assign);

        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        CompilerVisitorFieldHolder result = (CompilerVisitorFieldHolder)compiled.Call(ctx)!;
        Assert.Equal(42, result.FieldX);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMemberInit_FieldListBinding_Direct()
    {
        NewExpression newExpr = Expression.New(typeof(CompilerVisitorFieldHolder));
        FieldInfo listFld = typeof(CompilerVisitorFieldHolder).GetField(nameof(CompilerVisitorFieldHolder.ListField))!;
        MethodInfo addMethod = typeof(List<int>).GetMethod(nameof(List<int>.Add))!;
        MemberListBinding listBinding = Expression.ListBind(listFld,
            Expression.ElementInit(addMethod, Expression.Constant(1)),
            Expression.ElementInit(addMethod, Expression.Constant(2)));
        MemberInitExpression node = Expression.MemberInit(newExpr, listBinding);

        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        CompilerVisitorFieldHolder result = (CompilerVisitorFieldHolder)compiled.Call(ctx)!;
        Assert.Equal([1, 2], result.ListField);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMemberMemberBinding_WithNestedListBinding_Throws()
    {
        NewExpression newOuter = Expression.New(typeof(CompilerVisitorListContainer));
        PropertyInfo innerProp = typeof(CompilerVisitorListContainer).GetProperty(nameof(CompilerVisitorListContainer.Inner))!;
        PropertyInfo listProp = typeof(CompilerVisitorInnerWithList).GetProperty(nameof(CompilerVisitorInnerWithList.Items))!;

        MethodInfo addMethod = typeof(List<int>).GetMethod(nameof(List<int>.Add))!;
        MemberListBinding nestedList = Expression.ListBind(listProp, Expression.ElementInit(addMethod, Expression.Constant(1)));
        MemberMemberBinding innerBinding = Expression.MemberBind(innerProp, nestedList);

        MemberInitExpression node = Expression.MemberInit(newOuter, innerBinding);

        QueryCompilerVisitor visitor = new(CompilerOptions);
        Assert.Throws<NotSupportedException>(() => visitor.Visit(node));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMemberInit_MemberMemberBinding_Direct()
    {
        NewExpression newOuter = Expression.New(typeof(CompilerVisitorOuter));
        PropertyInfo innerProp = typeof(CompilerVisitorOuter).GetProperty(nameof(CompilerVisitorOuter.Inner))!;
        PropertyInfo xProp = typeof(CompilerVisitorInner).GetProperty(nameof(CompilerVisitorInner.X))!;

        MemberAssignment innerXAssign = Expression.Bind(xProp, Expression.Constant(42));
        MemberMemberBinding innerBinding = Expression.MemberBind(innerProp, innerXAssign);

        MemberInitExpression node = Expression.MemberInit(newOuter, innerBinding);

        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        object? result = compiled.Call(ctx);
        Assert.IsType<CompilerVisitorOuter>(result);
    }

    [Fact]
    public void QueryCompilerVisitor_InvokeOperator_AllNumericTypes_Direct()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("compiler-direct.db3").Build();
        Type t = typeof(QueryCompilerVisitor);
        MethodInfo invokeOp = t.GetMethod("InvokeOperator", BindingFlags.Static | BindingFlags.NonPublic)!;

        MethodInfo Op(string name) => (MethodInfo)t.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        MethodInfo addM = Op("BinaryAdditionOperator");
        MethodInfo subM = Op("BinarySubtractionOperator");
        MethodInfo mulM = Op("BinaryMultiplyOperator");
        MethodInfo divM = Op("BinaryDivisionOperator");
        MethodInfo modM = Op("BinaryModulusOperator");

        foreach (MethodInfo op in new[] { addM, subM, mulM, divM, modM })
        {
            Assert.NotNull(invokeOp.Invoke(null, [op, 5, 2, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5L, 2L, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5.0, 2.0, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5f, 2f, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5m, 2m, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, (short)5, (short)2, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, (ushort)5, (ushort)2, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, (byte)5, (byte)2, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, (sbyte)5, (sbyte)2, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5u, 2u, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5ul, 2ul, options]));
        }
    }

    [Fact]
    public void QueryCompilerVisitor_InvokeBitwiseShiftOperator_AllTypes_Direct()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("compiler-direct-bitwise.db3").Build();
        Type t = typeof(QueryCompilerVisitor);
        MethodInfo invokeOp = t.GetMethod("InvokeOperator", BindingFlags.Static | BindingFlags.NonPublic)!;

        MethodInfo Op(string name) => (MethodInfo)t.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        foreach (MethodInfo op in new[] { Op("BinaryBitwiseAndOperator"), Op("BinaryBitwiseOrOperator"), Op("BinaryExclusiveOrOperator") })
        {
            Assert.NotNull(invokeOp.Invoke(null, [op, true, false, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 12, 10, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 12L, 10L, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 12u, 10u, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 12ul, 10ul, options]));
        }

        foreach (MethodInfo op in new[] { Op("BinaryLeftShiftOperator"), Op("BinaryRightShiftOperator") })
        {
            Assert.NotNull(invokeOp.Invoke(null, [op, 12, 1, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 12L, 1, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 12u, 1, options]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 12ul, 1, options]));
        }
    }

    [Fact]
    public void QueryCompilerVisitor_InvokeUnaryOperator_AllNumericTypes_Direct()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("compiler-direct-unary.db3").Build();
        Type t = typeof(QueryCompilerVisitor);
        MethodInfo invokeUnary = t.GetMethod("InvokeUnaryOperator", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo negM = (MethodInfo)t.GetField("BinaryNegationOperator", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5, options]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5L, options]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5.0, options]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5f, options]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5m, options]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, (short)5, options]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, (sbyte)5, options]));
    }

    [Fact]
    public void QueryCompilerVisitor_InvokeOperator_UnknownOperator_FallsThroughToGeneric()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("compiler-direct-unknown-bin.db3").Build();
        Type t = typeof(QueryCompilerVisitor);
        MethodInfo invokeOp = t.GetMethod("InvokeOperator", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo negation = (MethodInfo)t.GetField("BinaryNegationOperator", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        Assert.Throws<TargetInvocationException>(() => invokeOp.Invoke(null, [negation, 5, 2, options]));
    }

    [Fact]
    public void QueryCompilerVisitor_InvokeUnaryOperator_UnknownOperator_FallsThroughToGeneric()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("compiler-direct-unknown-un.db3").Build();
        Type t = typeof(QueryCompilerVisitor);
        MethodInfo invokeUnary = t.GetMethod("InvokeUnaryOperator", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo addition = (MethodInfo)t.GetField("BinaryAdditionOperator", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        Assert.Throws<TargetInvocationException>(() => invokeUnary.Invoke(null, [addition, 5, options]));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitUnary_ConvertChecked_Converts()
    {
        ParameterExpression pp = Expression.Parameter(typeof(long), "v");
        UnaryExpression node = Expression.ConvertChecked(pp, typeof(int));

        QueryCompilerVisitor visitor = new(CompilerOptions, [pp]);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new() { Input = 42L };
        Assert.Equal(42, compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitTypeBinary_TypeEqual_NonNullValue_ReturnsTrue()
    {
        QueryCompilerVisitor visitor = new(CompilerOptions);
        TypeBinaryExpression node = Expression.TypeEqual(Expression.Constant("hello", typeof(object)), typeof(string));
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        Assert.True((bool)compiled.Call(new SQLiteQueryContext())!);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitTypeBinary_TypeEqual_NullValue_ReturnsFalse()
    {
        QueryCompilerVisitor visitor = new(CompilerOptions);
        TypeBinaryExpression node = Expression.TypeEqual(Expression.Constant(null, typeof(object)), typeof(string));
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        Assert.False((bool)compiled.Call(new SQLiteQueryContext())!);
    }

    [Fact]
    public void RowParameterExpander_IsFrameworkTranslatedMethod_NullDeclaringType_ReturnsFalse()
    {
        MethodInfo isFrameworkTranslatedMethod = typeof(RowParameterExpanderVisitor)
            .GetMethod("IsFrameworkTranslatedMethod", BindingFlags.Static | BindingFlags.NonPublic)!;

        bool result = (bool)isFrameworkTranslatedMethod.Invoke(null, [new NullDeclaringTypeMethodInfo()])!;
        Assert.False(result);
    }

    [Fact]
    public void SQLiteDatabase_FindRootElementType_NonGenericRoot_ReturnsType()
    {
        MethodInfo method = typeof(SQLiteDatabase).GetMethod(
            "FindRootElementType",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        Expression nonGenericRoot = Expression.Constant(42, typeof(int));
        Type result = (Type)method.Invoke(null, [nonGenericRoot])!;

        Assert.Equal(typeof(int), result);
    }

    [Fact]
    public void PropertyVisitor_HandleStringProperty_UnknownName_ReturnsOriginalNode()
    {
        using TestDatabase db = new();

        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        SQLiteExpression source = SQLiteExpression.Leaf(typeof(string), 0, "\"Title\"", null);
        Expression result = StringMemberVisitor.HandleStringProperty(sqlVisitor, "NotARealProperty", typeof(string), source);

        Assert.Same(source, result);
    }

    [Fact]
    public void PropertyVisitor_HandleDateOnlyProperty_UnknownName_ReturnsOriginalNode()
    {
        using TestDatabase db = new();

        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        SQLiteExpression source = SQLiteExpression.Leaf(typeof(System.DateOnly), 0, "\"Date\"", null);
        Expression result = DateTimeMemberVisitor.HandleDateOnlyProperty(sqlVisitor, "NotARealProperty", typeof(System.DateOnly), source);

        Assert.Same(source, result);
    }

    [Fact]
    public void StaticObjectEquals_UntranslatableOperand_FallsBackClientSide()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();
        db.Table<NumericType>().Add(new NumericType { Id = 1, IntValue = 7 });

        System.Collections.Generic.List<bool> actual = db.Table<NumericType>()
            .Select(x => object.Equals(InterceptorHelpers.IdentityInt(x.IntValue), x.IntValue))
            .ToList();

        Assert.Equal(new System.Collections.Generic.List<bool> { true }, actual);
    }

    [Fact]
    public void QueryableMethodVisitor_VisitContains_ArgResolvesToNonConstantNonSql_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);
        sqlVisitor.TableColumns["Id"] = SQLiteExpression.Leaf(typeof(int), 0, "b0.Id");

        QueryableVisitor qmv = new(db, sqlVisitor);

        MethodInfo containsMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == nameof(Queryable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(int));

        ConstantExpression source = Expression.Constant(Array.Empty<int>().AsQueryable(), typeof(IQueryable<int>));
        Expression weirdArg = Expression.Default(typeof(int));
        MethodCallExpression contains = Expression.Call(containsMethod, source, weirdArg);

        MethodInfo visitContains = typeof(QueryableVisitor).GetMethod(
            "VisitContains",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => visitContains.Invoke(qmv, [contains]));
        Assert.NotNull(tie.InnerException);
        Assert.Contains("Unsupported expression type", tie.InnerException!.Message);
        Assert.Contains("in Contains", tie.InnerException.Message);
    }

    [Fact]
    public void QueryableMethodVisitor_ResolveTable_UnsupportedBodyType_Throws()
    {
        using TestDatabase db = new();

        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);
        QueryableVisitor qmv = new(db, sqlVisitor);

        Expression unsupportedBody = Expression.Default(typeof(int));

        MethodInfo resolveTable = typeof(QueryableVisitor).GetMethod(
            "ResolveTable",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => resolveTable.Invoke(qmv, [unsupportedBody]));
        Assert.NotNull(tie.InnerException);
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("not supported in join", tie.InnerException!.Message);
    }

    [Fact]
    public void MethodVisitor_HandleSQLiteFunctionsMethod_UnknownName_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo unknownMethod = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;
        MethodCallExpression mce = Expression.Call(unknownMethod, Expression.Constant(""));

        Assert.Throws<NotSupportedException>(() => SQLiteFunctionsMemberVisitor.HandleSQLiteFunctionsMethod(new SQLiteCallerContext(sqlVisitor, mce)));
    }

    [Fact]
    public void MethodVisitor_HandleGroupingMethod_UnknownAggregate_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression groupingParam = Expression.Parameter(typeof(IGrouping<int, Book>), "g");
        sqlVisitor.MethodArguments[groupingParam] = new Dictionary<string, Expression>
        {
            ["Key"] = SQLiteExpression.Leaf(typeof(int), 0, "b0.Id")
        };

        MethodInfo firstMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.First) && m.GetParameters().Length == 1)
            .MakeGenericMethod(typeof(Book));
        MethodCallExpression mce = Expression.Call(firstMethod, groupingParam);

        Assert.Throws<NotSupportedException>(() => QueryableMemberVisitor.HandleGroupingMethod(sqlVisitor, mce));
    }

    [Fact]
    public void MethodVisitor_HandleGroupingMethod_KeyNotResolvable_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression groupingParam = Expression.Parameter(typeof(IGrouping<int, Book>), "g");

        MethodInfo sumMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Sum) && m.GetParameters().Length == 1 && m.ReturnType == typeof(int));
        MethodCallExpression mce = Expression.Call(sumMethod, Expression.Convert(groupingParam, typeof(IEnumerable<int>)));

        Assert.ThrowsAny<Exception>(() => QueryableMemberVisitor.HandleGroupingMethod(sqlVisitor, mce));
    }

    [Fact]
    public void MethodVisitor_HandleIntegerMethod_ParseWithUnresolvableArg_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo parseMethod = typeof(int).GetMethod(nameof(int.Parse), [typeof(string)])!;
        Expression unresolvable = Expression.Default(typeof(string));
        MethodCallExpression mce = Expression.Call(parseMethod, unresolvable);

        Expression result = NumericMemberVisitor.HandleIntegerMethod(new SQLiteCallerContext(sqlVisitor, mce));
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_HandleFloatingPointMethod_ParseWithUnresolvableArg_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo parseMethod = typeof(double).GetMethod(nameof(double.Parse), [typeof(string)])!;
        Expression unresolvable = Expression.Default(typeof(string));
        MethodCallExpression mce = Expression.Call(parseMethod, unresolvable);

        Expression result = NumericMemberVisitor.HandleFloatingPointMethod(new SQLiteCallerContext(sqlVisitor, mce));
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_AggregateExpression_LambdaBodyNotSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression lambdaParam = Expression.Parameter(typeof(int), "x");
        LambdaExpression lambda = Expression.Lambda(Expression.Default(typeof(int)), lambdaParam);

        ParameterExpression source = Expression.Parameter(typeof(IEnumerable<int>), "g");
        MethodInfo sumMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Sum) && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.IsGenericType && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>) && m.ReturnType == typeof(int) && m.GetGenericArguments().Length == 1)
            .MakeGenericMethod(typeof(int));
        MethodCallExpression mce = Expression.Call(sumMethod, source, lambda);

        MethodInfo aggregateMethod = typeof(QueryableMemberVisitor).GetMethod("AggregateExpression", BindingFlags.NonPublic | BindingFlags.Static)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() =>
            aggregateMethod.Invoke(null, [sqlVisitor, mce, "SUM", null, null]));

        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("Sum could not resolve", tie.InnerException!.Message);
    }

    [Fact]
    public void SimpleTranslator_UnresolvableArg_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo dummy = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;
        MethodInfo readLine = typeof(Console).GetMethod(nameof(Console.ReadLine), Type.EmptyTypes)!;
        MethodCallExpression mce = Expression.Call(dummy, Expression.Call(readLine));

        SQLiteMemberTranslator translator = SimpleTranslator.AsSimple((_, _) => "DUMMY");
        SQLiteCallerContext ctx = new(sqlVisitor, mce);
        Expression result = translator(ctx);
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void SimpleTranslator_UnresolvableObj_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo readLine = typeof(Console).GetMethod(nameof(Console.ReadLine), Type.EmptyTypes)!;
        MethodInfo startsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
        MethodCallExpression mce = Expression.Call(Expression.Call(readLine), startsWith, Expression.Constant("abc"));

        SQLiteMemberTranslator translator = SimpleTranslator.AsSimple((_, _) => "DUMMY");
        SQLiteCallerContext ctx = new(sqlVisitor, mce);
        Expression result = translator(ctx);
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_UnwrapPredicateBody_NonLambda_ReturnsExpressionUnchanged()
    {
        ConstantExpression literal = Expression.Constant("not a lambda", typeof(string));

        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("UnwrapPredicateBody", BindingFlags.NonPublic | BindingFlags.Static)!;
        Expression result = (Expression)method.Invoke(null, [literal])!;

        Assert.Same(literal, result);
    }

    [Fact]
    public void MethodVisitor_ResolveEntityAlias_UnsupportedExpression_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        Expression weird = Expression.Default(typeof(int));
        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("ResolveEntityAlias", BindingFlags.NonPublic | BindingFlags.Static)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [sqlVisitor, weird]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("direct entity reference", tie.InnerException!.Message);
    }

    [Fact]
    public void MethodVisitor_ResolveEntityAlias_ParameterWithNonSqlValues_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["NotSql"] = Expression.Constant("plain")
        };

        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("ResolveEntityAlias", BindingFlags.NonPublic | BindingFlags.Static)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [sqlVisitor, pe]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
    }

    [Fact]
    public void MethodVisitor_ResolveEntityAlias_ParameterWithDotlessSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["Id"] = SQLiteExpression.Leaf(typeof(int), 0, "noDots")
        };

        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("ResolveEntityAlias", BindingFlags.NonPublic | BindingFlags.Static)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [sqlVisitor, pe]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
    }

    [Fact]
    public void MethodVisitor_ResolveEntityAlias_MemberWithDotlessSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["Title"] = SQLiteExpression.Leaf(typeof(string), 0, "noDots")
        };
        MemberExpression member = Expression.Property(pe, nameof(Book.Title));

        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("ResolveEntityAlias", BindingFlags.NonPublic | BindingFlags.Static)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [sqlVisitor, member]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_NonMemberArg_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        Expression columnArg = Expression.Constant("Title");
        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Static)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [sqlVisitor, typeof(SQLite.Framework.Tests.Entities.ArticleSearch), columnArg]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("direct property reference", tie.InnerException!.Message);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_UndeclaredColumn_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        MemberExpression nonFtsColumn = Expression.Property(pe, nameof(Book.AuthorId));

        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Static)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [sqlVisitor, typeof(SQLite.Framework.Tests.Entities.ArticleSearch), nonFtsColumn]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("not declared", tie.InnerException!.Message);
    }

    [Fact]
    public void MethodVisitor_HandleEnumMethod_InstanceMethodOtherThanHasFlagOrToString_FallsThrough()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ConstantExpression enumValue = Expression.Constant(DayOfWeek.Monday);
        MethodInfo getTypeMethod = typeof(object).GetMethod(nameof(object.GetType))!;
        MethodCallExpression mce = Expression.Call(enumValue, getTypeMethod);

        Assert.Throws<NotSupportedException>(() => EnumMemberVisitor.HandleEnumMethod(new SQLiteCallerContext(sqlVisitor, mce)));
    }

    [Fact]
    public void MethodVisitor_HandleEnumMethod_NonGenericParseWithUnresolvableString_ReturnsNode()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo parseMethod = typeof(Enum).GetMethods()
            .First(m => m.Name == nameof(Enum.Parse) && m.GetParameters().Length == 2 && !m.IsGenericMethod && m.GetParameters()[0].ParameterType == typeof(Type));

        Expression typeArg = Expression.Constant(typeof(DayOfWeek), typeof(Type));
        Expression unresolvable = Expression.Default(typeof(string));
        MethodCallExpression mce = Expression.Call(parseMethod, typeArg, unresolvable);

        Expression result = EnumMemberVisitor.HandleEnumMethod(new SQLiteCallerContext(sqlVisitor, mce));
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_HandleEnumMethod_NonGenericParseWithoutTypeArg_ReturnsNode()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo parseMethod = typeof(Enum).GetMethods()
            .First(m => m.Name == nameof(Enum.Parse) && m.GetParameters().Length == 2 && !m.IsGenericMethod && m.GetParameters()[0].ParameterType == typeof(Type));

        Expression typeArg = Expression.Constant(null, typeof(Type));
        MethodCallExpression mce = Expression.Call(parseMethod, typeArg, Expression.Constant("X"));

        Expression result = EnumMemberVisitor.HandleEnumMethod(new SQLiteCallerContext(sqlVisitor, mce));
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_HandleFTS5Match_ColumnViaConvert_Works()
    {
        using TestDatabase db = new();
        db.Table<SQLite.Framework.Tests.Entities.Article>().Schema.CreateTable();
        db.Table<SQLite.Framework.Tests.Entities.ArticleSearch>().Schema.CreateTable();

        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(SQLite.Framework.Tests.Entities.ArticleSearch), "a");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["Title"] = SQLiteExpression.Leaf(typeof(string), 0, "a0.Title")
        };

        MemberExpression titleMember = Expression.Property(pe, nameof(SQLite.Framework.Tests.Entities.ArticleSearch.Title));
        UnaryExpression convert = Expression.Convert(titleMember, typeof(string));

        MethodInfo matchMethod = typeof(SQLiteFTS5Functions).GetMethods()
            .First(m => m.Name == nameof(SQLiteFTS5Functions.Match)
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType == typeof(string)
                && m.GetParameters()[1].ParameterType == typeof(string));

        MethodInfo handleMatch = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("HandleFTS5Match", BindingFlags.NonPublic | BindingFlags.Static)!;

        MethodCallExpression mce = Expression.Call(matchMethod, convert, Expression.Constant("hello"));

        Expression? result = (Expression?)handleMatch.Invoke(null, [sqlVisitor, mce]);
        Assert.NotNull(result);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_ConvertWrappedMember_Resolves()
    {
        using TestDatabase db = new();
        db.Table<SQLite.Framework.Tests.Entities.Article>().Schema.CreateTable();
        db.Table<SQLite.Framework.Tests.Entities.ArticleSearch>().Schema.CreateTable();

        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(SQLite.Framework.Tests.Entities.ArticleSearch), "a");
        MemberExpression member = Expression.Property(pe, nameof(SQLite.Framework.Tests.Entities.ArticleSearch.Title));
        UnaryExpression convert = Expression.Convert(member, typeof(string));

        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Static)!;
        int result = (int)method.Invoke(null, [sqlVisitor, typeof(SQLite.Framework.Tests.Entities.ArticleSearch), convert])!;

        Assert.True(result >= 0);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_NeitherMemberNorConvertMember_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        BinaryExpression nonMember = Expression.Add(Expression.Constant(1), Expression.Constant(2));

        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Static)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [sqlVisitor, typeof(SQLite.Framework.Tests.Entities.ArticleSearch), nonMember]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("direct property reference", tie.InnerException!.Message);
    }

    [Fact]
    public void SQLVisitor_VisitUnary_NegateConstant_ReturnsResolvedUnary()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        UnaryExpression negate = Expression.Negate(Expression.Constant(5));

        Expression result = sqlVisitor.Visit(negate);
        Assert.IsAssignableFrom<SQLiteExpression>(result);
    }

    [Fact]
    public void SQLVisitor_VisitUnary_UnsupportedOp_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["Id"] = SQLiteExpression.Leaf(typeof(int), 0, "b0.Id")
        };
        MemberExpression idMember = Expression.Property(pe, nameof(Book.Id));
        UnaryExpression increment = Expression.Increment(idMember);

        Assert.Throws<NotSupportedException>(() => sqlVisitor.Visit(increment));
    }

    [Fact]
    public void SQLVisitor_VisitMethodCall_ObjectEquals_NullSqlObj_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo equalsMethod = typeof(object).GetMethod(nameof(object.Equals), [typeof(object)])!;
        Expression target = Expression.Default(typeof(object));
        MethodCallExpression mce = Expression.Call(target, equalsMethod, Expression.Constant(new object()));

        Expression result = sqlVisitor.Visit(mce);
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void SQLVisitor_ResolveMember_UnregisteredParameter_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["NotSql"] = Expression.Constant("plain")
        };
        MemberExpression member = Expression.Property(pe, nameof(Book.Title));

        Assert.Throws<NotSupportedException>(() => sqlVisitor.ResolveMember(member));
    }

    [Fact]
    public void SQLVisitor_VisitBinary_NonConstantIntCompareToCharCast_AddsConvert()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression charPe = Expression.Parameter(typeof(char), "c");
        sqlVisitor.MethodArguments[charPe] = new Dictionary<string, Expression>
        {
            [""] = SQLiteExpression.Leaf(typeof(char), 0, "b0.Char")
        };

        ParameterExpression intPe = Expression.Parameter(typeof(int), "i");
        sqlVisitor.MethodArguments[intPe] = new Dictionary<string, Expression>
        {
            [""] = SQLiteExpression.Leaf(typeof(int), 0, "b0.Other")
        };

        UnaryExpression intCharCast = Expression.Convert(charPe, typeof(int));
        BinaryExpression eq = Expression.Equal(intPe, intCharCast);

        Expression result = sqlVisitor.Visit(eq);
        Assert.IsAssignableFrom<SQLiteExpression>(result);
    }

    [Fact]
    public void SQLVisitor_VisitBinary_UnsupportedOp_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(double), "x");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            [""] = SQLiteExpression.Leaf(typeof(double), 0, "b0.Price")
        };

        BinaryExpression power = Expression.Power(pe, Expression.Constant(2.0));

        Assert.Throws<NotSupportedException>(() => sqlVisitor.Visit(power));
    }

    [Fact]
    public void SQLVisitor_VisitMember_ConstantNonTable_ReturnsParameter()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        SimpleHolder holder = new() { Value = 42 };
        MemberExpression access = Expression.Property(Expression.Constant(holder), nameof(SimpleHolder.Value));

        Expression result = sqlVisitor.Visit(access);
        SQLiteExpression sql = Assert.IsAssignableFrom<SQLiteExpression>(result);
        Assert.Equal("@p0", sql.ToString());
    }

    [Fact]
    public void SQLVisitor_ResolveExpression_ConvertWrappingEnumConstant_FoldsToNumber()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        UnaryExpression convert = Expression.Convert(Expression.Constant(DayOfWeek.Monday), typeof(int));

        ResolvedModel resolved = sqlVisitor.ResolveExpression(convert);
        Assert.True(resolved.IsConstant);
        Assert.Equal(1, resolved.Constant);
    }

    [Fact]
    public void SQLVisitor_VisitUnary_ConvertOfNonResolvableParameter_ReturnsOperand()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(int), "i");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            [""] = Expression.Constant(42)
        };

        UnaryExpression convert = Expression.Convert(pe, typeof(long));

        Expression result = sqlVisitor.Visit(convert);
        Assert.NotNull(result);
    }

    [Fact]
    public void SQLVisitor_VisitBinary_CharCompareToNonConstantInt_AddsConvert()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        ParameterExpression charPe = Expression.Parameter(typeof(char), "c");
        sqlVisitor.MethodArguments[charPe] = new Dictionary<string, Expression>
        {
            [""] = SQLiteExpression.Leaf(typeof(char), 0, "b0.Char")
        };

        ParameterExpression intPe = Expression.Parameter(typeof(int), "i");
        sqlVisitor.MethodArguments[intPe] = new Dictionary<string, Expression>
        {
            [""] = SQLiteExpression.Leaf(typeof(int), 0, "b0.Other")
        };

        UnaryExpression intCharCast = Expression.Convert(charPe, typeof(int));
        BinaryExpression eq = Expression.Equal(intCharCast, intPe);

        Expression result = sqlVisitor.Visit(eq);
        Assert.IsAssignableFrom<SQLiteExpression>(result);
    }

    [Fact]
    public void SQLVisitor_VisitMemberBinding_CustomBindingType_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        PropertyInfo prop = typeof(Book).GetProperty(nameof(Book.Id))!;
        CustomMemberBinding binding = new(prop);

        MethodInfo method = typeof(SQLVisitor).GetMethod("VisitMemberBinding", BindingFlags.NonPublic | BindingFlags.Instance)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(sqlVisitor, [binding]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("Unsupported binding type", tie.InnerException!.Message);
    }

    [Fact]
    public void SQLVisitor_TryGetMethodTranslator_ConstructedGenericNoMatch_ReturnsFalse()
    {
        using TestDatabase db = new();

        MethodInfo method = typeof(List<int>).GetMethod(nameof(List<int>.Sort), Type.EmptyTypes)!;
        bool result = db.Options.TryGetMethodTranslator(method, out _);
        Assert.False(result);
    }

    [Fact]
    public void PredicateTranslator_InstanceNotResolvable_ReturnsNode()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo method = typeof(InternalHelpersDirectTests)
            .GetMethod(nameof(StaticPredicateHelper), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(int));

        ParameterExpression lambdaParam = Expression.Parameter(typeof(int), "x");
        Expression<Func<int, bool>> predicate = Expression.Lambda<Func<int, bool>>(Expression.Constant(true), lambdaParam);

        Expression instanceExpr = Expression.Default(typeof(IEnumerable<int>));
        MethodCallExpression mce = Expression.Call(method, instanceExpr, predicate);

        SQLiteMemberTranslator translator = SimpleTranslator.AsPredicate((i, p) => $"({i} :: {p})");
        SQLiteCallerContext ctx = new(sqlVisitor, mce);
        Expression result = translator(ctx);

        Assert.Same(mce, result);
    }

    private static int StaticPredicateHelper<T>(IEnumerable<T> source, Func<T, bool> predicate) => 0;

    [Fact]
    public void PredicateTranslator_PredicateNotSql_ReturnsNode()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo method = typeof(InternalHelpersDirectTests)
            .GetMethod(nameof(StaticPredicateHelper), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(int));

        ConstantExpression source = Expression.Constant(new int[] { 1, 2, 3 }, typeof(IEnumerable<int>));
        ParameterExpression lambdaParam = Expression.Parameter(typeof(int), "x");
        Expression<Func<int, bool>> predicate = Expression.Lambda<Func<int, bool>>(
            Expression.Default(typeof(bool)),
            lambdaParam);

        MethodCallExpression mce = Expression.Call(method, source, predicate);

        SQLiteMemberTranslator translator = SimpleTranslator.AsPredicate((i, p) => $"({i} :: {p})");
        SQLiteCallerContext ctx = new(sqlVisitor, mce);
        Expression result = translator(ctx);

        Assert.Same(mce, result);
    }

    [Fact]
    public void SQLVisitor_TranslateProperty_NoTranslatorMatches_ReturnsNull()
    {
        SQLiteOptionsBuilder builder = new($"NoTranslatorMatch_{Guid.NewGuid():N}.db3");
        builder.PropertyTranslators.Add((_, _) => null);
        SQLiteOptions options = builder.Build();
        File.Delete(options.DatabasePath);

        try
        {
            using SQLiteDatabase db = new(options);

            string? result = db.Options.TranslateProperty("SomeMember", "obj.sql");

            Assert.Null(result);
        }
        finally
        {
            for (int i = 0; i < 10 && File.Exists(options.DatabasePath); i++)
            {
                try { File.Delete(options.DatabasePath); break; }
                catch (IOException) { Thread.Sleep(50); }
            }
        }
    }

    [Fact]
    public void SQLVisitor_CoercedResultType_IEnumerableElementMatch_ReturnsSource()
    {
        SQLiteOptionsBuilder builder = new($"CoerceIE_{Guid.NewGuid():N}.db3");
        builder.TypeConverters[typeof(MatchingEnumerable<int>)] = new TestPassThroughConverter();
        SQLiteOptions options = builder.Build();
        File.Delete(options.DatabasePath);

        try
        {
            using SQLiteDatabase db = new(options);

            Type result = db.Options.CoercedResultType(typeof(IList<int>), typeof(MatchingEnumerable<int>));

            Assert.Equal(typeof(MatchingEnumerable<int>), result);
        }
        finally
        {
            for (int i = 0; i < 10 && File.Exists(options.DatabasePath); i++)
            {
                try { File.Delete(options.DatabasePath); break; }
                catch (IOException) { Thread.Sleep(50); }
            }
        }
    }

    [Fact]
    public void SQLiteDatabase_SelectMaterializerHits_DefaultIsZero()
    {
        using TestDatabase db = new();
        Assert.Equal(0, db.SelectMaterializerHits);
    }

    [Fact]
    public void SQLiteDatabase_IncrementSelectMaterializerHits_IncrementsCounter()
    {
        using TestDatabase db = new();
        long before = db.SelectMaterializerHits;

        MethodInfo method = typeof(SQLiteDatabase).GetMethod("IncrementSelectMaterializerHits", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(db, null);

        Assert.Equal(before + 1, db.SelectMaterializerHits);
    }



    [Fact]
    public void SQLiteDatabase_ExecuteGroupingQuery_NonGroupByExpression_Throws()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        IQueryable<IGrouping<int, Book>> wrapped = db.Table<Book>()
            .GroupBy(b => b.AuthorId)
            .Where(g => g.Count() > 0);

        Assert.Throws<NotSupportedException>(() => wrapped.ToList());
    }

    [Fact]
    public void SQLiteDatabase_BackupTo_DestLockedByOtherConnection_HitsBusyRetry()
    {
        string sourcePath = $"BackupBusySrc_{Guid.NewGuid():N}.db3";
        string destPath = $"BackupBusyDest_{Guid.NewGuid():N}.db3";

        try
        {
            using SQLiteDatabase src = new(new SQLiteOptionsBuilder(sourcePath).Build());
            src.Table<Book>().Schema.CreateTable();
            for (int i = 1; i <= 50; i++)
            {
                src.Table<Book>().Add(new Book { Id = i, Title = $"t{i}", AuthorId = 1, Price = i });
            }

            using SQLiteDatabase dest = new(new SQLiteOptionsBuilder(destPath).Build());

            using SQLiteDatabase destLocker = new(new SQLiteOptionsBuilder(destPath).Build());
            destLocker.Table<Book>().Schema.CreateTable();
            destLocker.Execute("PRAGMA busy_timeout = 5000");
            using SQLiteTransaction tx = destLocker.BeginTransaction();
            destLocker.Table<Book>().Add(new Book { Id = 999, Title = "lock", AuthorId = 1, Price = 999 });

            ManualResetEventSlim done = new();
            Exception? backupException = null;

            Thread backupThread = new(() =>
            {
                try { src.BackupTo(dest); }
                catch (Exception ex) { backupException = ex; }
                finally { done.Set(); }
            });

            backupThread.Start();
            Thread.Sleep(300);

            try
            {
                tx.Commit();
            }
            finally
            {
                tx.Dispose();
                Assert.True(done.Wait(TimeSpan.FromSeconds(30)));
                backupThread.Join();
            }

            Assert.Null(backupException);
        }
        finally
        {
            foreach (string path in new[] { sourcePath, destPath })
            {
                for (int i = 0; i < 10 && File.Exists(path); i++)
                {
                    try { File.Delete(path); break; }
                    catch (IOException) { Thread.Sleep(50); }
                }
            }
        }
    }

    [Fact]
    public void SQLiteDatabase_BackupTo_PathWithEncryptionKey_OpensEncryptedDestination()
    {
        string sourcePath = $"BackupSrc_{Guid.NewGuid():N}.db3";
        string destPath = $"BackupDest_{Guid.NewGuid():N}.db3";

        try
        {
            SQLiteOptionsBuilder srcBuilder = new(sourcePath);
            srcBuilder.UseEncryptionKey("test-key");
            using (SQLiteDatabase db = new(srcBuilder.Build()))
            {
                db.Table<Book>().Schema.CreateTable();
                db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });
                db.BackupTo(destPath);
            }

            Assert.True(File.Exists(destPath));
        }
        finally
        {
            foreach (string path in new[] { sourcePath, destPath })
            {
                for (int i = 0; i < 10 && File.Exists(path); i++)
                {
                    try { File.Delete(path); break; }
                    catch (IOException) { Thread.Sleep(50); }
                }
            }
        }
    }

    [Fact]
    public void SQLVisitor_ConvertMemberExpression_UnhandledType_ReturnsSqlExpression()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        SQLiteExpression structSql = SQLiteExpression.Leaf(typeof(SimpleHolder), 0, "h0.Holder");
        MemberExpression member = Expression.Property(structSql, nameof(SimpleHolder.Value));

        MethodInfo method = typeof(SQLVisitor).GetMethod("ConvertMemberExpression", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Expression result = (Expression)method.Invoke(sqlVisitor, [member, structSql])!;

        Assert.Same(structSql, result);
    }

    [Fact]
    public void MethodVisitor_ResolveTrim_UnresolvableTrimChars_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        MethodInfo trim = typeof(string).GetMethod(nameof(string.Trim), [typeof(char[])])!;
        NewArrayExpression arr = Expression.NewArrayInit(typeof(char), Expression.Default(typeof(char)));
        MethodCallExpression mce = Expression.Call(Expression.Constant("hello"), trim, arr);

        SQLiteExpression objSql = SQLiteExpression.Leaf(typeof(string), 0, "\"Title\"");
        ResolvedModel arrArg = new() { IsConstant = false, Constant = null, SQLiteExpression = null, Expression = arr };

        MethodInfo method = typeof(StringMemberVisitor).GetMethod("ResolveTrim", BindingFlags.NonPublic | BindingFlags.Static)!;
        Expression result = (Expression)method.Invoke(null, [sqlVisitor, mce, objSql, new List<ResolvedModel> { arrArg }, "TRIM"])!;

        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_NonFtsEntity_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);

        Expression columnArg = Expression.Constant("Title");
        MethodInfo method = typeof(SQLiteFTS5FunctionsMemberVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Static)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [sqlVisitor, typeof(Book), columnArg]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("FullTextSearch", tie.InnerException!.Message);
    }

    [Fact]
    public void SQLiteTable_DispatchActionWithColumns_InvalidAction_Throws()
    {
        using TestDatabase db = new();
        object table = db.Table<RerouteHookRow>();

        MethodInfo method = table.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == "DispatchAction" && m.GetParameters().Length == 3);

        Dictionary<string, object?> columns = new() { ["Name"] = "x" };
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(table, [(SQLite.Framework.Enums.SQLiteAction)999, new RerouteHookRow(), columns]));
        Assert.IsType<InvalidOperationException>(tie.InnerException);
    }

    [Fact]
    public void CteColumnMapper_BodyColumnNames_CountMismatch_ReturnsNull()
    {
        Dictionary<string, Expression> bodyColumns = new()
        {
            ["A"] = SQLiteExpression.Leaf(typeof(int), 0, "a"),
            ["B"] = SQLiteExpression.Leaf(typeof(int), 1, "b")
        };
        List<SQLiteExpression> selects = [SQLiteExpression.Leaf(typeof(int), 2, "c")];

        string[]? names = CteColumnMapper.BodyColumnNames(bodyColumns, selects);

        Assert.Null(names);
    }

    [Fact]
    public void SQLVisitor_TryResolveConstructedMemberLeaf_DirectlyStoredDottedPath_ReturnsNull()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        ParameterExpression pe = Expression.Parameter(typeof(FoldPathHolder), "y");
        MethodInfo tag = typeof(CmcClientFns).GetMethod(nameof(CmcClientFns.Tag))!;
        visitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["Part.Label"] = Expression.Call(tag, Expression.Constant("v")),
        };

        MemberExpression node = Expression.Property(
            Expression.Property(pe, nameof(FoldPathHolder.Part)),
            nameof(EsfPart.Label));

        Assert.Null(visitor.TryResolveConstructedMemberLeaf(node));
    }

    [Fact]
    public void SQLVisitor_FoldConstructedMemberAccess_MemberInitBinding_ReturnsBoundExpression()
    {
        Expression<Func<FoldConstructedHolder>> lambda = () => new FoldConstructedHolder(7) { B = 8 };
        MethodInfo method = typeof(SQLVisitor).GetMethod("FoldConstructedMemberAccess", BindingFlags.NonPublic | BindingFlags.Static)!;

        Expression bound = (Expression)method.Invoke(null, [lambda.Body, "B"])!;
        Assert.Equal(8, ((ConstantExpression)bound).Value);
    }

    [Fact]
    public void SQLVisitor_FoldConstructedMemberAccess_MemberInitWithoutBinding_FallsToConstructorArgument()
    {
        Expression<Func<FoldConstructedHolder>> lambda = () => new FoldConstructedHolder(7) { B = 8 };
        MethodInfo method = typeof(SQLVisitor).GetMethod("FoldConstructedMemberAccess", BindingFlags.NonPublic | BindingFlags.Static)!;

        Expression bound = (Expression)method.Invoke(null, [lambda.Body, "A"])!;
        Assert.Equal(7, ((ConstantExpression)bound).Value);
    }

    [Fact]
    public void JsonEnumText_TryFormat_WithoutRegisteredTypeInfo_ReturnsFalse()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder($"json-enum-text-null-{Guid.NewGuid():N}.db3").Build();

        bool result = JsonEnumText.TryFormat(options, DayOfWeek.Monday, out string? text);

        Assert.False(result);
        Assert.Null(text);
    }

    [Fact]
    public void JsonValueText_NormalizeInValue_NonJsonSource_ReturnsValueUnchanged()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder($"json-enum-norm-plain-{Guid.NewGuid():N}.db3")
            .AddJsonContext(JselContext.Default)
            .Build();

        object? result = JsonValueText.NormalizeInValue(options, isJsonSource: false, JselState.Active);

        Assert.Equal(JselState.Active, result);
    }

    [Fact]
    public void JsonValueText_NormalizeInValue_JsonSourceStringEnum_ReturnsMemberName()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder($"json-enum-norm-name-{Guid.NewGuid():N}.db3")
            .AddJsonContext(JselContext.Default)
            .Build();

        object? result = JsonValueText.NormalizeInValue(options, isJsonSource: true, JselState.Active);

        Assert.Equal("Active", result);
    }

    [Fact]
    public void JsonValueText_NormalizeInValue_JsonSourceWithoutTypeInfo_ReturnsValueUnchanged()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder($"json-enum-norm-num-{Guid.NewGuid():N}.db3").Build();

        object? result = JsonValueText.NormalizeInValue(options, isJsonSource: true, DayOfWeek.Monday);

        Assert.Equal(DayOfWeek.Monday, result);
    }

    [Fact]
    public void JsonValueText_NormalizeInValue_JsonSourceTemporal_ReturnsJsonText()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder($"json-temporal-norm-{Guid.NewGuid():N}.db3").Build();

        object? result = JsonValueText.NormalizeInValue(options, isJsonSource: true, new DateOnly(2024, 5, 6));

        Assert.Equal("2024-05-06", result);
    }

    [Theory]
    [InlineData("from")]
    [InlineData("into")]
    [InlineData("on")]
    [InlineData("update")]
    [InlineData("join")]
    [InlineData("table")]
    [InlineData("index")]
    [InlineData("trigger")]
    [InlineData("view")]
    [InlineData("references")]
    public void SchemaSqlNormalizer_MainQualifierAfterSchemaKeyword_IsNeutral(string keyword)
    {
        Assert.True(SchemaSqlNormalizer.AreEquivalent($"x {keyword} \"a\"", $"x {keyword} \"main\".\"a\""));
    }

    [Fact]
    public void SchemaSqlNormalizer_MainQualifierAfterOtherWord_StaysDifferent()
    {
        Assert.False(SchemaSqlNormalizer.AreEquivalent("select \"a\"", "select \"main\".\"a\""));
    }

    [Fact]
    public void CteSqlCanonicalizer_SharedIdentifierMap_ReusesExistingEntries()
    {
        Dictionary<string, string> map = new(StringComparer.Ordinal);
        SQLiteExpression first = SQLiteExpression.Leaf(typeof(int), 0, "SELECT h0.\"A\" FROM \"T\" AS h0");
        SQLiteExpression second = SQLiteExpression.Leaf(typeof(int), 1, "SELECT h0.\"A\" FROM \"T\" AS h0");

        string canonicalFirst = CteSqlCanonicalizer.Canonicalize(first, map);
        string canonicalSecond = CteSqlCanonicalizer.Canonicalize(second, map);

        Assert.Equal(canonicalFirst, canonicalSecond);
        Assert.NotEmpty(map);
    }

    [Fact]
    public void SQLVisitor_IsSingleLeafColumn_CaseInsensitiveMatchAfterOtherEntries_ReturnsTrue()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod("IsSingleLeafColumn", BindingFlags.NonPublic | BindingFlags.Static)!;
        Dictionary<string, Expression> columns = new()
        {
            ["Other"] = SQLiteExpression.Leaf(typeof(int), 0, "o"),
            ["TOTAL"] = SQLiteExpression.Leaf(typeof(int), 1, "t"),
        };

        bool result = (bool)method.Invoke(null, [columns, "Total", typeof(DirectGetOnlyHolder)])!;

        Assert.True(result);
    }

    [Fact]
    public void SQLVisitor_IsSingleLeafColumn_CaseInsensitiveMatchOnClientValue_ReturnsFalse()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod("IsSingleLeafColumn", BindingFlags.NonPublic | BindingFlags.Static)!;
        Dictionary<string, Expression> columns = new()
        {
            ["TOTAL"] = Expression.Constant(1),
        };

        bool result = (bool)method.Invoke(null, [columns, "Total", typeof(DirectGetOnlyHolder)])!;

        Assert.False(result);
    }

    [Fact]
    public void SQLVisitor_HasConstructedBase_FindsConditionalBase()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod("HasConstructedBase", BindingFlags.NonPublic | BindingFlags.Static)!;
        Dictionary<string, Expression> columns = new()
        {
            ["A"] = Expression.Condition(Expression.Constant(true), Expression.Constant(1), Expression.Constant(2)),
        };

        Assert.True((bool)method.Invoke(null, [columns, "A.B.C"])!);
    }

    [Fact]
    public void SQLVisitor_HasConstructedBase_NonConditionalBase_ReturnsFalse()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod("HasConstructedBase", BindingFlags.NonPublic | BindingFlags.Static)!;
        Dictionary<string, Expression> columns = new()
        {
            ["A.B"] = Expression.Constant(1),
        };

        Assert.False((bool)method.Invoke(null, [columns, "A.B.C"])!);
    }

    [Fact]
    public void CteColumnMapper_BuildDeclaredBodyLeaf_JsonSourceCarriesTheFlag()
    {
        SQLiteExpression source = SQLiteExpression.Leaf(typeof(DateTime), 0, "j").WithJsonSource();

        SQLiteExpression leaf = CteColumnMapper.BuildDeclaredBodyLeaf(source, "First", "c0", new SQLiteCounters());

        Assert.True(leaf.IsJsonSource);
    }

    [Fact]
    public void CteColumnMapper_BuildDeclaredBodyLeaf_PlainColumnStaysUnflagged()
    {
        SQLiteExpression source = SQLiteExpression.Leaf(typeof(int), 0, "c");

        SQLiteExpression leaf = CteColumnMapper.BuildDeclaredBodyLeaf(source, "Id", "c0", new SQLiteCounters());

        Assert.False(leaf.IsJsonSource);
    }

    [Fact]
    public void CteColumnMapper_BuildDeclaredBodyLeaf_ClientValueStaysUnflagged()
    {
        SQLiteExpression leaf = CteColumnMapper.BuildDeclaredBodyLeaf(Expression.Constant(5), "C", "c0", new SQLiteCounters());

        Assert.False(leaf.IsJsonSource);
    }

    [Fact]
    public void CteSqlCanonicalizer_WithoutSharedMap_RenumbersIdentifiers()
    {
        SQLiteExpression node = SQLiteExpression.Leaf(typeof(int), 0, "SELECT h0.\"A\" FROM \"T\" AS h0");

        string canonical = CteSqlCanonicalizer.Canonicalize(node);

        Assert.Contains("?i0", canonical);
    }

    [Fact]
    public void SQLVisitor_HasConstructedBase_NoBaseEntry_ReturnsFalse()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod("HasConstructedBase", BindingFlags.NonPublic | BindingFlags.Static)!;
        Dictionary<string, Expression> columns = new()
        {
            ["X"] = Expression.Constant(1),
        };

        Assert.False((bool)method.Invoke(null, [columns, "A.B"])!);
    }

    [Fact]
    public void SQLVisitor_UnwrapDecimalCast_SkipsNonMatchingEntriesAndFindsTheSource()
    {
        using TestDatabase db = new(nameof(SQLVisitor_UnwrapDecimalCast_SkipsNonMatchingEntriesAndFindsTheSource));
        SQLVisitor visitor = new(db, new SQLiteCounters(), 0);
        SQLiteExpression other = SQLiteExpression.Leaf(typeof(decimal), 0, "o");
        SQLiteExpression amount = SQLiteExpression.Leaf(typeof(decimal), 1, "a");
        visitor.InternDecimalCast(other);
        SQLiteExpression amountCast = visitor.InternDecimalCast(amount);

        Assert.Same(amount, visitor.UnwrapDecimalCast(amountCast));
        Assert.Same(other, visitor.UnwrapDecimalCast(other));
    }

    [Fact]
    public void QueryCompilerVisitor_TypeAsOverNull_ReturnsNull()
    {
        UnaryExpression node = Expression.TypeAs(Expression.Constant(null, typeof(object)), typeof(string));
        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        Assert.Null(compiled.Call(new SQLiteQueryContext()));
    }

    [Fact]
    public void QueryCompilerVisitor_PlainConvertOverNull_ReturnsNull()
    {
        UnaryExpression node = Expression.Convert(Expression.Constant(null, typeof(int?)), typeof(long?));
        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        Assert.Null(compiled.Call(new SQLiteQueryContext()));
    }

    [Fact]
    public void QueryCompilerVisitor_ConvertChecked_WithConversionOperator_InvokesIt()
    {
        MethodInfo conversion = typeof(H24lMoney).GetMethod("op_Explicit")!;
        UnaryExpression node = Expression.ConvertChecked(Expression.Constant(new H24lMoney(250)), typeof(int), conversion);
        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        Assert.Equal(2, compiled.Call(new SQLiteQueryContext()));
    }

    [Fact]
    public void QueryCompilerVisitor_ConvertChecked_WithConversionOperatorOverNull_ReturnsNull()
    {
        MethodInfo conversion = typeof(H24lMoney).GetMethod("op_Explicit")!;
        UnaryExpression node = Expression.ConvertChecked(
            Expression.Constant(null, typeof(H24lMoney?)), typeof(int?), conversion);
        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        Assert.Null(compiled.Call(new SQLiteQueryContext()));
    }

    [Fact]
    public void QueryableVisitor_ContainsListBinding_NestedAssignment_ReturnsTrue()
    {
        MethodInfo method = typeof(SQLite.Framework.Internals.Visitors.Queryable.QueryableVisitor)
            .GetMethod("ContainsListBinding", BindingFlags.NonPublic | BindingFlags.Static)!;
        PropertyInfo holderProperty = typeof(DirectListHolder).GetProperty(nameof(DirectListHolder.Inner))!;
        PropertyInfo listProperty = typeof(DirectListInner).GetProperty(nameof(DirectListInner.Items))!;
        MethodInfo add = typeof(List<int>).GetMethod(nameof(List<int>.Add))!;
        MemberInitExpression inner = Expression.MemberInit(
            Expression.New(typeof(DirectListInner)),
            Expression.ListBind(listProperty, Expression.ElementInit(add, Expression.Constant(1))));
        MemberInitExpression body = Expression.MemberInit(
            Expression.New(typeof(DirectListHolder)),
            Expression.Bind(holderProperty, inner));

        Assert.True((bool)method.Invoke(null, [body])!);
    }

    [Fact]
    public void SetOperationAlignment_NumericOperand_IsSkipped()
    {
        MethodInfo method = Type.GetType("SQLite.Framework.Internals.Helpers.SetOperationAlignment, SQLite.Framework")!
            .GetMethod("ThrowIfBranchMembersMisaligned", BindingFlags.Public | BindingFlags.Static)!;
        List<string> main = ["Id", "Name"];
        List<IReadOnlyList<string>> operands = [new List<string> { "7", "8" }];

        Exception? ex = Record.Exception(() => method.Invoke(null, [false, main, operands]));

        Assert.Null(ex);
    }

    [Fact]
    public void SetOperationAlignment_NoOperands_IsSkipped()
    {
        MethodInfo method = Type.GetType("SQLite.Framework.Internals.Helpers.SetOperationAlignment, SQLite.Framework")!
            .GetMethod("ThrowIfBranchMembersMisaligned", BindingFlags.Public | BindingFlags.Static)!;
        List<string> main = ["Id", "Name"];
        List<IReadOnlyList<string>> operands = [];

        Exception? ex = Record.Exception(() => method.Invoke(null, [false, main, operands]));

        Assert.Null(ex);
    }

    [Fact]
    public void SetOperationAlignment_EmptyMainIdentifiers_IsSkipped()
    {
        MethodInfo method = Type.GetType("SQLite.Framework.Internals.Helpers.SetOperationAlignment, SQLite.Framework")!
            .GetMethod("ThrowIfBranchMembersMisaligned", BindingFlags.Public | BindingFlags.Static)!;
        List<string> main = [];
        List<IReadOnlyList<string>> operands = [new List<string> { "Id", "Name" }];

        Exception? ex = Record.Exception(() => method.Invoke(null, [false, main, operands]));

        Assert.Null(ex);
    }

    [Fact]
    public void CommandHelpers_DoubleToInt64_NaN_IsZero()
    {
        MethodInfo method = typeof(CommandHelpers).GetMethod("DoubleToInt64", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.Equal(0L, method.Invoke(null, [double.NaN]));
    }

    [Fact]
    public void CommandHelpers_TextToStoredDouble_ParsesSignAndExponentPrefixes()
    {
        MethodInfo method = typeof(CommandHelpers).GetMethod("TextToStoredDouble", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.Equal(7d, method.Invoke(null, ["+7"]));
        Assert.Equal(-2.5d, method.Invoke(null, ["-2.5"]));
        Assert.Equal(300d, method.Invoke(null, ["3e2"]));
        Assert.Equal(40d, method.Invoke(null, ["4E+1"]));
        Assert.Equal(0.5d, method.Invoke(null, ["5e-1"]));
        Assert.Equal(6d, method.Invoke(null, ["6e"]));
        Assert.Equal(7d, method.Invoke(null, ["7e+"]));
        Assert.Equal(8d, method.Invoke(null, ["8eX"]));
        Assert.Equal(0d, method.Invoke(null, ["abc"]));
        Assert.Equal(0d, method.Invoke(null, [""]));
    }

#if !SQLITECIPHER
    [Fact]
    public void BareSqlTranslator_ConverterReadsDisabled_EmitsTheRawColumnForSQLiteColumnOf()
    {
        using TestDatabase db = new(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
        TableMapping mapping = db.Table<DirectJsonbColumnRow>().Table;

        ParameterExpression row = Expression.Parameter(typeof(object), "row");
        MethodCallExpression body = Expression.Call(
            typeof(SQLiteColumn),
            nameof(SQLiteColumn.Of),
            [typeof(Address)],
            row,
            Expression.Constant("Data"));
        LambdaExpression lambda = Expression.Lambda(body, row);

        string wrapped = BareSqlTranslator.Translate(db, mapping, lambda, wrapConverterReads: true);
        string raw = BareSqlTranslator.Translate(db, mapping, lambda, wrapConverterReads: false);

        Assert.Equal("json(\"Data\")", wrapped);
        Assert.Equal("\"Data\"", raw);
    }
#endif

    [Fact]
    public void SetOperationAlignment_SingleNamedMember_ThrowsOnlyForByNameMaterialization()
    {
        MethodInfo method = Type.GetType("SQLite.Framework.Internals.Helpers.SetOperationAlignment, SQLite.Framework")!
            .GetMethod("ThrowIfBranchMembersMisaligned", BindingFlags.Public | BindingFlags.Static)!;
        List<string> main = ["Left"];
        List<IReadOnlyList<string>> operands = [new List<string> { "Right" }];

        Exception? scalar = Record.Exception(() => method.Invoke(null, [false, main, operands]));
        TargetInvocationException byName = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, [true, main, operands]));

        Assert.Null(scalar);
        Assert.IsType<NotSupportedException>(byName.InnerException);
    }

    [Fact]
    public void SQLVisitor_VisitMember_UnsetConstructedMember_ReadsTheDefaultValue()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new SQLiteCounters(), 0);
        ParameterExpression holder = Expression.Parameter(typeof(DirectUnsetHolder), "x");
        Dictionary<string, Expression> expressions = new()
        {
            ["P.Value"] = SQLiteExpression.Leaf(typeof(int), 0, "b0.A")
        };
        sqlVisitor.MethodArguments[holder] = expressions;
        sqlVisitor.ConstructedProjectionPaths[expressions] = new HashSet<string>(StringComparer.Ordinal) { "P" };
        MemberExpression node = Expression.Property(Expression.Property(holder, nameof(DirectUnsetHolder.P)), nameof(DirectUnsetPart.Untouched));

        Expression resolved = sqlVisitor.Visit(node);

        SQLiteExpression sql = Assert.IsAssignableFrom<SQLiteExpression>(resolved);
        Assert.Equal(0, Assert.Single(sql.Parameters!).Value);
    }

    [Fact]
    public void SQLTranslator_InnerQueryWithSetOperations_SkipsTheAlignmentGuard()
    {
        using TestDatabase db = new();
        SQLTranslator translator = new(db, new SQLiteCounters(), 1, true);
        IQueryable<int> query = db.Table<Book>().Select(b => b.Id)
            .Concat(db.Table<Book>().Select(b => b.AuthorId));

        SQLQuery translated = translator.Translate(query.Expression);

        Assert.Contains("UNION ALL", translated.Sql);
    }

    [Fact]
    public void CteColumnMapper_BodyColumnOrderIsAmbiguous_LeafCountMismatch_IsFalse()
    {
        Dictionary<string, Expression> bodyColumns = new()
        {
            ["A"] = Expression.Constant(1)
        };
        List<SQLiteExpression> selects = [SQLiteExpression.Leaf(typeof(int), 0, "c0.\"A\"")];

        Assert.False(CteColumnMapper.BodyColumnOrderIsAmbiguous(bodyColumns, selects));
    }

    [Fact]
    public void QueryFilterInjector_TryResolveOwnedTable_NonConstantReceiver_ReturnsNull()
    {
        MethodInfo method = Type.GetType("SQLite.Framework.Internals.Visitors.QueryFilterInjectorVisitor, SQLite.Framework")!
            .GetMethod("TryResolveOwnedTable", BindingFlags.NonPublic | BindingFlags.Static)!;
        ParameterExpression repo = Expression.Parameter(typeof(DirectFilterRepository), "repo");
        MethodCallExpression node = Expression.Call(repo, typeof(DirectFilterRepository).GetMethod(nameof(DirectFilterRepository.Books))!);

        Assert.Null(method.Invoke(null, [node]));
    }

    [Fact]
    public void QueryFilterInjector_TryResolveOwnedTable_NullReceiver_ReturnsNull()
    {
        MethodInfo method = Type.GetType("SQLite.Framework.Internals.Visitors.QueryFilterInjectorVisitor, SQLite.Framework")!
            .GetMethod("TryResolveOwnedTable", BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodCallExpression node = Expression.Call(
            Expression.Constant(null, typeof(DirectFilterRepository)),
            typeof(DirectFilterRepository).GetMethod(nameof(DirectFilterRepository.Books))!);

        Assert.Null(method.Invoke(null, [node]));
    }

    [Fact]
    public void QueryFilterInjector_TryResolveOwnedTable_NonConstantArgument_ReturnsNull()
    {
        using TestDatabase db = new();
        MethodInfo method = Type.GetType("SQLite.Framework.Internals.Visitors.QueryFilterInjectorVisitor, SQLite.Framework")!
            .GetMethod("TryResolveOwnedTable", BindingFlags.NonPublic | BindingFlags.Static)!;
        ParameterExpression flag = Expression.Parameter(typeof(bool), "flag");
        MethodCallExpression node = Expression.Call(
            Expression.Constant(new DirectFilterRepository(db)),
            typeof(DirectFilterRepository).GetMethod(nameof(DirectFilterRepository.BooksIf))!,
            flag);

        Assert.Null(method.Invoke(null, [node]));
    }

    [Fact]
    public void QueryFilterInjector_TryResolveOwnedTable_ConstantArgument_ReturnsTheTable()
    {
        using TestDatabase db = new();
        MethodInfo method = Type.GetType("SQLite.Framework.Internals.Visitors.QueryFilterInjectorVisitor, SQLite.Framework")!
            .GetMethod("TryResolveOwnedTable", BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodCallExpression node = Expression.Call(
            Expression.Constant(new DirectFilterRepository(db)),
            typeof(DirectFilterRepository).GetMethod(nameof(DirectFilterRepository.BooksIf))!,
            Expression.Constant(true));

        object? resolved = method.Invoke(null, [node]);

        Assert.IsType<SQLiteTable<Book>>(resolved);
    }

    [Fact]
    public void QueryFilterInjector_ResolveOwnerOptions_UnresolvableCall_FallsBackToTheQueryOptions()
    {
        using TestDatabase db = new();
        Type visitorType = Type.GetType("SQLite.Framework.Internals.Visitors.QueryFilterInjectorVisitor, SQLite.Framework")!;
        object visitor = Activator.CreateInstance(visitorType, db.Options, false)!;
        MethodInfo method = visitorType.GetMethod("ResolveOwnerOptions", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ParameterExpression repo = Expression.Parameter(typeof(DirectFilterRepository), "repo");
        MethodCallExpression node = Expression.Call(repo, typeof(DirectFilterRepository).GetMethod(nameof(DirectFilterRepository.Books))!);

        Assert.Same(db.Options, method.Invoke(visitor, [node]));
    }

    [Fact]
    public void QueryableVisitor_ResolveGroupKeyMemberNames_StructWithoutConstructor_ReturnsNull()
    {
        MethodInfo method = typeof(SQLite.Framework.Internals.Visitors.Queryable.QueryableVisitor)
            .GetMethod("ResolveGroupKeyMemberNames", BindingFlags.NonPublic | BindingFlags.Static)!;
        NewExpression keyNew = Expression.New(typeof(DirectEmptyStructKey));

        Assert.Null(method.Invoke(null, [keyNew]));
    }

    [Fact]
    public void QueryableVisitor_ResolveGroupKeyMemberNames_ParameterlessConstructor_ReturnsNull()
    {
        MethodInfo method = typeof(SQLite.Framework.Internals.Visitors.Queryable.QueryableVisitor)
            .GetMethod("ResolveGroupKeyMemberNames", BindingFlags.NonPublic | BindingFlags.Static)!;
        NewExpression keyNew = Expression.New(typeof(DirectBindingInner));

        Assert.Null(method.Invoke(null, [keyNew]));
    }

    [Fact]
    public void QueryableVisitor_ContainsListBinding_MemberMemberBinding_ReturnsFalse()
    {
        MethodInfo method = typeof(SQLite.Framework.Internals.Visitors.Queryable.QueryableVisitor)
            .GetMethod("ContainsListBinding", BindingFlags.NonPublic | BindingFlags.Static)!;
        PropertyInfo holderProperty = typeof(DirectBindingHolder).GetProperty(nameof(DirectBindingHolder.Inner))!;
        PropertyInfo valueProperty = typeof(DirectBindingInner).GetProperty(nameof(DirectBindingInner.Value))!;
        MemberInitExpression body = Expression.MemberInit(
            Expression.New(typeof(DirectBindingHolder)),
            Expression.MemberBind(holderProperty, Expression.Bind(valueProperty, Expression.Constant(1))));

        Assert.False((bool)method.Invoke(null, [body])!);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitBinary_CoalesceWithConversion_Throws()
    {
        ParameterExpression conversionParam = Expression.Parameter(typeof(DirectConvertibleValue), "v");
        BinaryExpression node = Expression.Coalesce(
            Expression.Constant(new DirectConvertibleValue { Value = 9 }, typeof(DirectConvertibleValue?)),
            Expression.Constant(5L),
            Expression.Lambda(Expression.Convert(conversionParam, typeof(long)), conversionParam));

        QueryCompilerVisitor visitor = new(CompilerOptions);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        Assert.Throws<NotSupportedException>(() => compiled.Call(ctx));
    }
}

public struct DirectConvertibleValue
{
    public long Value { get; set; }

    public static implicit operator long(DirectConvertibleValue value)
    {
        return value.Value;
    }
}

public class DirectListInner
{
    public List<int> Items { get; set; } = [];
}

public class DirectListHolder
{
    public DirectListInner Inner { get; set; } = new();
}

public struct DirectEmptyStructKey
{
    public int Value { get; set; }
}

public class DirectBindingInner
{
    public int Value { get; set; }
}

public class DirectBindingHolder
{
    public DirectBindingInner Inner { get; set; } = new();
}

public class DirectGetOnlyHolder
{
    public DirectGetOnlyHolder(int total)
    {
        Total = total;
    }

    public int Total { get; }
}

public class InternalHelpersOneArg
{
    public InternalHelpersOneArg(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

public interface IRebindFoo
{
    string Tag { get; }
}

public class RebindEntityWithExplicitImpl : IRebindFoo
{
    string IRebindFoo.Tag => "explicit";
}

public class CompilerVisitorOuter
{
    public CompilerVisitorInner Inner { get; } = new();
}

public class CompilerVisitorInner
{
    public int X { get; set; }
}

public class CompilerVisitorFieldHolder
{
    public int FieldX;
    public List<int> ListField = new();
}

public readonly struct CompilerVisitorNegatable
{
    public int Value { get; }
    public CompilerVisitorNegatable(int value) => Value = value;
    public static CompilerVisitorNegatable operator -(CompilerVisitorNegatable v) => new(-v.Value);
}

public class CompilerVisitorListContainer
{
    public CompilerVisitorInnerWithList Inner { get; } = new();
}

public class CompilerVisitorInnerWithList
{
    public List<int> Items { get; } = new();
}

internal sealed class NullDeclaringTypeMethodInfo : MethodInfo
{
    public override Type? DeclaringType => null;
    public override string Name => "Dummy";
    public override Type? ReflectedType => null;
    public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();
    public override MethodAttributes Attributes => MethodAttributes.Public | MethodAttributes.Static;
    public override CallingConventions CallingConvention => CallingConventions.Standard;
    public override MethodInfo GetBaseDefinition() => this;
    public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.IL;
    public override ParameterInfo[] GetParameters() => [];
    public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) => null;
    public override Type ReturnType => typeof(void);
    public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotSupportedException();
    public override object[] GetCustomAttributes(bool inherit) => [];
    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];
    public override bool IsDefined(Type attributeType, bool inherit) => false;
}

public class InternalHelpersTestEntity
{
    public event EventHandler? SomethingHappened
    {
        add { _ = value; }
        remove { _ = value; }
    }

    public int Value { get; set; }
}

public class SimpleHolder
{
    public int Value { get; set; }
}

public class HandlerDispatchTests
{
    private static SQLVisitor GetVisitor(TestDatabase db)
    {
        return new SQLVisitor(db, new SQLiteCounters(), 0);
    }

    private static MethodCallExpression UnknownNamedCall()
    {
        MethodInfo method = typeof(object).GetMethod(nameof(object.GetType))!;
        return Expression.Call(Expression.Constant(new object()), method);
    }

    [Fact]
    public void HandleSQLiteFTS5FunctionsMethod_UnknownName_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor v = GetVisitor(db);
        MethodCallExpression mce = UnknownNamedCall();
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => SQLiteFTS5FunctionsMemberVisitor.HandleSQLiteFTS5FunctionsMethod(new SQLiteCallerContext(v, mce)));
        Assert.Contains("SQLiteFTS5Functions.GetType", ex.Message);
    }

    [Fact]
    public void HandleSQLiteJsonFunctionsMethod_UnknownName_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor v = GetVisitor(db);
        MethodCallExpression mce = UnknownNamedCall();
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => SQLiteJsonFunctionsMemberVisitor.HandleSQLiteJsonFunctionsMethod(new SQLiteCallerContext(v, mce)));
        Assert.Contains("SQLiteJsonFunctions.GetType", ex.Message);
    }

    [Fact]
    public void HandleSQLiteDateFunctionsMethod_UnknownName_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor v = GetVisitor(db);
        MethodCallExpression mce = UnknownNamedCall();
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => SQLiteDateFunctionsMemberVisitor.HandleSQLiteDateFunctionsMethod(new SQLiteCallerContext(v, mce)));
        Assert.Contains("SQLiteDateFunctions.GetType", ex.Message);
    }

    [Fact]
    public void HandleWindowFunction_UnknownName_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor v = GetVisitor(db);
        MethodCallExpression mce = UnknownNamedCall();
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => WindowFunctionsMemberVisitor.HandleWindowFunctionMethod(new SQLiteCallerContext(v, mce)));
        Assert.Contains("Object.GetType", ex.Message);
    }

    [Fact]
    public void HandleFrameBoundary_UnknownName_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor v = GetVisitor(db);
        MethodCallExpression mce = UnknownNamedCall();
        MethodInfo handleFrameBoundary = typeof(WindowFunctionsMemberVisitor).GetMethod("HandleFrameBoundary", BindingFlags.NonPublic | BindingFlags.Static)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => handleFrameBoundary.Invoke(null, [v, mce]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("SQLiteFrameBoundary.GetType", tie.InnerException!.Message);
    }

    [Fact]
    public void HandleCustomMethod_InstanceMethod_PrependsObjectSqlExpression()
    {
        TestDatabase db = new(b =>
        {
            b.AddTypeConverter<System.Numerics.BigInteger>(new BigIntegerConverterTests.BigIntegerConverter());
            MethodInfo compareTo = typeof(System.Numerics.BigInteger).GetMethod(
                nameof(System.Numerics.BigInteger.CompareTo),
                [typeof(System.Numerics.BigInteger)])!;
            b.MemberTranslators[compareTo] = SimpleTranslator.AsSimple((instance, args) => $"FAKE_CMP({instance}, {args[0]})");
        });
        try
        {
            db.Table<BigIntegerConverterTests.BigEntity>().Schema.CreateTable();
            System.Numerics.BigInteger value = System.Numerics.BigInteger.Parse("42");
            db.Table<BigIntegerConverterTests.BigEntity>().Add(new BigIntegerConverterTests.BigEntity { Id = 1, Value = value });

            SQLiteCommand command = db.Table<BigIntegerConverterTests.BigEntity>()
                .Where(e => e.Value.CompareTo(value) > 0)
                .ToSqlCommand();

            Assert.Equal("""
                         SELECT b0."Id" AS "Id",
                                b0."Value" AS "Value"
                         FROM "BigEntity" AS b0
                         WHERE FAKE_CMP(b0."Value", @p0) > @p1
                         """.Replace("\r\n", "\n"),
                command.CommandText.Replace("\r\n", "\n"));
        }
        finally
        {
            db.Dispose();
        }
    }

    [Fact]
    public void InlineParameterBuffer8_OverflowPath_PreservesAllParameters()
    {
        SQLite.Framework.Internals.Helpers.InlineParameterBuffer8 buffer = default;

        SQLiteParameter[] inputs = new SQLiteParameter[12];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = new SQLiteParameter { Name = $"@p{i}", Value = i };
        }

        buffer.AddRange(inputs);

        Assert.Equal(12, buffer.Count);
        SQLiteParameter[] result = buffer.ToArray();
        Assert.Equal(12, result.Length);
        for (int i = 0; i < inputs.Length; i++)
        {
            Assert.Same(inputs[i], result[i]);
        }
    }

    [Fact]
    public void SQLVisitor_InternDecimalCast_SameSource_ReturnsSameInstance()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLite.Framework.Models.SQLiteCounters(), level: 0);

        SQLiteExpression source = SQLiteExpression.Leaf(typeof(decimal), -1, "t0.Price", (SQLiteParameter[]?)null);

        SQLiteExpression first = visitor.InternDecimalCast(source);
        SQLiteExpression second = visitor.InternDecimalCast(source);

        Assert.Same(first, second);
        Assert.Equal("CAST(t0.Price AS REAL)", first.ToString());
    }

    [Fact]
    public void Grouping_NonGenericGetEnumerator_ReturnsSameSequence()
    {
        SQLite.Framework.Internals.Models.Grouping<int, string> grouping = new(1, ["a", "b"]);
        System.Collections.IEnumerable seq = grouping;

        List<object?> items = [];
        foreach (object? item in seq)
        {
            items.Add(item);
        }

        Assert.Equal(["a", "b"], items);
    }

    [Fact]
    public void CompiledExpression_NodeType_IsCall()
    {
        SQLite.Framework.Internals.Models.CompiledExpression expr = new(typeof(int), _ => 5);

        Assert.Equal(ExpressionType.Call, expr.NodeType);
        Assert.Equal(typeof(int), expr.Type);
    }

    [Fact]
    public void SQLiteCallerContext_ExposesVisitorState()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLite.Framework.Models.SQLiteCounters(), level: 3);
        ConstantExpression node = Expression.Constant(0);

        SQLiteCallerContext ctx = new(visitor, node);

        Assert.Equal(3, ctx.Level);
        Assert.Equal(visitor.IsInSelectProjection, ctx.IsInSelectProjection);
        Assert.Same(visitor.From, ctx.From);
        Assert.Same(visitor.TableColumns, ctx.TableColumns);
    }

    [Fact]
    public void SQLVisitor_InternJsonExtract_SameSourceAndMember_ReturnsSameInstance()
    {
        using TestDatabase db = new();
        SQLVisitor visitor = new(db, new SQLite.Framework.Models.SQLiteCounters(), level: 0);

        SQLiteExpression source = SQLiteExpression.Leaf(typeof(string), -1, "t0.Address", (SQLiteParameter[]?)null);

        SQLiteExpression first = visitor.InternJsonExtract(source, "City", typeof(string));
        SQLiteExpression second = visitor.InternJsonExtract(source, "City", typeof(string));
        SQLiteExpression different = visitor.InternJsonExtract(source, "Street", typeof(string));

        Assert.Same(first, second);
        Assert.NotSame(first, different);
        Assert.Equal("json_extract(t0.Address, '$.City')", first.ToString());
        Assert.True(first.IsJsonSource);
    }

    [Fact]
    public void ColumnBinderFactory_ValueTypeEntity_ReturnsNull()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").Build();
        TableColumn column = new(typeof(BinderStructRow).GetProperty(nameof(BinderStructRow.Value))!, options);

        Assert.Null(ColumnBinderFactory.TryCreate<BinderStructRow>(column, 1, options));
    }

    [Fact]
    public void ColumnBinderFactory_SetOnlyProperty_ReturnsNull()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").Build();
        TableColumn column = new(typeof(BinderPropertyRow).GetProperty(nameof(BinderPropertyRow.SetOnly))!, options);

        Assert.Null(ColumnBinderFactory.TryCreate<BinderPropertyRow>(column, 1, options));
    }

    [Fact]
    public void ColumnBinderFactory_StaticProperty_ReturnsNull()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").Build();
        TableColumn column = new(typeof(BinderPropertyRow).GetProperty(nameof(BinderPropertyRow.Marker))!, options);

        Assert.Null(ColumnBinderFactory.TryCreate<BinderPropertyRow>(column, 1, options));
    }

    public struct BinderStructRow
    {
        public int Value { get; set; }
    }

    public class BinderPropertyRow
    {
        public int SetOnly
        {
            set
            {
            }
        }

        public static int Marker { get; set; }
    }
}

internal sealed class CustomMemberBinding : MemberBinding
{
    public CustomMemberBinding(MemberInfo member)
        : base((MemberBindingType)999, member)
    {
    }
}

internal sealed class TestPassThroughConverter : SQLite.Framework.ISQLiteTypeConverter
{
    public SQLite.Framework.Enums.SQLiteColumnType ColumnType => SQLite.Framework.Enums.SQLiteColumnType.Text;
    public object? ToDatabase(object? value) => value;
    public object? FromDatabase(object? value) => value;
}

public sealed class MatchingEnumerable<T> : IEnumerable<T>
{
    public IEnumerator<T> GetEnumerator() => System.Linq.Enumerable.Empty<T>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class NoArgWithMembersHolder
{
    public int Value { get; set; }
}

public sealed class FoldConstructedHolder
{
    public FoldConstructedHolder(int a)
    {
        A = a;
    }

    public int A { get; }

    public int B { get; set; }
}

public sealed class FoldPathHolder
{
    public EsfPart? Part { get; set; }
}

public sealed class CompilerVisitorHolderOwner
{
    public CompilerVisitorFieldHolder Holder { get; set; } = new();
}

public struct ConvOperatorSource
{
    public int V;
}

public struct ConvOperatorTarget
{
    public int W;
}

public sealed class ConvOperatorBox;

public static class ConvOperatorMethods
{
    public static ConvOperatorTarget ToTarget(ConvOperatorSource source)
    {
        return new ConvOperatorTarget { W = source.V };
    }

    public static ConvOperatorBox ToBox(ConvOperatorSource source)
    {
        return new ConvOperatorBox();
    }
}

public class DirectFilterRepository
{
    private readonly SQLiteDatabase database;

    public DirectFilterRepository(SQLiteDatabase database)
    {
        this.database = database;
    }

    public SQLiteTable<Book> Books()
    {
        return database.Table<Book>();
    }

    public SQLiteTable<Book> BooksIf(bool include)
    {
        return database.Table<Book>();
    }
}

public class DirectUnsetPart
{
    public DirectUnsetPart(int value)
    {
        Value = value;
    }

    public int Value { get; set; }

    public int Untouched { get; set; }
}

public class DirectUnsetHolder
{
    public DirectUnsetPart? P { get; set; }
}

[Table("DirectJsonbColumnRows")]
public class DirectJsonbColumnRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}
