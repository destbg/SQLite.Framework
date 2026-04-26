using SQLite.Framework.Tests.Enums;

namespace SQLite.Framework.Tests.Helpers;

internal static class InterceptorHelpers
{
    public static int Double(int x) => x * 2;
    public static string Identity(string s) => s;
    public static int IdentityInt(int x) => x;
    public static double IdentityDouble(double d) => d;
    public static PublisherType IdentityEnum(PublisherType x) => x;
}
