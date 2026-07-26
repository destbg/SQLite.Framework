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
    /// Records a closed type-argument tuple for a method. Returns true when the tuple was not
    /// already known, which is what lets the forwarding closure detect that it has settled.
    /// </summary>
    public bool AddMethod(IMethodSymbol method, ImmutableArray<INamedTypeSymbol> typeArgs)
    {
        if (!Methods.TryGetValue(method, out HashSet<ImmutableArray<INamedTypeSymbol>>? set))
        {
            set = new HashSet<ImmutableArray<INamedTypeSymbol>>(TypeArgTupleComparer.Instance);
            Methods[method] = set;
        }
        return set.Add(typeArgs);
    }

    /// <summary>
    /// Records a closed type-argument tuple for a type. Returns true when the tuple was not
    /// already known, which is what lets the forwarding closure detect that it has settled.
    /// </summary>
    public bool AddType(INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> typeArgs)
    {
        if (!Types.TryGetValue(type, out HashSet<ImmutableArray<INamedTypeSymbol>>? set))
        {
            set = new HashSet<ImmutableArray<INamedTypeSymbol>>(TypeArgTupleComparer.Instance);
            Types[type] = set;
        }
        return set.Add(typeArgs);
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
    /// Gets the recorded tuples for a type. A generic base class is never constructed on its own,
    /// so the tuples of every recorded derived type are projected onto it as well.
    /// </summary>
    public IEnumerable<ImmutableArray<INamedTypeSymbol>> GetTypeInstantiations(INamedTypeSymbol type)
    {
        HashSet<ImmutableArray<INamedTypeSymbol>> result = new(TypeArgTupleComparer.Instance);
        if (Types.TryGetValue(type, out HashSet<ImmutableArray<INamedTypeSymbol>>? set))
        {
            foreach (ImmutableArray<INamedTypeSymbol> tuple in set)
            {
                result.Add(tuple);
            }
        }

        foreach (KeyValuePair<INamedTypeSymbol, HashSet<ImmutableArray<INamedTypeSymbol>>> entry in Types)
        {
            if (SymbolEqualityComparer.Default.Equals(entry.Key, type)
                || entry.Key.TypeParameters.Length == 0)
            {
                continue;
            }

            foreach (ImmutableArray<INamedTypeSymbol> tuple in entry.Value)
            {
                if (tuple.Length != entry.Key.TypeParameters.Length)
                {
                    continue;
                }

                INamedTypeSymbol constructed = entry.Key.Construct([.. tuple]);
                for (INamedTypeSymbol? current = constructed.BaseType; current != null; current = current.BaseType)
                {
                    if (!SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, type))
                    {
                        continue;
                    }

                    ImmutableArray<INamedTypeSymbol>.Builder builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
                    bool allClosed = true;
                    foreach (ITypeSymbol arg in current.TypeArguments)
                    {
                        if (arg is not INamedTypeSymbol named)
                        {
                            allClosed = false;
                            break;
                        }

                        builder.Add(named);
                    }

                    if (allClosed)
                    {
                        result.Add(builder.ToImmutable());
                    }
                }
            }
        }

        return result;
    }
}
