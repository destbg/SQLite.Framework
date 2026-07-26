using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// A generic call whose type arguments are still open because they come from the type parameters of
/// the method or type that contains the call (e.g. <c>Core&lt;TResult&gt;(query)</c> inside
/// <c>Via&lt;TResult&gt;</c>). The closed tuples already known for the containing method or type are
/// substituted into <see cref="TypeArguments"/> so the target method is recorded as instantiated too.
/// </summary>
public sealed class ForwardedMethodInstantiation
{
    /// <summary>
    /// Creates a new forwarded instantiation.
    /// </summary>
    public ForwardedMethodInstantiation(IMethodSymbol target, ImmutableArray<ITypeSymbol> typeArguments, IMethodSymbol? enclosingMethod, INamedTypeSymbol? enclosingType)
    {
        Target = target;
        TypeArguments = typeArguments;
        EnclosingMethod = enclosingMethod;
        EnclosingType = enclosingType;
    }

    /// <summary>
    /// The generic method being called.
    /// </summary>
    public IMethodSymbol Target { get; }

    /// <summary>
    /// The type arguments written at the call site, at least one of which is still open.
    /// </summary>
    public ImmutableArray<ITypeSymbol> TypeArguments { get; }

    /// <summary>
    /// The method that contains the call, when there is one.
    /// </summary>
    public IMethodSymbol? EnclosingMethod { get; }

    /// <summary>
    /// The type that contains the call, when it is generic.
    /// </summary>
    public INamedTypeSymbol? EnclosingType { get; }
}
