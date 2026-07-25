using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Visitors.Member;
using SQLite.Framework.Internals.Visitors.SQL;
using SQLitePCL;

namespace SQLite.Framework.Tests;

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
internal enum UlongBackedEnum : ulong
{
    A
}

internal sealed class NestedOuter
{
    public NestedInner Inner { get; set; } = new();
}

internal sealed class NestedInner
{
    public int Value { get; set; }
}

public class InternalCoverageTests
{
    [Fact]
    public void WindowCallDetectorClassifiesCallKinds()
    {
        Expression<Func<SQLiteWindow<double>>> windowFunc = () => SQLiteWindowFunctions.Sum(1.0);
        Assert.True(WindowCallDetector.Contains(windowFunc.Body));

        Expression<Func<SQLiteWindow<double>>> windowChain = () => SQLiteWindowFunctions.Sum(1.0).Over();
        Assert.True(WindowCallDetector.Contains(windowChain.Body));

        Expression<Func<bool>> genericNonWindow = () => new List<int>().Contains(1);
        Assert.False(WindowCallDetector.Contains(genericNonWindow.Body));

        Expression<Func<string>> nonGeneric = () => string.Empty.ToUpper();
        Assert.False(WindowCallDetector.Contains(nonGeneric.Body));

        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            DynamicMethod nullDeclaringType = new("NoDeclaringType", typeof(int), Type.EmptyTypes);
            ILGenerator il = nullDeclaringType.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ret);
            Assert.False(WindowCallDetector.Contains(Expression.Call(nullDeclaringType)));
        }
    }

    [Fact]
    public void StripEnumBoxingHandlesNonConvertArguments()
    {
        MethodInfo method = typeof(EnumMemberVisitor).GetMethod("StripEnumBoxing", BindingFlags.Static | BindingFlags.NonPublic)!;

        Expression negate = Expression.Negate(Expression.Constant(5));
        Assert.Same(negate, method.Invoke(null, [negate]));

        Expression constant = Expression.Constant(5);
        Assert.Same(constant, method.Invoke(null, [constant]));

        UnaryExpression convert = Expression.Convert(Expression.Constant(DayOfWeek.Monday), typeof(Enum));
        Assert.Same(convert.Operand, method.Invoke(null, [convert]));
    }

    [Fact]
    public void IsUlongSourceClassifiesTypes()
    {
        MethodInfo method = typeof(SQLVisitor).GetMethod("IsUlongSource", BindingFlags.Static | BindingFlags.NonPublic)!;

        Assert.True((bool)method.Invoke(null, [typeof(UlongBackedEnum)])!);
        Assert.True((bool)method.Invoke(null, [typeof(ulong?)])!);
        Assert.False((bool)method.Invoke(null, [typeof(int)])!);
    }

    [Fact]
    public void IsRawColumnPassthroughMemberWalksNestedMembers()
    {
        MethodInfo method = typeof(SQLTranslator).GetMethod("IsRawColumnPassthroughMember", BindingFlags.Static | BindingFlags.NonPublic)!;

        Expression<Func<NestedOuter, int>> nested = o => o.Inner.Value;
        Assert.True((bool)method.Invoke(null, [nested.Body])!);

        Expression<Func<int>> fromMethod = () => MakeInner().Value;
        Assert.False((bool)method.Invoke(null, [fromMethod.Body])!);

        Expression<Func<int>> fromNew = () => new NestedInner().Value;
        Assert.False((bool)method.Invoke(null, [fromNew.Body])!);

        Expression<Func<int>> fromInit = () => new NestedInner { Value = 1 }.Value;
        Assert.False((bool)method.Invoke(null, [fromInit.Body])!);
    }

    [Fact]
    public void OsProviderSelectsByPlatform()
    {
        Assert.IsType<SQLite3Provider_winsqlite3>(SQLiteProviderInitializer.OsProvider(true));
        Assert.IsType<SQLite3Provider_sqlite3>(SQLiteProviderInitializer.OsProvider(false));
    }

    private static NestedInner MakeInner()
    {
        return new NestedInner();
    }
}
#endif
