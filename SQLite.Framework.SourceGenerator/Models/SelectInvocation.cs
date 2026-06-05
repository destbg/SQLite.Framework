using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// Describes a select invocation and its projection.
/// </summary>
public sealed class SelectInvocation
{
    /// <summary>
    /// Creates a new select invocation.
    /// </summary>
    public SelectInvocation(string signature, ExpressionSyntax body, SelectSignatureCtx writerCtx, SemanticModel model, INamedTypeSymbol projectionType)
    {
        Signature = signature;
        Body = body;
        WriterCtx = writerCtx;
        Model = model;
        ProjectionType = projectionType;
    }

    /// <summary>
    /// The signature string of the invocation.
    /// </summary>
    public string Signature { get; }

    /// <summary>
    /// The body of the select lambda.
    /// </summary>
    public ExpressionSyntax Body { get; }

    /// <summary>
    /// The select signature context for this invocation.
    /// </summary>
    public SelectSignatureCtx WriterCtx { get; }

    /// <summary>
    /// The semantic model for this invocation.
    /// </summary>
    public SemanticModel Model { get; }

    /// <summary>
    /// The projection type produced by the select.
    /// </summary>
    public INamedTypeSymbol ProjectionType { get; }
}
