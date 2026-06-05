using System.Numerics;

namespace SQLite.Framework.Tests.Entities;

public readonly record struct Coverage_ShiftableValue(int Value)
    : IBitwiseOperators<Coverage_ShiftableValue, Coverage_ShiftableValue, Coverage_ShiftableValue>,
      IShiftOperators<Coverage_ShiftableValue, int, Coverage_ShiftableValue>
{
    public static Coverage_ShiftableValue operator &(Coverage_ShiftableValue left, Coverage_ShiftableValue right) => new(left.Value & right.Value);
    public static Coverage_ShiftableValue operator |(Coverage_ShiftableValue left, Coverage_ShiftableValue right) => new(left.Value | right.Value);
    public static Coverage_ShiftableValue operator ^(Coverage_ShiftableValue left, Coverage_ShiftableValue right) => new(left.Value ^ right.Value);
    public static Coverage_ShiftableValue operator ~(Coverage_ShiftableValue value) => new(~value.Value);
    public static Coverage_ShiftableValue operator <<(Coverage_ShiftableValue value, int shiftAmount) => new(value.Value << shiftAmount);
    public static Coverage_ShiftableValue operator >>(Coverage_ShiftableValue value, int shiftAmount) => new(value.Value >> shiftAmount);
    public static Coverage_ShiftableValue operator >>>(Coverage_ShiftableValue value, int shiftAmount) => new(value.Value >>> shiftAmount);
}
