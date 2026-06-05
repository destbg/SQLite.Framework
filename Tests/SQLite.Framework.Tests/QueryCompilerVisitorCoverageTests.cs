using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
using SQLite.Framework.Generated;
#endif
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class QueryCompilerVisitorCoverageTests
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.NonPublicFields)]
    private static readonly Type VisitorType = typeof(QueryCompilerVisitor);

    private static readonly SQLiteOptions TestOptions = new SQLiteOptionsBuilder("compiler-coverage.db3").Build();

    private static readonly MethodInfo InvokeOperator = VisitorType.GetMethod("InvokeOperator", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo InvokeUnaryOperator = VisitorType.GetMethod("InvokeUnaryOperator", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo CompareValues = VisitorType.GetMethod("CompareValues", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static MethodInfo GetOpenMethod(string fieldName)
    {
        FieldInfo field = VisitorType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field {fieldName} not found.");
        return (MethodInfo)field.GetValue(null)!;
    }

    [Fact]
    public void Constructor_ThrowsWhenReflectionFallbackDisabled()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("compiler-fallback-disabled.db3")
            .DisableReflectionFallback()
            .Build();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new QueryCompilerVisitor(options));
        Assert.Contains("ReflectionFallbackDisabled", ex.Message);
    }

#if !SQLITE_FRAMEWORK_SOURCE_GENERATOR
#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void InvokeOperator_AdditionOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(3m);
        Coverage_NumericValue b = new(5m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinaryAdditionOperator"), a, b, TestOptions]);

        Assert.Equal(new Coverage_NumericValue(8m), result);
    }
#endif

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void InvokeOperator_SubtractionOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(10m);
        Coverage_NumericValue b = new(3m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinarySubtractionOperator"), a, b, TestOptions]);

        Assert.Equal(new Coverage_NumericValue(7m), result);
    }
#endif

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void InvokeOperator_MultiplyOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(4m);
        Coverage_NumericValue b = new(6m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinaryMultiplyOperator"), a, b, TestOptions]);

        Assert.Equal(new Coverage_NumericValue(24m), result);
    }
#endif

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void InvokeOperator_DivisionOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(20m);
        Coverage_NumericValue b = new(4m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinaryDivisionOperator"), a, b, TestOptions]);

        Assert.Equal(new Coverage_NumericValue(5m), result);
    }
#endif

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void InvokeOperator_ModulusOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(10m);
        Coverage_NumericValue b = new(3m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinaryModulusOperator"), a, b, TestOptions]);

        Assert.Equal(new Coverage_NumericValue(1m), result);
    }
#endif

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void InvokeUnaryOperator_NegationOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue value = new(7m);

        object? result = InvokeUnaryOperator.Invoke(null, [GetOpenMethod("BinaryNegationOperator"), value, TestOptions]);

        Assert.Equal(new Coverage_NumericValue(-7m), result);
    }
#endif
#endif

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
    [Fact]
    public void InvokeGenericOperator_UnderPublishAotWithoutGeneratedMaterializers_ThrowsNotSupported()
    {
        SQLiteOptions emptyOptions = new SQLiteOptionsBuilder("compiler-throw.db3").Build();
        Coverage_NumericValue a = new(3m);
        Coverage_NumericValue b = new(5m);

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(
            () => InvokeOperator.Invoke(null, [GetOpenMethod("BinaryAdditionOperator"), a, b, emptyOptions]));

        NotSupportedException inner = Assert.IsType<NotSupportedException>(ex.InnerException);
        Assert.Contains("PublishAot", inner.Message);
    }

    [Fact]
    public void InvokeGenericUnaryOperator_UnderPublishAotWithoutGeneratedMaterializers_ThrowsNotSupported()
    {
        SQLiteOptions emptyOptions = new SQLiteOptionsBuilder("compiler-throw-unary.db3").Build();
        Coverage_NumericValue value = new(7m);

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(
            () => InvokeUnaryOperator.Invoke(null, [GetOpenMethod("BinaryNegationOperator"), value, emptyOptions]));

        NotSupportedException inner = Assert.IsType<NotSupportedException>(ex.InnerException);
        Assert.Contains("PublishAot", inner.Message);
    }

    [Fact]
    public void InvokeGenericOperator_UnderPublishAotWithGeneratedMaterializers_GoesThroughGenericFallback()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder("compiler-aot-genmat.db3")
            .UseGeneratedMaterializers()
            .Build();
        Coverage_NumericValue a = new(8m);
        Coverage_NumericValue b = new(3m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinarySubtractionOperator"), a, b, options]);

        Assert.Equal(new Coverage_NumericValue(5m), result);
    }
#endif

    [Fact]
    public void CompareValues_LeftNullRightComparable_UsesRight()
    {
        int result = (int)CompareValues.Invoke(null, [null, 5])!;

        Assert.Equal(1, result);
    }

    [Fact]
    public void CompareValues_BothNull_ReturnsZero()
    {
        int result = (int)CompareValues.Invoke(null, [null, null])!;

        Assert.Equal(0, result);
    }

    [Fact]
    public void CompareValues_NeitherComparableNorNull_Throws()
    {
        Coverage_NotComparableValue left = new(1);
        Coverage_NotComparableValue right = new(2);

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            CompareValues.Invoke(null, [left, right]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
        Assert.Contains("Cannot compare values of type", ex.InnerException!.Message);
    }

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void Visit_UnsupportedExpressionTypes_AllThrowNotSupported()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        LabelTarget target = Expression.Label();

        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.Block(Expression.Constant(1))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.Default(typeof(int))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.Goto(target)));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.Invoke(Expression.Lambda<Func<int>>(Expression.Constant(1)))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.Label(target, Expression.Constant(0))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.Lambda<Func<int>>(Expression.Constant(1))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.Loop(Expression.Constant(1))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.Switch(Expression.Constant(1), Expression.Constant(2), Expression.SwitchCase(Expression.Constant(3), Expression.Constant(1)))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.TryCatch(Expression.Constant(1), Expression.Catch(typeof(Exception), Expression.Constant(0)))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.RuntimeVariables(Expression.Parameter(typeof(int), "p"))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(Expression.TypeIs(Expression.Constant(1), typeof(int))));
        Assert.Throws<NotSupportedException>(() => visitor.Visit(new UnsupportedExtensionExpression()));
    }
#endif

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void VisitDynamic_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        DynamicExpression dyn = Expression.Dynamic(new TestCallSiteBinder(), typeof(object), Expression.Constant(0));

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitDynamic", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [dyn]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }
#endif

    [Fact]
    public void VisitMemberAssignment_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        MemberInfo member = typeof(Coverage_BindTarget).GetProperty(nameof(Coverage_BindTarget.Value))!;
        MemberAssignment binding = Expression.Bind(member, Expression.Constant(1));

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitMemberAssignment", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [binding]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void VisitElementInit_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        MethodInfo addMethod = typeof(List<int>).GetMethod(nameof(List<int>.Add))!;
        ElementInit init = Expression.ElementInit(addMethod, Expression.Constant(1));

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitElementInit", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [init]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void VisitMemberBinding_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        PropertyInfo prop = typeof(Coverage_BindTarget).GetProperty(nameof(Coverage_BindTarget.Value))!;
        MemberAssignment binding = Expression.Bind(prop, Expression.Constant(1));

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitMemberBinding", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [binding]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void VisitMemberListBinding_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        PropertyInfo prop = typeof(Coverage_BindTarget).GetProperty(nameof(Coverage_BindTarget.List))!;
        MethodInfo addMethod = typeof(List<int>).GetMethod(nameof(List<int>.Add))!;
        MemberListBinding binding = Expression.ListBind(prop, Expression.ElementInit(addMethod, Expression.Constant(1)));

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitMemberListBinding", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [binding]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void VisitMemberMemberBinding_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        PropertyInfo nested = typeof(Coverage_BindTarget).GetProperty(nameof(Coverage_BindTarget.Nested))!;
        PropertyInfo nestedValue = typeof(Coverage_BindTargetNested).GetProperty(nameof(Coverage_BindTargetNested.Value))!;
        MemberMemberBinding binding = Expression.MemberBind(nested, Expression.Bind(nestedValue, Expression.Constant(1)));

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitMemberMemberBinding", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [binding]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void VisitCatchBlock_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        CatchBlock block = Expression.Catch(typeof(Exception), Expression.Constant(0));

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitCatchBlock", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [block]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void VisitDebugInfo_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        SymbolDocumentInfo doc = Expression.SymbolDocument("x");
        DebugInfoExpression debug = Expression.DebugInfo(doc, 1, 1, 1, 1);

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitDebugInfo", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [debug]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void VisitSwitchCase_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        SwitchCase switchCase = Expression.SwitchCase(Expression.Constant(1), Expression.Constant(0));

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitSwitchCase", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [switchCase]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void VisitLabelTarget_Null_ReturnsNull()
    {
        QueryCompilerVisitor visitor = new(TestOptions);

        object? result = VisitorType.GetMethod("VisitLabelTarget", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(visitor, [null]);

        Assert.Null(result);
    }

    [Fact]
    public void VisitLabelTarget_NonNull_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        LabelTarget target = Expression.Label();

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            VisitorType.GetMethod("VisitLabelTarget", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(visitor, [target]));

        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void VisitIndex_Throws()
    {
        QueryCompilerVisitor visitor = new(TestOptions);
        ConstantExpression list = Expression.Constant(new List<int> { 1, 2, 3 });
        PropertyInfo indexer = typeof(List<int>).GetProperty("Item")!;
        IndexExpression idx = Expression.MakeIndex(list, indexer, [Expression.Constant(0)]);

        Assert.Throws<NotSupportedException>(() => visitor.Visit(idx));
    }
}

public sealed class Coverage_BindTarget
{
    public int Value { get; set; }
    public Coverage_BindTargetNested Nested { get; set; } = new();
    public List<int> List { get; set; } = [];
}

public sealed class Coverage_BindTargetNested
{
    public int Value { get; set; }
}

public sealed class UnsupportedExtensionExpression : Expression
{
    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof(int);
    public override bool CanReduce => false;
}

public sealed class TestCallSiteBinder : System.Runtime.CompilerServices.CallSiteBinder
{
    public override Expression Bind(object[] args, System.Collections.ObjectModel.ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
    {
        throw new NotImplementedException();
    }
}
