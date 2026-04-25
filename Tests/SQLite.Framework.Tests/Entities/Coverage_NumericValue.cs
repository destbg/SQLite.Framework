using System.Numerics;

namespace SQLite.Framework.Tests.Entities;

public readonly record struct Coverage_NumericValue(decimal Value)
    : IAdditionOperators<Coverage_NumericValue, Coverage_NumericValue, Coverage_NumericValue>,
      ISubtractionOperators<Coverage_NumericValue, Coverage_NumericValue, Coverage_NumericValue>,
      IMultiplyOperators<Coverage_NumericValue, Coverage_NumericValue, Coverage_NumericValue>,
      IDivisionOperators<Coverage_NumericValue, Coverage_NumericValue, Coverage_NumericValue>,
      IModulusOperators<Coverage_NumericValue, Coverage_NumericValue, Coverage_NumericValue>,
      IUnaryNegationOperators<Coverage_NumericValue, Coverage_NumericValue>
{
    public static Coverage_NumericValue operator +(Coverage_NumericValue a, Coverage_NumericValue b) => new(a.Value + b.Value);
    public static Coverage_NumericValue operator -(Coverage_NumericValue a, Coverage_NumericValue b) => new(a.Value - b.Value);
    public static Coverage_NumericValue operator *(Coverage_NumericValue a, Coverage_NumericValue b) => new(a.Value * b.Value);
    public static Coverage_NumericValue operator /(Coverage_NumericValue a, Coverage_NumericValue b) => new(a.Value / b.Value);
    public static Coverage_NumericValue operator %(Coverage_NumericValue a, Coverage_NumericValue b) => new(a.Value % b.Value);
    public static Coverage_NumericValue operator -(Coverage_NumericValue a) => new(-a.Value);
}
