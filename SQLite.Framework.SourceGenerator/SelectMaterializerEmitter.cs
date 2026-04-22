using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

/// <summary>
/// Writes the body of a method that builds one Select result from a SQLite row.
/// </summary>
internal static class SelectMaterializerEmitter
{
    public static bool TryEmit(StringBuilder sb, string methodName, ExpressionSyntax body, SelectSignatureCtx writerCtx)
    {
        if (body is IdentifierNameSyntax)
        {
            return false;
        }

        EmitContext emitCtx = new(writerCtx)
        {
            OwnerMethodName = methodName
        };
        if (!CollectLeaves(body, emitCtx))
        {
            return false;
        }

        if (emitCtx.Leaves.Count == 0)
        {
            return false;
        }

        string? bodyText = RewriteBody(body, emitCtx);
        if (bodyText == null)
        {
            return false;
        }

        sb.Append("        private static object? ").Append(methodName).AppendLine("(SQLite.Framework.Models.SQLiteQueryContext ctx)");
        sb.AppendLine("        {");
        sb.AppendLine("            var reader = ctx.Reader!;");

        for (int i = 0; i < emitCtx.Leaves.Count; i++)
        {
            LeafInfo leaf = emitCtx.Leaves[i];
            string typeText = FormatType(leaf.Type);
            string typeOfText = FormatType(StripNullableSymbol(leaf.Type));

            if (leaf.IsNullable)
            {
                sb.Append("            object? ").Append(leaf.VarName).Append(" = reader.GetValue(").Append(i)
                    .Append(", reader.GetColumnType(").Append(i).Append("), typeof(")
                    .Append(typeOfText).AppendLine("));");
            }
            else
            {
                sb.Append("            ")
                    .Append(typeText).Append(' ').Append(leaf.VarName).Append(" = (")
                    .Append(typeText).Append(")reader.GetValue(").Append(i)
                    .Append(", reader.GetColumnType(").Append(i).Append("), typeof(")
                    .Append(typeOfText).AppendLine("))!;");
            }
        }

        sb.Append("            return ").Append(bodyText).AppendLine(";");
        sb.AppendLine("        }");
        sb.AppendLine();

        if (emitCtx.HelperMethods.Length > 0)
        {
            sb.Append(emitCtx.HelperMethods);
        }
        return true;
    }

    private static bool CollectLeaves(SyntaxNode node, EmitContext ctx)
    {
        if (node is IdentifierNameSyntax substIdent
            && ctx.Model.GetSymbolInfo(substIdent).Symbol is { } substSym
            && ctx.WriterCtx.ParameterSubstitutions.TryGetValue(substSym, out ExpressionSyntax? substExpr))
        {
            return CollectLeaves(substExpr, ctx);
        }

        if (node is BaseObjectCreationExpressionSyntax boc
            && SelectSignatureWriter.IsConstantCollectionInit(boc, ctx.WriterCtx))
        {
            return true;
        }

        if (node is BinaryExpressionSyntax bin
            && (bin.Kind() == SyntaxKind.EqualsExpression || bin.Kind() == SyntaxKind.NotEqualsExpression))
        {
            IdentifierNameSyntax? rangeIdent = null;
            if (bin.Left is IdentifierNameSyntax li && bin.Right.IsKind(SyntaxKind.NullLiteralExpression)) rangeIdent = li;
            else if (bin.Right is IdentifierNameSyntax ri && bin.Left.IsKind(SyntaxKind.NullLiteralExpression)) rangeIdent = ri;

            if (rangeIdent != null
                && ctx.Model.GetSymbolInfo(rangeIdent).Symbol is { } binRangeSym
                && ctx.WriterCtx.NullableRangeVars.Contains(binRangeSym)
                && !ctx.NullableRangeFirstLeaf.ContainsKey(binRangeSym)
                && ctx.Model.GetTypeInfo(rangeIdent).Type is INamedTypeSymbol rangeType)
            {
                List<IPropertySymbol> props = SelectSignatureWriter.GetRowProperties(rangeType);
                if (props.Count > 0)
                {
                    int idx = ctx.Leaves.Count;
                    string varName = "__leaf_" + idx;
                    ctx.Leaves.Add(new LeafInfo(rangeIdent, props[0].Type, varName, isNullable: true));
                    ctx.NullableRangeFirstLeaf[binRangeSym] = idx;
                }
            }
        }

        if (node is BaseObjectCreationExpressionSyntax privBoc
            && privBoc.Initializer != null
            && privBoc.Initializer.Kind() == SyntaxKind.ObjectInitializerExpression
            && ctx.Model.GetTypeInfo(privBoc).Type is ITypeSymbol privType
            && !IsTypePubliclyReachable(privType))
        {
            foreach (ExpressionSyntax initExpr in privBoc.Initializer.Expressions)
            {
                if (initExpr is AssignmentExpressionSyntax assign)
                {
                    if (!CollectLeaves(assign.Right, ctx))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        switch (node)
        {
            case MemberAccessExpressionSyntax access when access.Kind() == SyntaxKind.SimpleMemberAccessExpression:
                if (access.Expression is IdentifierNameSyntax rowIdent && IsRowReference(rowIdent, ctx))
                {
                    ITypeSymbol? leafType = ctx.Model.GetTypeInfo(access).ConvertedType ?? ctx.Model.GetTypeInfo(access).Type;
                    if (leafType == null || !IsTypePubliclyReachable(leafType))
                    {
                        return false;
                    }

                    bool isNullable = IsNullableRangeVarIdentifier(rowIdent, ctx);
                    int idx = ctx.Leaves.Count;
                    string varName = "__leaf_" + idx;
                    ctx.Leaves.Add(new LeafInfo(access, leafType, varName, isNullable));
                    ctx.LeafIndexBySyntax[access] = idx;
                    if (isNullable && ctx.Model.GetSymbolInfo(rowIdent).Symbol is { } rowSym)
                    {
                        if (!ctx.NullableRangeFirstLeaf.ContainsKey(rowSym))
                        {
                            ctx.NullableRangeFirstLeaf[rowSym] = idx;
                        }
                    }
                    return true;
                }

                if (!IsSymbolPubliclyReachable(ctx.Model.GetSymbolInfo(access).Symbol))
                {
                    return false;
                }

                return CollectLeaves(access.Expression, ctx);

            case IdentifierNameSyntax ident:
                if (ident.Parent is NameEqualsSyntax ne && ne.Name == ident)
                {
                    return true;
                }
                if (ident.Parent is MemberAccessExpressionSyntax parentMa && parentMa.Name == ident)
                {
                    return true;
                }
                if (ident.Parent is AssignmentExpressionSyntax assignParent
                    && assignParent.Left == ident
                    && assignParent.Parent is InitializerExpressionSyntax initExpr
                    && initExpr.Kind() == SyntaxKind.ObjectInitializerExpression)
                {
                    return true;
                }

                ISymbol? sym = ctx.Model.GetSymbolInfo(ident).Symbol;
                if (sym is IParameterSymbol or IRangeVariableSymbol)
                {
                    if (ctx.WriterCtx.NullableRangeVars.Contains(sym))
                    {
                        return true;
                    }
                    return !ctx.WriterCtx.RowBindings.ContainsKey(sym);
                }

                return IsSymbolPubliclyReachable(sym);

            case InvocationExpressionSyntax invoke:
                if (ctx.Model.GetSymbolInfo(invoke).Symbol is not IMethodSymbol method)
                {
                    return false;
                }

                bool expand = !IsFrameworkTranslatedMethod(method);
                if (expand)
                {
                    if (invoke.Expression is MemberAccessExpressionSyntax recvMa
                        && SelectSignatureWriter.IsRowLikeReference(recvMa.Expression, ctx.WriterCtx))
                    {
                        if (!RegisterRowExpansion(recvMa.Expression, ctx))
                        {
                            return false;
                        }
                    }

                    foreach (ArgumentSyntax arg in invoke.ArgumentList.Arguments)
                    {
                        if (SelectSignatureWriter.IsRowLikeReference(arg.Expression, ctx.WriterCtx))
                        {
                            if (!RegisterRowExpansion(arg.Expression, ctx))
                            {
                                return false;
                            }
                            continue;
                        }

                        if (!CollectLeaves(arg.Expression, ctx))
                        {
                            return false;
                        }
                    }

                    if (invoke.Expression is MemberAccessExpressionSyntax recv2
                        && !SelectSignatureWriter.IsRowLikeReference(recv2.Expression, ctx.WriterCtx))
                    {
                        if (!CollectLeaves(recv2.Expression, ctx))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                break;

            case ParenthesizedExpressionSyntax paren:
                return CollectLeaves(paren.Expression, ctx);
        }

        foreach (SyntaxNode child in node.ChildNodes())
        {
            if (!CollectLeaves(child, ctx))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsRowReference(IdentifierNameSyntax ident, EmitContext ctx)
    {
        return SelectSignatureWriter.TryGetRowBinding(ident, ctx.WriterCtx, out _);
    }

    private static bool IsNullableRangeVarIdentifier(IdentifierNameSyntax ident, EmitContext ctx)
    {
        ISymbol? sym = ctx.Model.GetSymbolInfo(ident).Symbol;
        return sym != null && ctx.WriterCtx.NullableRangeVars.Contains(sym);
    }

    private static bool RegisterRowExpansion(ExpressionSyntax rowExpr, EmitContext ctx)
    {
        ITypeSymbol? type = ctx.Model.GetTypeInfo(rowExpr).Type;
        if (type is not INamedTypeSymbol rowType)
        {
            return false;
        }

        List<IPropertySymbol> props = SelectSignatureWriter.GetRowProperties(rowType);
        if (props.Count == 0)
        {
            return false;
        }

        List<LeafInfo> propLeaves = new();
        foreach (IPropertySymbol prop in props)
        {
            if (!IsTypePubliclyReachable(prop.Type))
            {
                return false;
            }

            int idx = ctx.Leaves.Count;
            string varName = "__leaf_" + idx;
            LeafInfo leaf = new(rowExpr, prop.Type, varName);
            ctx.Leaves.Add(leaf);
            propLeaves.Add(leaf);
        }

        ctx.RowExpansions[rowExpr] = new RowExpansion(rowType, props, propLeaves);
        return true;
    }

    private static bool IsFrameworkTranslatedMethod(IMethodSymbol method)
    {
        string? declType = method.ContainingType?.ToDisplayString();
        string? ns = method.ContainingType?.ContainingNamespace?.ToDisplayString();
        return declType is "System.Linq.Queryable" or "System.Linq.Enumerable"
            || ns == "SQLite.Framework.Extensions";
    }

    private static string? RewriteBody(ExpressionSyntax expression, EmitContext ctx)
    {
        FullyQualifiedRewriter rewriter = new(ctx);
        SyntaxNode? rewritten = rewriter.Visit(expression);
        if (rewriter.Failed || rewritten == null)
        {
            return null;
        }

        return rewritten.NormalizeWhitespace(indentation: "", eol: " ").ToFullString();
    }

    internal static bool IsSymbolPubliclyReachable(ISymbol? symbol)
    {
        if (symbol == null)
        {
            return false;
        }

        for (ISymbol? current = symbol; current != null; current = current.ContainingSymbol)
        {
            if (current is INamespaceSymbol)
            {
                return true;
            }

            if (current is IParameterSymbol || current is ILocalSymbol || current is IRangeVariableSymbol)
            {
                current = current.ContainingSymbol;
                continue;
            }

            if (current.DeclaredAccessibility != Accessibility.Public
                && current.DeclaredAccessibility != Accessibility.NotApplicable)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsTypePubliclyReachable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol nullable
            && nullable.IsGenericType
            && nullable.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
            && nullable.TypeArguments.Length == 1)
        {
            return IsTypePubliclyReachable(nullable.TypeArguments[0]);
        }

        if (type is IArrayTypeSymbol array)
        {
            return IsTypePubliclyReachable(array.ElementType);
        }

        if (type is INamedTypeSymbol named)
        {
            foreach (ITypeSymbol arg in named.TypeArguments)
            {
                if (!IsTypePubliclyReachable(arg))
                {
                    return false;
                }
            }
        }

        return IsSymbolPubliclyReachable(type);
    }

    internal static string FormatType(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    internal static ITypeSymbol StripNullableSymbol(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named
            && named.IsGenericType
            && named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0];
        }

        return type;
    }
}
