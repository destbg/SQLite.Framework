using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator;

/// <summary>
/// Closed type-argument tuples observed at every generic invocation and object creation
/// in the compilation. Used to monomorphize generic helper methods that wrap calls like
/// <c>ExecuteQuery&lt;T&gt;</c> or <c>Select(f =&gt; new TResult { ... })</c>.
/// </summary>
internal sealed class GenericInstantiationIndex
{
    public Dictionary<IMethodSymbol, HashSet<ImmutableArray<INamedTypeSymbol>>> Methods { get; }
        = new(SymbolEqualityComparer.Default);

    public Dictionary<INamedTypeSymbol, HashSet<ImmutableArray<INamedTypeSymbol>>> Types { get; }
        = new(SymbolEqualityComparer.Default);

    public void AddMethod(IMethodSymbol method, ImmutableArray<INamedTypeSymbol> typeArgs)
    {
        if (!Methods.TryGetValue(method, out HashSet<ImmutableArray<INamedTypeSymbol>>? set))
        {
            set = new HashSet<ImmutableArray<INamedTypeSymbol>>(TypeArgTupleComparer.Instance);
            Methods[method] = set;
        }
        set.Add(typeArgs);
    }

    public void AddType(INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> typeArgs)
    {
        if (!Types.TryGetValue(type, out HashSet<ImmutableArray<INamedTypeSymbol>>? set))
        {
            set = new HashSet<ImmutableArray<INamedTypeSymbol>>(TypeArgTupleComparer.Instance);
            Types[type] = set;
        }
        set.Add(typeArgs);
    }

    public IEnumerable<ImmutableArray<INamedTypeSymbol>> GetMethodInstantiations(IMethodSymbol method)
    {
        return Methods.TryGetValue(method, out HashSet<ImmutableArray<INamedTypeSymbol>>? set)
            ? set
            : Array.Empty<ImmutableArray<INamedTypeSymbol>>();
    }

    public IEnumerable<ImmutableArray<INamedTypeSymbol>> GetTypeInstantiations(INamedTypeSymbol type)
    {
        return Types.TryGetValue(type, out HashSet<ImmutableArray<INamedTypeSymbol>>? set)
            ? set
            : Array.Empty<ImmutableArray<INamedTypeSymbol>>();
    }

    private sealed class TypeArgTupleComparer : IEqualityComparer<ImmutableArray<INamedTypeSymbol>>
    {
        public static readonly TypeArgTupleComparer Instance = new();

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
}
