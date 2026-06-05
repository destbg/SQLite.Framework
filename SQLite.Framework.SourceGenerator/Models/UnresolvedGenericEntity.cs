using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// A reference to an entity at an invocation site whose type argument is itself an open
/// type parameter (e.g. <c>ExecuteQuery&lt;T&gt;</c> inside <c>Repo&lt;T&gt;.Get</c>). The
/// type-parameter symbol is resolved against the generic-instantiation index in the
/// resolve stage to produce one closed <see cref="INamedTypeSymbol"/> per concrete
/// substitution observed at any callsite.
/// </summary>
public sealed class UnresolvedGenericEntity
{
    /// <summary>
    /// Creates a new unresolved generic entity.
    /// </summary>
    public UnresolvedGenericEntity(ITypeParameterSymbol typeParameter)
    {
        TypeParameter = typeParameter;
    }

    /// <summary>
    /// The open type parameter to resolve later.
    /// </summary>
    public ITypeParameterSymbol TypeParameter { get; }
}
