namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Reports whether a lambda body references its first parameter.
/// </summary>
internal static class ParameterUsageFinder
{
    public static bool Uses(LambdaExpression lambda)
    {
        ParameterUsageFinderVisitor finder = new(lambda.Parameters[0]);
        finder.Visit(lambda.Body);
        return finder.Found;
    }
}
