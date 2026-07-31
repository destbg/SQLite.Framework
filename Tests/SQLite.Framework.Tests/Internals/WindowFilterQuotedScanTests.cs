using System.Reflection;
using SQLite.Framework.Internals.Visitors.Member;

namespace SQLite.Framework.Tests;

public class WindowFilterQuotedScanTests
{
    private static readonly MethodInfo IndexOfUnquoted = typeof(WindowFunctionsMemberVisitor)
        .GetMethod("IndexOfUnquoted", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public void ANeedleInsideAStringLiteralIsSkipped()
    {
        int index = (int)IndexOfUnquoted.Invoke(null, ["lead(\"v\", 1, ' OVER x') OVER ()", " OVER"])!;

        Assert.Equal(23, index);
    }

    [Fact]
    public void ANeedleOnlyInsideAQuotedNameIsNotFound()
    {
        int index = (int)IndexOfUnquoted.Invoke(null, ["\" OVER \"", " OVER"])!;

        Assert.Equal(-1, index);
    }
}
