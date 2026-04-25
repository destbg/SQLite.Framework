using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class QueryCompilerVisitorCoverageTests
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.NonPublicFields)]
    private static readonly Type VisitorType = typeof(QueryCompilerVisitor);

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
    public void InvokeOperator_AdditionOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(3m);
        Coverage_NumericValue b = new(5m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinaryAdditionOperator"), a, b]);

        Assert.Equal(new Coverage_NumericValue(8m), result);
    }

    [Fact]
    public void InvokeOperator_SubtractionOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(10m);
        Coverage_NumericValue b = new(3m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinarySubtractionOperator"), a, b]);

        Assert.Equal(new Coverage_NumericValue(7m), result);
    }

    [Fact]
    public void InvokeOperator_MultiplyOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(4m);
        Coverage_NumericValue b = new(6m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinaryMultiplyOperator"), a, b]);

        Assert.Equal(new Coverage_NumericValue(24m), result);
    }

    [Fact]
    public void InvokeOperator_DivisionOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(20m);
        Coverage_NumericValue b = new(4m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinaryDivisionOperator"), a, b]);

        Assert.Equal(new Coverage_NumericValue(5m), result);
    }

    [Fact]
    public void InvokeOperator_ModulusOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue a = new(10m);
        Coverage_NumericValue b = new(3m);

        object? result = InvokeOperator.Invoke(null, [GetOpenMethod("BinaryModulusOperator"), a, b]);

        Assert.Equal(new Coverage_NumericValue(1m), result);
    }

    [Fact]
    public void InvokeUnaryOperator_NegationOnCustomStruct_GoesThroughGenericFallback()
    {
        Coverage_NumericValue value = new(7m);

        object? result = InvokeUnaryOperator.Invoke(null, [GetOpenMethod("BinaryNegationOperator"), value]);

        Assert.Equal(new Coverage_NumericValue(-7m), result);
    }

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
}
