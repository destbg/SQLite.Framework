using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator;

/// <summary>
/// A reference to an entity at an invocation site whose type argument is itself an open
/// type parameter (e.g. <c>ExecuteQuery&lt;T&gt;</c> inside <c>Repo&lt;T&gt;.Get</c>). The
/// type-parameter symbol is resolved against the generic-instantiation index in the
/// resolve stage to produce one closed <see cref="INamedTypeSymbol"/> per concrete
/// substitution observed at any callsite.
/// </summary>
internal sealed class UnresolvedGenericEntity
{
    public UnresolvedGenericEntity(ITypeParameterSymbol typeParameter)
    {
        TypeParameter = typeParameter;
    }

    public ITypeParameterSymbol TypeParameter { get; }
}
