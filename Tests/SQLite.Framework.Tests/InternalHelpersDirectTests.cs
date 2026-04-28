using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;
using QueryCompilerVisitor = SQLite.Framework.Internals.Visitors.QueryCompilerVisitor;
using QueryFilterRebinder = SQLite.Framework.Internals.Visitors.QueryFilterRebinder;
using CommandHelpers = SQLite.Framework.Internals.Helpers.CommandHelpers;
using CommonHelpers = SQLite.Framework.Internals.Helpers.CommonHelpers;
using FtsRenderState = SQLite.Framework.Internals.Helpers.FtsRenderState;
using UpsertSqlBuilder = SQLite.Framework.Internals.Helpers.UpsertSqlBuilder;

namespace SQLite.Framework.Tests;

public class InternalHelpersDirectTests
{
    [Fact]
    public void CommandHelpers_ReadColumnValue_UnknownColumnType_Throws()
    {
        using TestDatabase db = new();
        SQLiteCommand cmd = db.CreateCommand("SELECT 1", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();
        reader.Read();

        Assert.Throws<NotSupportedException>(() =>
            CommandHelpers.ReadColumnValue(reader.Statement, 0, (SQLiteColumnType)999, typeof(int), db.Options));
    }

    [Fact]
    public void FtsRenderState_WriteFts5Call_UnknownMethodName_Throws()
    {
        using TestDatabase db = new();
        SQLite.Framework.Internals.Visitors.SQLVisitor visitor = new(
            db,
            new SQLite.Framework.Internals.Models.IndexWrapper(),
            new SQLite.Framework.Internals.Models.IndexWrapper(),
            new SQLite.Framework.Internals.Models.TableIndexWrapper(),
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
        Assert.Throws<InvalidOperationException>(() => CommonHelpers.GetConstantValue(ne));
    }

    [Fact]
    public void CommonHelpers_CreateMember_TypeWithoutInstance_Throws()
    {
        MemberInitExpression mie = Expression.MemberInit(
            Expression.New(typeof(int?)),
            Array.Empty<MemberBinding>());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => CommonHelpers.GetConstantValue(mie));
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
            () => CommonHelpers.GetConstantValue(mie));
        Assert.Contains("not found in type", ex.Message);
    }

    [Fact]
    public void UpsertSqlBuilder_Build_DoUpdateReferencesUnknownProperty_Throws()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping<Book>();

        UpsertConflictTarget<Book> target = ConstructConflictTarget<Book>(
            new[] { "Id" },
            ConstructDoUpdateAction<Book>(new[] { "DoesNotExist" }));

        Assert.Throws<InvalidOperationException>(() =>
            UpsertSqlBuilder.Build(mapping, target, (_, name) => name));
    }

    [Fact]
    public void UpsertSqlBuilder_Build_UnknownActionKind_Throws()
    {
        using TestDatabase db = new();
        TableMapping mapping = db.TableMapping<Book>();

        UpsertConflictTarget<Book> target = ConstructConflictTarget<Book>(
            new[] { "Id" },
            ConstructActionWithKind<Book>(999));

        Assert.Throws<InvalidOperationException>(() =>
            UpsertSqlBuilder.Build(mapping, target, (_, name) => name));
    }

    private static UpsertConflictTarget<T> ConstructConflictTarget<T>(IReadOnlyList<string> conflictColumns, UpsertAction<T> action)
    {
        ConstructorInfo ctor = typeof(UpsertConflictTarget<T>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(IReadOnlyList<string>) },
            modifiers: null)!;
        UpsertConflictTarget<T> target = (UpsertConflictTarget<T>)ctor.Invoke(new object[] { conflictColumns });

        FieldInfo actionField = typeof(UpsertConflictTarget<T>).GetField("action", BindingFlags.Instance | BindingFlags.NonPublic)!;
        actionField.SetValue(target, action);
        return target;
    }

    private static UpsertAction<T> ConstructDoUpdateAction<T>(IReadOnlyList<string> columns)
    {
        ConstructorInfo ctor = typeof(UpsertAction<T>).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        Type kindType = typeof(UpsertAction<T>).Assembly.GetType("SQLite.Framework.Internals.Enums.UpsertActionKind", throwOnError: true)!;
        object doUpdateKind = Enum.ToObject(kindType, 1);
        return (UpsertAction<T>)ctor.Invoke(new object?[] { doUpdateKind, columns });
    }

    private static UpsertAction<T> ConstructActionWithKind<T>(int kindValue)
    {
        ConstructorInfo ctor = typeof(UpsertAction<T>).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        Type kindType = typeof(UpsertAction<T>).Assembly.GetType("SQLite.Framework.Internals.Enums.UpsertActionKind", throwOnError: true)!;
        object kind = Enum.ToObject(kindType, kindValue);
        return (UpsertAction<T>)ctor.Invoke(new object?[] { kind, null });
    }

    [Fact]
    public void AliasVisitor_ConstructorParameterCountMismatch_Throws()
    {
        using TestDatabase db = new();

        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);
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
    public void QueryFilterRebinder_ConcreteMemberNotFound_FallsThroughToBaseUpdate()
    {
        Expression<Func<IRebindFoo, bool>> lambda = x => x.Tag == "x";
        LambdaExpression rebound = QueryFilterRebinder.Rebind(lambda, typeof(RebindEntityWithExplicitImpl));

        Assert.NotSame(lambda, rebound);
        Assert.Equal(typeof(RebindEntityWithExplicitImpl), rebound.Parameters[0].Type);
    }

    [Fact]
    public void RowParameterExpander_EmptyRowParameters_ReturnsLambdaUnchanged()
    {
        Expression<Func<int, int>> lambda = x => x + 1;
        LambdaExpression result = RowParameterExpander.ExpandRowsInMethodCalls(lambda, []);
        Assert.Same(lambda, result);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitParameter_InInputParameters_ReturnsContextInput()
    {
        ParameterExpression param = Expression.Parameter(typeof(int), "x");
        QueryCompilerVisitor visitor = new([param]);

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

        QueryCompilerVisitor visitor = new();
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        Assert.Throws<InvalidOperationException>(() => compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitBinary_DefaultArm_Throws()
    {
        BinaryExpression node = Expression.LeftShift(Expression.Constant(1), Expression.Constant(2));
        QueryCompilerVisitor visitor = new();
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        Assert.Throws<NotSupportedException>(() => compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitNew_NullConstructorOnValueType_Throws()
    {
        NewExpression node = Expression.New(typeof(int));
        Assert.Null(node.Constructor);

        QueryCompilerVisitor visitor = new();
        Assert.Throws<NotSupportedException>(() => visitor.Visit(node));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMember_FieldInfo_ReturnsFieldValue()
    {
        ParameterExpression tupleParam = Expression.Parameter(typeof(ValueTuple<int>), "t");
        MemberExpression node = Expression.Field(tupleParam, "Item1");

        QueryCompilerVisitor visitor = new([tupleParam]);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new() { Input = new ValueTuple<int>(42) };
        Assert.Equal(42, compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitUnary_NegateWithUserDefinedOperator_InvokesMethod()
    {
        ParameterExpression p = Expression.Parameter(typeof(CompilerVisitorNegatable), "v");
        UnaryExpression node = Expression.Negate(p);
        Assert.NotNull(node.Method);

        QueryCompilerVisitor visitor = new([p]);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new() { Input = new CompilerVisitorNegatable(5) };
        CompilerVisitorNegatable result = (CompilerVisitorNegatable)compiled.Call(ctx)!;
        Assert.Equal(-5, result.Value);
    }

    [Fact]
    public void QueryCompilerVisitor_VisitUnary_DefaultArm_Throws()
    {
        UnaryExpression node = Expression.ArrayLength(Expression.Constant(new[] { 1, 2, 3 }));
        QueryCompilerVisitor visitor = new();
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        Assert.Throws<NotSupportedException>(() => compiled.Call(ctx));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitMemberInit_FieldAssignment_Direct()
    {
        NewExpression newExpr = Expression.New(typeof(CompilerVisitorFieldHolder));
        FieldInfo fld = typeof(CompilerVisitorFieldHolder).GetField(nameof(CompilerVisitorFieldHolder.FieldX))!;
        MemberAssignment assign = Expression.Bind(fld, Expression.Constant(42));
        MemberInitExpression node = Expression.MemberInit(newExpr, assign);

        QueryCompilerVisitor visitor = new();
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

        QueryCompilerVisitor visitor = new();
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

        QueryCompilerVisitor visitor = new();
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

        QueryCompilerVisitor visitor = new();
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new();
        object? result = compiled.Call(ctx);
        Assert.IsType<CompilerVisitorOuter>(result);
    }

    [Fact]
    public void QueryCompilerVisitor_InvokeOperator_AllNumericTypes_Direct()
    {
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
            Assert.NotNull(invokeOp.Invoke(null, [op, 5, 2]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5L, 2L]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5.0, 2.0]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5f, 2f]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5m, 2m]));
            Assert.NotNull(invokeOp.Invoke(null, [op, (short)5, (short)2]));
            Assert.NotNull(invokeOp.Invoke(null, [op, (ushort)5, (ushort)2]));
            Assert.NotNull(invokeOp.Invoke(null, [op, (byte)5, (byte)2]));
            Assert.NotNull(invokeOp.Invoke(null, [op, (sbyte)5, (sbyte)2]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5u, 2u]));
            Assert.NotNull(invokeOp.Invoke(null, [op, 5ul, 2ul]));
        }
    }

    [Fact]
    public void QueryCompilerVisitor_InvokeUnaryOperator_AllNumericTypes_Direct()
    {
        Type t = typeof(QueryCompilerVisitor);
        MethodInfo invokeUnary = t.GetMethod("InvokeUnaryOperator", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo negM = (MethodInfo)t.GetField("BinaryNegationOperator", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5L]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5.0]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5f]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, 5m]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, (short)5]));
        Assert.NotNull(invokeUnary.Invoke(null, [negM, (sbyte)5]));
    }

    [Fact]
    public void QueryCompilerVisitor_InvokeOperator_UnknownOperator_FallsThroughToGeneric()
    {
        Type t = typeof(QueryCompilerVisitor);
        MethodInfo invokeOp = t.GetMethod("InvokeOperator", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo negation = (MethodInfo)t.GetField("BinaryNegationOperator", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        Assert.Throws<TargetInvocationException>(() => invokeOp.Invoke(null, [negation, 5, 2]));
    }

    [Fact]
    public void QueryCompilerVisitor_InvokeUnaryOperator_UnknownOperator_FallsThroughToGeneric()
    {
        Type t = typeof(QueryCompilerVisitor);
        MethodInfo invokeUnary = t.GetMethod("InvokeUnaryOperator", BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo addition = (MethodInfo)t.GetField("BinaryAdditionOperator", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        Assert.Throws<TargetInvocationException>(() => invokeUnary.Invoke(null, [addition, 5]));
    }

    [Fact]
    public void QueryCompilerVisitor_VisitUnary_ConvertChecked_Converts()
    {
        ParameterExpression pp = Expression.Parameter(typeof(long), "v");
        UnaryExpression node = Expression.ConvertChecked(pp, typeof(int));

        QueryCompilerVisitor visitor = new([pp]);
        CompiledExpression compiled = (CompiledExpression)visitor.Visit(node);

        SQLiteQueryContext ctx = new() { Input = 42L };
        Assert.Equal(42, compiled.Call(ctx));
    }

    [Fact]
    public void RowParameterExpander_IsFrameworkTranslatedMethod_NullDeclaringType_ReturnsFalse()
    {
        MethodInfo isFrameworkTranslatedMethod = typeof(RowParameterExpander)
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

        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);
        PropertyVisitor propertyVisitor = new(sqlVisitor);

        SQLExpression source = new(typeof(string), 0, "\"Title\"", null);
        Expression result = propertyVisitor.HandleStringProperty("NotARealProperty", typeof(string), source);

        Assert.Same(source, result);
    }

    [Fact]
    public void QueryableMethodVisitor_VisitContains_ArgResolvesToNonConstantNonSql_Throws()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<Book>();

        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);
        sqlVisitor.TableColumns["Id"] = new SQLExpression(typeof(int), 0, "b0.Id");

        QueryableMethodVisitor qmv = new(db, sqlVisitor);

        MethodInfo containsMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == nameof(Queryable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(int));

        ConstantExpression source = Expression.Constant(Array.Empty<int>().AsQueryable(), typeof(IQueryable<int>));
        Expression weirdArg = Expression.Default(typeof(int));
        MethodCallExpression contains = Expression.Call(containsMethod, source, weirdArg);

        MethodInfo visitContains = typeof(QueryableMethodVisitor).GetMethod(
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

        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);
        QueryableMethodVisitor qmv = new(db, sqlVisitor);

        Expression unsupportedBody = Expression.Default(typeof(int));

        MethodInfo resolveTable = typeof(QueryableMethodVisitor).GetMethod(
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
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        MethodInfo unknownMethod = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;
        MethodCallExpression mce = Expression.Call(unknownMethod, Expression.Constant(""));

        Assert.Throws<NotSupportedException>(() => sqlVisitor.MethodVisitor.HandleSQLiteFunctionsMethod(mce));
    }

    [Fact]
    public void MethodVisitor_HandleGroupingMethod_UnknownAggregate_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ParameterExpression groupingParam = Expression.Parameter(typeof(IGrouping<int, Book>), "g");
        sqlVisitor.MethodArguments[groupingParam] = new Dictionary<string, Expression>
        {
            ["Key"] = new SQLExpression(typeof(int), 0, "b0.Id")
        };

        MethodInfo firstMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.First) && m.GetParameters().Length == 1)
            .MakeGenericMethod(typeof(Book));
        MethodCallExpression mce = Expression.Call(firstMethod, groupingParam);

        Assert.Throws<NotSupportedException>(() => sqlVisitor.MethodVisitor.HandleGroupingMethod(mce));
    }

    [Fact]
    public void MethodVisitor_HandleGroupingMethod_KeyNotResolvable_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ParameterExpression groupingParam = Expression.Parameter(typeof(IGrouping<int, Book>), "g");

        MethodInfo sumMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Sum) && m.GetParameters().Length == 1 && m.ReturnType == typeof(int));
        MethodCallExpression mce = Expression.Call(sumMethod, Expression.Convert(groupingParam, typeof(IEnumerable<int>)));

        Assert.ThrowsAny<Exception>(() => sqlVisitor.MethodVisitor.HandleGroupingMethod(mce));
    }

    [Fact]
    public void MethodVisitor_HandleIntegerMethod_ParseWithUnresolvableArg_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        MethodInfo parseMethod = typeof(int).GetMethod(nameof(int.Parse), [typeof(string)])!;
        Expression unresolvable = Expression.Default(typeof(string));
        MethodCallExpression mce = Expression.Call(parseMethod, unresolvable);

        Expression result = sqlVisitor.MethodVisitor.HandleIntegerMethod(mce);
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_HandleFloatingPointMethod_ParseWithUnresolvableArg_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        MethodInfo parseMethod = typeof(double).GetMethod(nameof(double.Parse), [typeof(string)])!;
        Expression unresolvable = Expression.Default(typeof(string));
        MethodCallExpression mce = Expression.Call(parseMethod, unresolvable);

        Expression result = sqlVisitor.MethodVisitor.HandleFloatingPointMethod(mce);
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_AggregateExpression_LambdaBodyNotSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ParameterExpression lambdaParam = Expression.Parameter(typeof(int), "x");
        LambdaExpression lambda = Expression.Lambda(Expression.Default(typeof(int)), lambdaParam);

        ParameterExpression source = Expression.Parameter(typeof(IEnumerable<int>), "g");
        MethodInfo sumMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Sum) && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.IsGenericType && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>) && m.ReturnType == typeof(int) && m.GetGenericArguments().Length == 1)
            .MakeGenericMethod(typeof(int));
        MethodCallExpression mce = Expression.Call(sumMethod, source, lambda);

        MethodInfo aggregateMethod = typeof(MethodVisitor).GetMethod("AggregateExpression", BindingFlags.NonPublic | BindingFlags.Instance)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() =>
            aggregateMethod.Invoke(sqlVisitor.MethodVisitor, [mce, "SUM", null]));

        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("Sum could not resolve", tie.InnerException!.Message);
    }

    [Fact]
    public void MethodVisitor_HandleCustomMethod_UnresolvableArg_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        MethodInfo dummy = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;
        MethodCallExpression mce = Expression.Call(dummy, Expression.Default(typeof(string)));

        ResolvedModel unresolvedArg = new() { IsConstant = false, Constant = null, SQLExpression = null, Expression = Expression.Default(typeof(string)) };
        SQLiteMethodTranslator translator = (instance, args) => "DUMMY";

        Expression result = sqlVisitor.MethodVisitor.HandleCustomMethod(mce, null, [unresolvedArg], translator);
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_HandleCustomMethod_UnresolvableObj_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        MethodInfo startsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
        Expression target = Expression.Default(typeof(string));
        MethodCallExpression mce = Expression.Call(target, startsWith, Expression.Constant("abc"));

        ResolvedModel unresolvedObj = new() { IsConstant = false, Constant = null, SQLExpression = null, Expression = target };
        ResolvedModel resolvedArg = new() { IsConstant = true, Constant = "abc", SQLExpression = new SQLExpression(typeof(string), 0, "@p0", "abc"), Expression = Expression.Constant("abc") };
        SQLiteMethodTranslator translator = (instance, args) => "DUMMY";

        Expression result = sqlVisitor.MethodVisitor.HandleCustomMethod(mce, unresolvedObj, [resolvedArg], translator);
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_UnwrapPredicateBody_NonLambda_ReturnsExpressionUnchanged()
    {
        ConstantExpression literal = Expression.Constant("not a lambda", typeof(string));

        MethodInfo method = typeof(MethodVisitor).GetMethod("UnwrapPredicateBody", BindingFlags.NonPublic | BindingFlags.Static)!;
        Expression result = (Expression)method.Invoke(null, [literal])!;

        Assert.Same(literal, result);
    }

    [Fact]
    public void MethodVisitor_ResolveEntityAlias_UnsupportedExpression_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        Expression weird = Expression.Default(typeof(int));
        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveEntityAlias", BindingFlags.NonPublic | BindingFlags.Instance)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(sqlVisitor.MethodVisitor, [weird]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("direct entity reference", tie.InnerException!.Message);
    }

    [Fact]
    public void MethodVisitor_ResolveEntityAlias_ParameterWithNonSqlValues_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["NotSql"] = Expression.Constant("plain")
        };

        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveEntityAlias", BindingFlags.NonPublic | BindingFlags.Instance)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(sqlVisitor.MethodVisitor, [pe]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
    }

    [Fact]
    public void MethodVisitor_ResolveEntityAlias_ParameterWithDotlessSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["Id"] = new SQLExpression(typeof(int), 0, "noDots")
        };

        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveEntityAlias", BindingFlags.NonPublic | BindingFlags.Instance)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(sqlVisitor.MethodVisitor, [pe]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
    }

    [Fact]
    public void MethodVisitor_ResolveEntityAlias_MemberWithDotlessSql_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["Title"] = new SQLExpression(typeof(string), 0, "noDots")
        };
        MemberExpression member = Expression.Property(pe, nameof(Book.Title));

        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveEntityAlias", BindingFlags.NonPublic | BindingFlags.Instance)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(sqlVisitor.MethodVisitor, [member]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_NonMemberArg_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        Expression columnArg = Expression.Constant("Title");
        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(sqlVisitor.MethodVisitor, [typeof(SQLite.Framework.Tests.Entities.ArticleSearch), columnArg]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("direct property reference", tie.InnerException!.Message);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_UndeclaredColumn_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(Book), "b");
        MemberExpression nonFtsColumn = Expression.Property(pe, nameof(Book.AuthorId));

        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(sqlVisitor.MethodVisitor, [typeof(SQLite.Framework.Tests.Entities.ArticleSearch), nonFtsColumn]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("not declared", tie.InnerException!.Message);
    }

    [Fact]
    public void MethodVisitor_HandleEnumMethod_InstanceMethodOtherThanHasFlagOrToString_FallsThrough()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ConstantExpression enumValue = Expression.Constant(DayOfWeek.Monday);
        MethodInfo getTypeMethod = typeof(object).GetMethod(nameof(object.GetType))!;
        MethodCallExpression mce = Expression.Call(enumValue, getTypeMethod);

        Assert.Throws<NotSupportedException>(() => sqlVisitor.MethodVisitor.HandleEnumMethod(mce));
    }

    [Fact]
    public void MethodVisitor_HandleEnumMethod_NonGenericParseWithUnresolvableString_ReturnsNode()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        MethodInfo parseMethod = typeof(Enum).GetMethods()
            .First(m => m.Name == nameof(Enum.Parse) && m.GetParameters().Length == 2 && !m.IsGenericMethod && m.GetParameters()[0].ParameterType == typeof(Type));

        Expression typeArg = Expression.Constant(typeof(DayOfWeek), typeof(Type));
        Expression unresolvable = Expression.Default(typeof(string));
        MethodCallExpression mce = Expression.Call(parseMethod, typeArg, unresolvable);

        Expression result = sqlVisitor.MethodVisitor.HandleEnumMethod(mce);
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_HandleEnumMethod_NonGenericParseWithoutTypeArg_ReturnsNode()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        MethodInfo parseMethod = typeof(Enum).GetMethods()
            .First(m => m.Name == nameof(Enum.Parse) && m.GetParameters().Length == 2 && !m.IsGenericMethod && m.GetParameters()[0].ParameterType == typeof(Type));

        Expression typeArg = Expression.Constant(null, typeof(Type));
        MethodCallExpression mce = Expression.Call(parseMethod, typeArg, Expression.Constant("X"));

        Expression result = sqlVisitor.MethodVisitor.HandleEnumMethod(mce);
        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_HandleFTS5Match_ColumnViaConvert_Works()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<SQLite.Framework.Tests.Entities.Article>();
        db.Schema.CreateTable<SQLite.Framework.Tests.Entities.ArticleSearch>();

        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(SQLite.Framework.Tests.Entities.ArticleSearch), "a");
        sqlVisitor.MethodArguments[pe] = new Dictionary<string, Expression>
        {
            ["Title"] = new SQLExpression(typeof(string), 0, "a0.Title")
        };

        MemberExpression titleMember = Expression.Property(pe, nameof(SQLite.Framework.Tests.Entities.ArticleSearch.Title));
        UnaryExpression convert = Expression.Convert(titleMember, typeof(string));

        MethodInfo matchMethod = typeof(SQLiteFunctions).GetMethods()
            .First(m => m.Name == nameof(SQLiteFunctions.Match)
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType == typeof(string)
                && m.GetParameters()[1].ParameterType == typeof(string));

        MethodInfo handleMatch = typeof(MethodVisitor).GetMethod("HandleFTS5Match", BindingFlags.NonPublic | BindingFlags.Instance)!;

        MethodCallExpression mce = Expression.Call(matchMethod, convert, Expression.Constant("hello"));

        Expression? result = (Expression?)handleMatch.Invoke(sqlVisitor.MethodVisitor, [mce]);
        Assert.NotNull(result);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_ConvertWrappedMember_Resolves()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<SQLite.Framework.Tests.Entities.Article>();
        db.Schema.CreateTable<SQLite.Framework.Tests.Entities.ArticleSearch>();

        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        ParameterExpression pe = Expression.Parameter(typeof(SQLite.Framework.Tests.Entities.ArticleSearch), "a");
        MemberExpression member = Expression.Property(pe, nameof(SQLite.Framework.Tests.Entities.ArticleSearch.Title));
        UnaryExpression convert = Expression.Convert(member, typeof(string));

        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
        int result = (int)method.Invoke(sqlVisitor.MethodVisitor, [typeof(SQLite.Framework.Tests.Entities.ArticleSearch), convert])!;

        Assert.True(result >= 0);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_NeitherMemberNorConvertMember_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        BinaryExpression nonMember = Expression.Add(Expression.Constant(1), Expression.Constant(2));

        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(sqlVisitor.MethodVisitor, [typeof(SQLite.Framework.Tests.Entities.ArticleSearch), nonMember]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("direct property reference", tie.InnerException!.Message);
    }

    [Fact]
    public void MethodVisitor_ResolveTrim_UnresolvableTrimChars_FallsBackToCall()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        MethodInfo trim = typeof(string).GetMethod(nameof(string.Trim), [typeof(char[])])!;
        NewArrayExpression arr = Expression.NewArrayInit(typeof(char), Expression.Default(typeof(char)));
        MethodCallExpression mce = Expression.Call(Expression.Constant("hello"), trim, arr);

        SQLExpression objSql = new(typeof(string), 0, "\"Title\"");
        ResolvedModel arrArg = new() { IsConstant = false, Constant = null, SQLExpression = null, Expression = arr };

        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveTrim", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Expression result = (Expression)method.Invoke(sqlVisitor.MethodVisitor, [mce, objSql, new List<ResolvedModel> { arrArg }, "TRIM"])!;

        Assert.IsAssignableFrom<MethodCallExpression>(result);
    }

    [Fact]
    public void MethodVisitor_ResolveFTS5ColumnIndex_NonFtsEntity_Throws()
    {
        using TestDatabase db = new();
        SQLVisitor sqlVisitor = new(db, new IndexWrapper(), new IndexWrapper(), new TableIndexWrapper(), 0);

        Expression columnArg = Expression.Constant("Title");
        MethodInfo method = typeof(MethodVisitor).GetMethod("ResolveFTS5ColumnIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;

        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => method.Invoke(sqlVisitor.MethodVisitor, [typeof(Book), columnArg]));
        Assert.IsType<NotSupportedException>(tie.InnerException);
        Assert.Contains("FullTextSearch", tie.InnerException!.Message);
    }
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
