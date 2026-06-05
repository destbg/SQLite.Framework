using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator.Writers;

/// <summary>
/// Compares type-argument tuples by element symbol equality.
/// </summary>
public sealed class TypeArgTupleComparer : IEqualityComparer<ImmutableArray<INamedTypeSymbol>>
{
    /// <summary>
    /// The shared comparer instance.
    /// </summary>
    public static TypeArgTupleComparer Instance { get; } = new();

    /// <summary>
    /// Checks whether two tuples hold the same symbols in the same order.
    /// </summary>
    public bool Equals(ImmutableArray<INamedTypeSymbol> x, ImmutableArray<INamedTypeSymbol> y)
    {
        if (x.Length != y.Length)
        {
            return false;
        }
        for (int i = 0; i < x.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(x[i], y[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Gets a hash code for a tuple.
    /// </summary>
    public int GetHashCode(ImmutableArray<INamedTypeSymbol> obj)
    {
        int hash = 17;
        foreach (INamedTypeSymbol s in obj)
        {
            hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(s);
        }
        return hash;
    }
}
