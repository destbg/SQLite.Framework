using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

internal sealed class SelectInvocation
{
    public SelectInvocation(string signature, ExpressionSyntax body, SelectSignatureCtx writerCtx, SemanticModel model, INamedTypeSymbol projectionType)
    {
        Signature = signature;
        Body = body;
        WriterCtx = writerCtx;
        Model = model;
        ProjectionType = projectionType;
    }

    public string Signature { get; }
    public ExpressionSyntax Body { get; }
    public SelectSignatureCtx WriterCtx { get; }
    public SemanticModel Model { get; }
    public INamedTypeSymbol ProjectionType { get; }
}
