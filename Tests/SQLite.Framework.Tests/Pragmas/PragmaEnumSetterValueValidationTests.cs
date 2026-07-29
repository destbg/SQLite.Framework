using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PragmaEnumSetterValueValidationTests
{
    [Fact]
    public void SynchronousModeRejectsUndefinedEnumValue()
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => db.Pragmas.SynchronousMode = (SQLiteSynchronousMode)99);
    }

    [Fact]
    public void AutoVacuumRejectsUndefinedEnumValue()
    {
        using TestDatabase db = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => db.Pragmas.AutoVacuum = (SQLiteAutoVacuumMode)99);
    }
}
