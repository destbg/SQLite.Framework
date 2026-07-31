using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SignedTextIntegerColumnReadTests
{
    [Fact]
    public void AStoredTextWithAMinusSignReadsAsANegativeLong()
    {
        using TestDatabase db = new(null, nameof(AStoredTextWithAMinusSignReadsAsANegativeLong));

        Assert.Equal(-42L, db.ExecuteScalar<long>("SELECT '-42'"));
    }

    [Fact]
    public void AStoredTextWithAPlusSignReadsAsAPositiveLong()
    {
        using TestDatabase db = new(null, nameof(AStoredTextWithAPlusSignReadsAsAPositiveLong));

        Assert.Equal(42L, db.ExecuteScalar<long>("SELECT '+42'"));
    }

    [Fact]
    public void AStoredTextWithLeadingZerosReadsTheDigitsAfterThem()
    {
        using TestDatabase db = new(null, nameof(AStoredTextWithLeadingZerosReadsTheDigitsAfterThem));

        Assert.Equal(7L, db.ExecuteScalar<long>("SELECT '007'"));
    }

    [Fact]
    public void AStoredTextOfOnlySpacesReadsAsZero()
    {
        using TestDatabase db = new(null, nameof(AStoredTextOfOnlySpacesReadsAsZero));

        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT '   '"));
    }

    [Fact]
    public void AStoredTextJustAboveLongMaxClampsToLongMax()
    {
        using TestDatabase db = new(null, nameof(AStoredTextJustAboveLongMaxClampsToLongMax));

        Assert.Equal(long.MaxValue, db.ExecuteScalar<long>("SELECT '9223372036854775808'"));
    }

    [Fact]
    public void AStoredTextOfExactLongMinReadsAsLongMin()
    {
        using TestDatabase db = new(null, nameof(AStoredTextOfExactLongMinReadsAsLongMin));

        Assert.Equal(long.MinValue, db.ExecuteScalar<long>("SELECT '-9223372036854775808'"));
    }

    [Fact]
    public void AStoredTextJustBelowLongMinClampsToLongMin()
    {
        using TestDatabase db = new(null, nameof(AStoredTextJustBelowLongMinClampsToLongMin));

        Assert.Equal(long.MinValue, db.ExecuteScalar<long>("SELECT '-9223372036854775809'"));
    }

    [Fact]
    public void AStoredTextWithTwentyDigitsClampsToLongMax()
    {
        using TestDatabase db = new(null, nameof(AStoredTextWithTwentyDigitsClampsToLongMax));

        Assert.Equal(long.MaxValue, db.ExecuteScalar<long>("SELECT '99999999999999999999'"));
    }

    [Fact]
    public void AStoredNegativeTextWithTwentyDigitsClampsToLongMin()
    {
        using TestDatabase db = new(null, nameof(AStoredNegativeTextWithTwentyDigitsClampsToLongMin));

        Assert.Equal(long.MinValue, db.ExecuteScalar<long>("SELECT '-99999999999999999999'"));
    }
}
