namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Rejects LINQ overloads that take an <see cref="IEqualityComparer{T}" />, which SQLite cannot apply.
/// </summary>
internal static class ComparerArgumentGuard
{
    public static void ThrowIfComparer(MethodCallExpression node)
    {
        foreach (Expression argument in node.Arguments)
        {
            if (!IsEqualityComparerType(argument.Type))
            {
                continue;
            }

            if (ExpressionHelpers.IsConstant(argument) && ExpressionHelpers.GetConstantValue(argument) == null)
            {
                continue;
            }

            throw new NotSupportedException(
                $"{node.Method.Name} with an IEqualityComparer is not translatable to SQL. " +
                "SQLite compares values with its own collation, so a custom comparer cannot be applied in the database. " +
                "Remove the comparer, or materialize the sequence with ToList first and apply the comparer client-side.");
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "The comparer type comes from a user-supplied LINQ argument and is only inspected to throw a clear NotSupportedException.")]
    private static bool IsEqualityComparerType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
        {
            return true;
        }

        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEqualityComparer<>));
    }
}
