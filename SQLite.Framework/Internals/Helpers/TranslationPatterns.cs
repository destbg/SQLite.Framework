namespace SQLite.Framework.Internals.Helpers;

internal static class TranslationPatterns
{
    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="name" /> is a method name that produces
    /// a projected result and therefore acts as a select boundary in the LINQ chain.
    /// </summary>
    public static bool IsSelectMethodName(string name)
    {
        return name is
            nameof(Queryable.Select)
            or nameof(Queryable.Min)
            or nameof(Queryable.Max)
            or nameof(Queryable.Sum)
            or nameof(Queryable.Count)
            or nameof(Queryable.LongCount)
            or nameof(Queryable.Average)
            or nameof(Queryable.Contains)
            or nameof(QueryableExtensions.GroupConcatMarker)
            or nameof(QueryableExtensions.TotalMarker);
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="name" /> is a LINQ method that
    /// operates on a JSON array collection inside a projection.
    /// </summary>
    public static bool IsJsonCollectionMethod(string name)
    {
        return name is
            nameof(Enumerable.Where)
            or nameof(Enumerable.Select)
            or nameof(Enumerable.OrderBy)
            or nameof(Enumerable.OrderByDescending)
            or nameof(Enumerable.ThenBy)
            or nameof(Enumerable.ThenByDescending)
            or nameof(Enumerable.GroupBy)
            or nameof(Enumerable.SelectMany)
            or nameof(Enumerable.Skip)
            or nameof(Enumerable.Take)
            or nameof(Enumerable.First)
            or nameof(Enumerable.FirstOrDefault)
            or nameof(Enumerable.Last)
            or nameof(Enumerable.LastOrDefault)
            or nameof(Enumerable.Single)
            or nameof(Enumerable.SingleOrDefault)
            or nameof(Enumerable.Count)
            or nameof(Enumerable.LongCount)
            or nameof(Enumerable.Any)
            or nameof(Enumerable.All)
            or nameof(Enumerable.Min)
            or nameof(Enumerable.Max)
            or nameof(Enumerable.Sum)
            or nameof(Enumerable.Average)
            or nameof(Enumerable.Distinct)
            or nameof(Enumerable.Reverse)
            or nameof(Enumerable.ElementAt)
            or nameof(Enumerable.Contains);
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="name" /> is a terminal aggregation
    /// that consumes a JSON array window and must materialize it before applying the operation.
    /// </summary>
    public static bool IsWindowConsumer(string name)
    {
        return name is
            nameof(Enumerable.Count)
            or nameof(Enumerable.LongCount)
            or nameof(Enumerable.Any)
            or nameof(Enumerable.All)
            or nameof(Enumerable.Min)
            or nameof(Enumerable.Max)
            or nameof(Enumerable.Sum)
            or nameof(Enumerable.Average)
            or nameof(Enumerable.Contains);
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="name" /> is a static <see cref="Array" />
    /// method whose second argument is a predicate or converter lambda over individual elements.
    /// </summary>
    public static bool IsArrayLambdaMethod(string name)
    {
        return name is
            nameof(Array.Exists)
            or nameof(Array.Find)
            or nameof(Array.FindAll)
            or nameof(Array.FindIndex)
            or nameof(Array.FindLast)
            or nameof(Array.FindLastIndex)
            or nameof(Array.TrueForAll)
            or nameof(Array.ConvertAll);
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="name" /> is a <see cref="Math" /> method
    /// that requires SQLite 3.35 or later because it maps to a math extension function.
    /// </summary>
    public static bool IsMathExtensionFunction(string name)
    {
        return name is
            nameof(Math.Ceiling)
            or nameof(Math.Floor)
            or nameof(Math.Truncate)
            or nameof(Math.Pow)
            or nameof(Math.Sqrt)
            or nameof(Math.Exp)
            or nameof(Math.Log)
            or nameof(Math.Log10)
            or nameof(Math.Log2)
            or nameof(Math.Sin)
            or nameof(Math.Cos)
            or nameof(Math.Tan)
            or nameof(Math.Asin)
            or nameof(Math.Acos)
            or nameof(Math.Atan)
            or nameof(Math.Atan2)
            or nameof(Math.Sinh)
            or nameof(Math.Cosh)
            or nameof(Math.Tanh)
            or nameof(Math.Asinh)
            or nameof(Math.Acosh)
            or nameof(Math.Atanh)
            or nameof(Math.Cbrt);
    }

    /// <summary>
    /// Returns <see langword="true" /> when an expression of node type <paramref name="nodeType" />
    /// must be parenthesised when used as an operand of a string concatenation.
    /// </summary>
    public static bool IsConcatBracketNodeType(ExpressionType nodeType)
    {
        return nodeType is
            ExpressionType.Equal
            or ExpressionType.NotEqual
            or ExpressionType.GreaterThan
            or ExpressionType.LessThan
            or ExpressionType.GreaterThanOrEqual
            or ExpressionType.LessThanOrEqual
            or ExpressionType.AndAlso
            or ExpressionType.OrElse
            or ExpressionType.And
            or ExpressionType.Or
            or ExpressionType.ExclusiveOr;
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="type" /> is a numeric primitive that,
    /// when nullable, should be cast to text via SQL rather than reflected through .NET.
    /// </summary>
    public static bool IsNumericCastType(Type type)
    {
        return type == typeof(int)
            || type == typeof(long)
            || type == typeof(short)
            || type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(uint)
            || type == typeof(ulong)
            || type == typeof(ushort)
            || type == typeof(double)
            || type == typeof(float)
            || type == typeof(decimal);
    }
}
