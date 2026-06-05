using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator.Writers;

/// <summary>
/// Closed type-argument tuples observed at every generic invocation and object creation
/// in the compilation. Used to monomorphize generic helper methods that wrap calls like
/// <c>ExecuteQuery&lt;T&gt;</c> or <c>Select(f =&gt; new TResult { ... })</c>.
/// </summary>
public sealed class GenericInstantiationIndex
{
    /// <summary>
    /// Closed type-argument tuples seen for each generic method.
    /// </summary>
    public Dictionary<IMethodSymbol, HashSet<ImmutableArray<INamedTypeSymbol>>> Methods { get; }
        = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Closed type-argument tuples seen for each generic type.
    /// </summary>
    public Dictionary<INamedTypeSymbol, HashSet<ImmutableArray<INamedTypeSymbol>>> Types { get; }
        = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Records a closed type-argument tuple for a method.
    /// </summary>
    public void AddMethod(IMethodSymbol method, ImmutableArray<INamedTypeSymbol> typeArgs)
    {
        if (!Methods.TryGetValue(method, out HashSet<ImmutableArray<INamedTypeSymbol>>? set))
        {
            set = new HashSet<ImmutableArray<INamedTypeSymbol>>(TypeArgTupleComparer.Instance);
            Methods[method] = set;
        }
        set.Add(typeArgs);
    }

    /// <summary>
    /// Records a closed type-argument tuple for a type.
    /// </summary>
    public void AddType(INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> typeArgs)
    {
        if (!Types.TryGetValue(type, out HashSet<ImmutableArray<INamedTypeSymbol>>? set))
        {
            set = new HashSet<ImmutableArray<INamedTypeSymbol>>(TypeArgTupleComparer.Instance);
            Types[type] = set;
        }
        set.Add(typeArgs);
    }

    /// <summary>
    /// Gets the recorded tuples for a method.
    /// </summary>
    public IEnumerable<ImmutableArray<INamedTypeSymbol>> GetMethodInstantiations(IMethodSymbol method)
    {
        return Methods.TryGetValue(method, out HashSet<ImmutableArray<INamedTypeSymbol>>? set)
            ? set
            : Array.Empty<ImmutableArray<INamedTypeSymbol>>();
    }

    /// <summary>
    /// Gets the recorded tuples for a type.
    /// </summary>
    public IEnumerable<ImmutableArray<INamedTypeSymbol>> GetTypeInstantiations(INamedTypeSymbol type)
    {
        return Types.TryGetValue(type, out HashSet<ImmutableArray<INamedTypeSymbol>>? set)
            ? set
            : Array.Empty<ImmutableArray<INamedTypeSymbol>>();
    }
}
