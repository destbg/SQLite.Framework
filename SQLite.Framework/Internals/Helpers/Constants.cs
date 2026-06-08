namespace SQLite.Framework.Internals.Helpers;

internal static class Constants
{
    /// <summary>
    /// The number of ticks between the .NET epoch (1/1/0001) and the Unix epoch (1/1/1970).
    /// <code>
    /// new DateTime(1970, 1, 1).Ticks
    /// </code>
    /// </summary>
    public const long TicksToEpoch = 621355968000000000;

    /// <summary>
    /// Prefix used to stash the source row columns of a composite-key GroupBy in the grouping map.
    /// </summary>
    public const string GroupingElementPrefix = "$elem.";

    /// <summary>
    /// Unicode whitespace code points that .NET treats as whitespace.
    /// </summary>
    public static readonly int[] WhitespaceCodePoints =
    [
        9, 10, 11, 12, 13, 32, 133, 160, 5760, 8192, 8193, 8194, 8195, 8196, 8197,
        8198, 8199, 8200, 8201, 8202, 8232, 8233, 8239, 8287, 12288,
    ];

    /// <summary>
    /// SQLite <c>CHAR(...)</c> list of the Unicode whitespace code points that .NET treats as whitespace.
    /// Passed as the second argument to TRIM, LTRIM and RTRIM so the result matches .NET Trim and the
    /// whitespace checks.
    /// </summary>
    public static readonly string WhitespaceChars = $"CHAR({string.Join(", ", WhitespaceCodePoints)})";

    /// <summary>
    /// Bit mask for 16 unsigned bits (ushort.MaxValue, 0xFFFF). Keeps a char code point inside the
    /// 16-bit range in generated SQL, where math is 64-bit.
    /// </summary>
    public const long UInt16Mask = 65535;

    /// <summary>
    /// Bit mask for 32 unsigned bits (uint.MaxValue, 0xFFFFFFFF). Keeps a value inside the uint range
    /// in generated SQL, where math is 64-bit.
    /// </summary>
    public const long UInt32Mask = 4294967295;

    /// <summary>
    /// The int sign bit value (2 to the power 31, 0x80000000). Used to shift a uint result back into
    /// the signed int range in generated SQL.
    /// </summary>
    public const long Int32SignBit = 2147483648;

    /// <summary>
    /// Two to the power 32 (0x100000000). Used as the modulus that wraps a value into the uint range
    /// in generated SQL.
    /// </summary>
    public const long UInt32Modulus = 4294967296;

    /// <summary>
    /// Bit mask that clears the sign bit of a 64-bit integer (long.MaxValue, 0x7FFFFFFFFFFFFFFF).
    /// Used after a right shift to emulate an unsigned (logical) shift in generated SQL.
    /// </summary>
    public const long Int64SignMask = 9223372036854775807;

    /// <summary>
    /// Mask for a 32-bit shift count (0x1F). C# masks the shift amount to 0-31 for 32-bit operands,
    /// so the generated SQL does the same.
    /// </summary>
    public const int Shift32CountMask = 31;

    /// <summary>
    /// Mask for a 64-bit shift count (0x3F). C# masks the shift amount to 0-63 for 64-bit operands,
    /// so the generated SQL does the same.
    /// </summary>
    public const int Shift64CountMask = 63;
}
