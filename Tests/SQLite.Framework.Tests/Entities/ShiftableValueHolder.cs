using System.Numerics;

namespace SQLite.Framework.Tests.Entities;

public readonly record struct ShiftableValueHolder(int Value)
    : IBitwiseOperators<ShiftableValueHolder, ShiftableValueHolder, ShiftableValueHolder>,
      IShiftOperators<ShiftableValueHolder, int, ShiftableValueHolder>
{
    public static ShiftableValueHolder operator &(ShiftableValueHolder left, ShiftableValueHolder right) => new(left.Value & right.Value);
    public static ShiftableValueHolder operator |(ShiftableValueHolder left, ShiftableValueHolder right) => new(left.Value | right.Value);
    public static ShiftableValueHolder operator ^(ShiftableValueHolder left, ShiftableValueHolder right) => new(left.Value ^ right.Value);
    public static ShiftableValueHolder operator ~(ShiftableValueHolder value) => new(~value.Value);
    public static ShiftableValueHolder operator <<(ShiftableValueHolder value, int shiftAmount) => new(value.Value << shiftAmount);
    public static ShiftableValueHolder operator >>(ShiftableValueHolder value, int shiftAmount) => new(value.Value >> shiftAmount);
    public static ShiftableValueHolder operator >>>(ShiftableValueHolder value, int shiftAmount) => new(value.Value >>> shiftAmount);
}
