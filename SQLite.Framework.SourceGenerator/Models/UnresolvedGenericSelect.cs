using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SQLite.Framework.SourceGenerator.Writers;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// A <c>Select</c> invocation whose projection type (and possibly types used inside the
/// lambda body) reference open type parameters from an enclosing generic method/class.
/// The resolve stage looks up concrete instantiations in the
/// <see cref="GenericInstantiationIndex"/> and emits one closed
/// <see cref="SelectInvocation"/> per substitution tuple.
/// </summary>
public sealed class UnresolvedGenericSelect
{
    /// <summary>
    /// Creates a new unresolved generic select.
    /// </summary>
    public UnresolvedGenericSelect(ExpressionSyntax body, SelectSignatureCtx baseCtx, SemanticModel model, ITypeParameterSymbol projectionParam, IMethodSymbol? enclosingMethod, INamedTypeSymbol? enclosingType)
    {
        Body = body;
        BaseCtx = baseCtx;
        Model = model;
        ProjectionParam = projectionParam;
        EnclosingMethod = enclosingMethod;
        EnclosingType = enclosingType;
    }

    /// <summary>
    /// The body of the select lambda.
    /// </summary>
    public ExpressionSyntax Body { get; }

    /// <summary>
    /// The base select signature context before resolving.
    /// </summary>
    public SelectSignatureCtx BaseCtx { get; }

    /// <summary>
    /// The semantic model for this invocation.
    /// </summary>
    public SemanticModel Model { get; }

    /// <summary>
    /// The open type parameter of the projection.
    /// </summary>
    public ITypeParameterSymbol ProjectionParam { get; }

    /// <summary>
    /// The enclosing generic method, if any.
    /// </summary>
    public IMethodSymbol? EnclosingMethod { get; }

    /// <summary>
    /// The enclosing generic type, if any.
    /// </summary>
    public INamedTypeSymbol? EnclosingType { get; }
}
