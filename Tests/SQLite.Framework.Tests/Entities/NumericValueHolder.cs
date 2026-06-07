using System.Numerics;

namespace SQLite.Framework.Tests.Entities;

public readonly record struct NumericValueHolder(decimal Value)
    : IAdditionOperators<NumericValueHolder, NumericValueHolder, NumericValueHolder>,
      ISubtractionOperators<NumericValueHolder, NumericValueHolder, NumericValueHolder>,
      IMultiplyOperators<NumericValueHolder, NumericValueHolder, NumericValueHolder>,
      IDivisionOperators<NumericValueHolder, NumericValueHolder, NumericValueHolder>,
      IModulusOperators<NumericValueHolder, NumericValueHolder, NumericValueHolder>,
      IUnaryNegationOperators<NumericValueHolder, NumericValueHolder>
{
    public static NumericValueHolder operator +(NumericValueHolder a, NumericValueHolder b) => new(a.Value + b.Value);
    public static NumericValueHolder operator -(NumericValueHolder a, NumericValueHolder b) => new(a.Value - b.Value);
    public static NumericValueHolder operator *(NumericValueHolder a, NumericValueHolder b) => new(a.Value * b.Value);
    public static NumericValueHolder operator /(NumericValueHolder a, NumericValueHolder b) => new(a.Value / b.Value);
    public static NumericValueHolder operator %(NumericValueHolder a, NumericValueHolder b) => new(a.Value % b.Value);
    public static NumericValueHolder operator -(NumericValueHolder a) => new(-a.Value);
}
