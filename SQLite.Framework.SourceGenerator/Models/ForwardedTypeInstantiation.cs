using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// A generic type usage whose type arguments are still open because they come from the type
/// parameters of the method or type that contains it (e.g. <c>new Store&lt;T&gt;(db)</c> inside
/// <c>Via&lt;T&gt;</c>). The closed tuples already known for the containing method or type are
/// substituted into <see cref="TypeArguments"/> so the target type is recorded as instantiated too.
/// </summary>
public sealed class ForwardedTypeInstantiation
{
    /// <summary>
    /// Creates a new forwarded type instantiation.
    /// </summary>
    public ForwardedTypeInstantiation(INamedTypeSymbol target, ImmutableArray<ITypeSymbol> typeArguments, IMethodSymbol? enclosingMethod, INamedTypeSymbol? enclosingType)
    {
        Target = target;
        TypeArguments = typeArguments;
        EnclosingMethod = enclosingMethod;
        EnclosingType = enclosingType;
    }

    /// <summary>
    /// The generic type being used.
    /// </summary>
    public INamedTypeSymbol Target { get; }

    /// <summary>
    /// The type arguments written at the usage site, at least one of which is still open.
    /// </summary>
    public ImmutableArray<ITypeSymbol> TypeArguments { get; }

    /// <summary>
    /// The method that contains the usage, when there is one.
    /// </summary>
    public IMethodSymbol? EnclosingMethod { get; }

    /// <summary>
    /// The type that contains the usage, when it is generic.
    /// </summary>
    public INamedTypeSymbol? EnclosingType { get; }
}
