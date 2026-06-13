using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SQLite.Framework.SourceGenerator.Helpers;
using SQLite.Framework.SourceGenerator.Models;

namespace SQLite.Framework.SourceGenerator.Writers;

/// <summary>
/// Rewrites a group-by key selector body so it compiles inside the generated file.
/// Enum constant references get their full type name and captured values become
/// indexed reads from the query context, in the same order the runtime collects them.
/// </summary>
public sealed class GroupKeyBodyRewriter : CSharpSyntaxRewriter
{
    private readonly SelectSignatureCtx ctx;
    private int capturedValueSlots;

    /// <summary>
    /// Creates a new rewriter for the given signature context.
    /// </summary>
    public GroupKeyBodyRewriter(SelectSignatureCtx ctx)
    {
        this.ctx = ctx;
    }

    /// <summary>
    /// Rewrites captured member reads and enum constant references.
    /// </summary>
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression
            && SelectSignatureWriter.IsCapturedValue(node, ctx))
        {
            return BuildCapturedValueExpression(ctx.Model.GetTypeInfo(node).Type);
        }

        if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression
            && ctx.Model.GetSymbolInfo(node).Symbol is IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum } enumField)
        {
            return SyntaxFactory.ParseExpression(
                SelectMaterializerEmitter.FormatType(enumField.ContainingType, ctx.TypeArgSubstitutions) + "." + enumField.Name);
        }

        return base.VisitMemberAccessExpression(node);
    }

    /// <summary>
    /// Rewrites captured local reads.
    /// </summary>
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (node.Parent is MemberAccessExpressionSyntax parentMa && parentMa.Name == node)
        {
            return node;
        }

        if (node.Parent is NameEqualsSyntax)
        {
            return node;
        }

        if (SelectSignatureWriter.IsCapturedValue(node, ctx))
        {
            return BuildCapturedValueExpression(ctx.Model.GetTypeInfo(node).Type);
        }

        return base.VisitIdentifierName(node);
    }

    /// <summary>
    /// Keeps the inferred member name when the member expression had to change.
    /// </summary>
    public override SyntaxNode? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
    {
        AnonymousObjectMemberDeclaratorSyntax visited = (AnonymousObjectMemberDeclaratorSyntax)base.VisitAnonymousObjectMemberDeclarator(node)!;
        if (visited.NameEquals != null || visited.Expression == node.Expression)
        {
            return visited;
        }

        string? inferredName = node.Expression switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText,
            _ => null
        };

        if (inferredName == null)
        {
            return visited;
        }

        return visited.WithNameEquals(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(inferredName)));
    }

    private ExpressionSyntax BuildCapturedValueExpression(ITypeSymbol? type)
    {
        int slot = capturedValueSlots++;
        string capturedType = type != null && SelectMaterializerEmitter.IsTypeAccessibleFromGenerator(type, ctx.Model.Compilation.Assembly)
            ? SelectMaterializerEmitter.FormatType(type, ctx.TypeArgSubstitutions)
            : "object";
        return SyntaxFactory.ParseExpression("((" + capturedType + ")ctx.CapturedValues![" + slot + "]!)");
    }
}
