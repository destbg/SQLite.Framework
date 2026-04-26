using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

/// <summary>
/// A <c>Select</c> invocation whose projection type (and possibly types used inside the
/// lambda body) reference open type parameters from an enclosing generic method/class.
/// The resolve stage looks up concrete instantiations in the
/// <see cref="GenericInstantiationIndex"/> and emits one closed
/// <see cref="SelectInvocation"/> per substitution tuple.
/// </summary>
internal sealed class UnresolvedGenericSelect
{
    public UnresolvedGenericSelect(ExpressionSyntax body, SelectSignatureCtx baseCtx, SemanticModel model, ITypeParameterSymbol projectionParam, IMethodSymbol? enclosingMethod, INamedTypeSymbol? enclosingType)
    {
        Body = body;
        BaseCtx = baseCtx;
        Model = model;
        ProjectionParam = projectionParam;
        EnclosingMethod = enclosingMethod;
        EnclosingType = enclosingType;
    }

    public ExpressionSyntax Body { get; }
    public SelectSignatureCtx BaseCtx { get; }
    public SemanticModel Model { get; }
    public ITypeParameterSymbol ProjectionParam { get; }
    public IMethodSymbol? EnclosingMethod { get; }
    public INamedTypeSymbol? EnclosingType { get; }
}
