using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

internal sealed class FullyQualifiedRewriter : CSharpSyntaxRewriter
{
    private readonly EmitContext ctx;
    private int reflectedMethodSlots;
    private int capturedValueSlots;

    public FullyQualifiedRewriter(EmitContext ctx)
    {
        this.ctx = ctx;
    }

    public bool Failed { get; private set; }

    public override SyntaxNode? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
    {
        if (node.NameEquals == null)
        {
            string? inferredName = node.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText,
                _ => null
            };

            ExpressionSyntax? rewritten = (ExpressionSyntax?)Visit(node.Expression);
            if (rewritten == null)
            {
                return null;
            }

            if (inferredName != null)
            {
                return SyntaxFactory.AnonymousObjectMemberDeclarator(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(inferredName)),
                    rewritten);
            }

            return SyntaxFactory.AnonymousObjectMemberDeclarator(rewritten);
        }

        return base.VisitAnonymousObjectMemberDeclarator(node);
    }

    public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        SyntaxKind kind = node.Kind();
        if (kind == SyntaxKind.EqualsExpression || kind == SyntaxKind.NotEqualsExpression)
        {
            ExpressionSyntax? rangeSide = null;
            bool leftIsNull = node.Left.IsKind(SyntaxKind.NullLiteralExpression);
            bool rightIsNull = node.Right.IsKind(SyntaxKind.NullLiteralExpression);
            if (leftIsNull ^ rightIsNull)
            {
                rangeSide = leftIsNull ? node.Right : node.Left;
            }

            if (rangeSide is IdentifierNameSyntax rangeIdent
                && ctx.Model.GetSymbolInfo(rangeIdent).Symbol is { } rangeSym
                && ctx.NullableRangeFirstLeaf.TryGetValue(rangeSym, out int firstLeafIdx))
            {
                string varName = ctx.Leaves[firstLeafIdx].VarName;
                return SyntaxFactory.ParseExpression("global::System.Convert.ToBoolean(" + varName + ")");
            }
        }
        return base.VisitBinaryExpression(node);
    }

    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        if (IsCollectionInitConstant(node))
        {
            return BuildCapturedValueExpression(ctx.Model.GetTypeInfo(node).Type);
        }
        if (IsPrivateMemberInit(node))
        {
            return BuildPrivateMemberInitCall(node);
        }
        return base.VisitObjectCreationExpression(node);
    }

    public override SyntaxNode? VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
    {
        if (IsCollectionInitConstant(node))
        {
            return BuildCapturedValueExpression(ctx.Model.GetTypeInfo(node).Type);
        }
        if (IsPrivateMemberInit(node))
        {
            return BuildPrivateMemberInitCall(node);
        }
        return base.VisitImplicitObjectCreationExpression(node);
    }

    private bool IsPrivateMemberInit(BaseObjectCreationExpressionSyntax node)
    {
        if (node.Initializer == null || node.Initializer.Kind() != SyntaxKind.ObjectInitializerExpression)
        {
            return false;
        }
        ITypeSymbol? type = ctx.Model.GetTypeInfo(node).Type;
        return type != null && !SelectMaterializerEmitter.IsTypePubliclyReachable(type);
    }

    private ExpressionSyntax BuildPrivateMemberInitCall(BaseObjectCreationExpressionSyntax node)
    {
        int helperIndex = ctx.HelperCounter++;
        string helperName = ctx.OwnerMethodName + "__Private_" + helperIndex;
        BuildPrivateHelperMethod(helperName, node);

        StringBuilder call = new();
        call.Append(helperName).Append("(ctx");
        foreach (LeafInfo leaf in ctx.Leaves)
        {
            call.Append(", ").Append(leaf.VarName);
        }
        call.Append(')');
        return SyntaxFactory.ParseExpression(call.ToString());
    }

    private void BuildPrivateHelperMethod(string helperName, BaseObjectCreationExpressionSyntax node)
    {
        StringBuilder hb = ctx.HelperMethods;
        hb.Append("        private static object ").Append(helperName).Append("(SQLite.Framework.Models.SQLiteQueryContext ctx");
        foreach (LeafInfo leaf in ctx.Leaves)
        {
            hb.Append(", ").Append(SelectMaterializerEmitter.FormatType(leaf.Type)).Append(' ').Append(leaf.VarName);
        }
        hb.AppendLine(")");
        hb.AppendLine("        {");

        int typeSlot = ctx.TypeSlotCounter++;
        int memberSlotCounter = ctx.MemberSlotCounter;

        hb.Append("            object __inst = global::System.Activator.CreateInstance(ctx.ReflectedTypes![")
            .Append(typeSlot).AppendLine("])!;");

        InitializerExpressionSyntax initializer = node.Initializer!;
        int listVarCounter = 0;
        foreach (ExpressionSyntax expr in initializer.Expressions)
        {
            if (expr is not AssignmentExpressionSyntax assign || assign.Left is not IdentifierNameSyntax)
            {
                Failed = true;
                return;
            }

            int memberSlot = memberSlotCounter++;

            if (assign.Right is InitializerExpressionSyntax rhsInit
                && rhsInit.Kind() == SyntaxKind.CollectionInitializerExpression)
            {
                string listVar = "__list_" + listVarCounter++;
                hb.Append("            global::System.Collections.IList ").Append(listVar)
                    .Append(" = (global::System.Collections.IList)((global::System.Reflection.PropertyInfo)ctx.ReflectedMembers![")
                    .Append(memberSlot).AppendLine("]).GetValue(__inst)!;");
                foreach (ExpressionSyntax elem in rhsInit.Expressions)
                {
                    SyntaxNode? rewrittenElem = Visit(elem);
                    if (rewrittenElem is not ExpressionSyntax rewrittenExprElem)
                    {
                        Failed = true;
                        return;
                    }
                    hb.Append("            ").Append(listVar).Append(".Add(")
                        .Append(rewrittenExprElem.NormalizeWhitespace(indentation: "", eol: " ").ToFullString())
                        .AppendLine(");");
                }
                continue;
            }

            if (assign.Right is InitializerExpressionSyntax nestedObjInit
                && nestedObjInit.Kind() == SyntaxKind.ObjectInitializerExpression)
            {
                Failed = true;
                return;
            }

            SyntaxNode? rewrittenValue = Visit(assign.Right);
            if (rewrittenValue is not ExpressionSyntax rewrittenExpr)
            {
                Failed = true;
                return;
            }

            hb.Append("            ((global::System.Reflection.PropertyInfo)ctx.ReflectedMembers![")
                .Append(memberSlot)
                .Append("]).SetValue(__inst, ")
                .Append(rewrittenExpr.NormalizeWhitespace(indentation: "", eol: " ").ToFullString())
                .AppendLine(");");
        }

        ctx.MemberSlotCounter = memberSlotCounter;

        hb.AppendLine("            return __inst;");
        hb.AppendLine("        }");
        hb.AppendLine();
    }

    private bool IsCollectionInitConstant(BaseObjectCreationExpressionSyntax node)
    {
        if (node.Initializer == null || node.Initializer.Kind() != SyntaxKind.CollectionInitializerExpression)
        {
            return false;
        }
        return SelectSignatureWriter.IsConstantCollectionInit(node, ctx.WriterCtx);
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (ctx.LeafIndexBySyntax.TryGetValue(node, out int leafIndex))
        {
            LeafInfo leaf = ctx.Leaves[leafIndex];
            if (leaf.IsNullable)
            {
                return SyntaxFactory.ParseExpression("((" + SelectMaterializerEmitter.FormatType(leaf.Type) + ")" + leaf.VarName + "!)");
            }
            return SyntaxFactory.IdentifierName(leaf.VarName);
        }

        if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression
            && SelectSignatureWriter.IsCapturedValue(node, ctx.WriterCtx))
        {
            return BuildCapturedValueExpression(ctx.Model.GetTypeInfo(node).Type);
        }

        SymbolInfo info = ctx.Model.GetSymbolInfo(node);
        ISymbol? symbol = info.Symbol;

        if (symbol is ITypeSymbol typeSymbol)
        {
            return SyntaxFactory.ParseName(SelectMaterializerEmitter.FormatType(typeSymbol));
        }

        if (ctx.Model.GetSymbolInfo(node.Expression).Symbol is ITypeSymbol receiverType)
        {
            return SyntaxFactory.ParseExpression(SelectMaterializerEmitter.FormatType(receiverType) + "." + node.Name.ToString());
        }

        return base.VisitMemberAccessExpression(node);
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (ctx.RowExpansions.TryGetValue(node, out RowExpansion? expansion))
        {
            return BuildRowMaterialization(expansion);
        }

        ISymbol? identSym = ctx.Model.GetSymbolInfo(node).Symbol;
        if (identSym != null
            && ctx.WriterCtx.ParameterSubstitutions.TryGetValue(identSym, out ExpressionSyntax? substExpr))
        {
            return Visit(substExpr);
        }

        if (SelectSignatureWriter.IsCapturedValue(node, ctx.WriterCtx))
        {
            return BuildCapturedValueExpression(ctx.Model.GetTypeInfo(node).Type);
        }

        ISymbol? symbol = ctx.Model.GetSymbolInfo(node).Symbol;

        if (symbol is IParameterSymbol or IRangeVariableSymbol)
        {
            if (ctx.WriterCtx.RowBindings.ContainsKey(symbol))
            {
                Failed = true;
            }
            return node;
        }

        if (symbol is ITypeSymbol typeSymbol)
        {
            return SyntaxFactory.ParseName(SelectMaterializerEmitter.FormatType(typeSymbol));
        }

        if (symbol is IMethodSymbol { IsStatic: true } method && method.ContainingType != null)
        {
            return SyntaxFactory.ParseName(SelectMaterializerEmitter.FormatType(method.ContainingType) + "." + method.Name);
        }

        if (symbol is IFieldSymbol { IsStatic: true } field && field.ContainingType != null)
        {
            return SyntaxFactory.ParseName(SelectMaterializerEmitter.FormatType(field.ContainingType) + "." + field.Name);
        }

        if (symbol is IPropertySymbol { IsStatic: true } prop && prop.ContainingType != null)
        {
            return SyntaxFactory.ParseName(SelectMaterializerEmitter.FormatType(prop.ContainingType) + "." + prop.Name);
        }

        return base.VisitIdentifierName(node);
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (ctx.Model.GetSymbolInfo(node).Symbol is not IMethodSymbol method)
        {
            Failed = true;
            return node;
        }

        if (!SelectMaterializerEmitter.IsSymbolPubliclyReachable(method))
        {
            return BuildReflectedInvocation(node, method);
        }

        if (method.IsExtensionMethod && method.ReducedFrom != null)
        {
            if (node.Expression is not MemberAccessExpressionSyntax ma)
            {
                Failed = true;
                return node;
            }

            SyntaxNode? receiverVisited = Visit(ma.Expression);
            if (receiverVisited is not ExpressionSyntax receiverExpr)
            {
                Failed = true;
                return node;
            }

            List<ArgumentSyntax> args = new() { SyntaxFactory.Argument(receiverExpr) };
            foreach (ArgumentSyntax arg in node.ArgumentList.Arguments)
            {
                SyntaxNode? visitedArg = Visit(arg.Expression);
                if (visitedArg is not ExpressionSyntax argExpr)
                {
                    Failed = true;
                    return node;
                }
                args.Add(SyntaxFactory.Argument(argExpr));
            }

            IMethodSymbol reduced = method.ReducedFrom;
            string qualifiedTarget = SelectMaterializerEmitter.FormatType(reduced.ContainingType) + "." + reduced.Name;
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseExpression(qualifiedTarget),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args)));
        }

        return base.VisitInvocationExpression(node);
    }

    private ExpressionSyntax BuildReflectedInvocation(InvocationExpressionSyntax node, IMethodSymbol method)
    {
        int slot = reflectedMethodSlots++;

        ExpressionSyntax? receiverExpr = null;
        if (!method.IsStatic && node.Expression is MemberAccessExpressionSyntax recv)
        {
            if (Visit(recv.Expression) is ExpressionSyntax visited)
            {
                receiverExpr = visited;
            }
            else
            {
                Failed = true;
                return node;
            }
        }

        List<string> argExprs = new();
        foreach (ArgumentSyntax arg in node.ArgumentList.Arguments)
        {
            if (Visit(arg.Expression) is not ExpressionSyntax argExpr)
            {
                Failed = true;
                return node;
            }
            argExprs.Add(argExpr.NormalizeWhitespace(indentation: "", eol: " ").ToFullString());
        }

        string returnType = SelectMaterializerEmitter.FormatType(method.ReturnType);
        string instanceText = receiverExpr != null
            ? "(object?)(" + receiverExpr.NormalizeWhitespace(indentation: "", eol: " ").ToFullString() + ")"
            : "ctx.ReflectedMethodInstances![" + slot + "]";
        string argsText = argExprs.Count == 0
            ? "global::System.Array.Empty<object?>()"
            : "new object?[] { " + string.Join(", ", argExprs) + " }";

        string reflected = "((" + returnType + ")ctx.ReflectedMethods![" + slot + "].Invoke(" + instanceText + ", " + argsText + ")!)";
        return SyntaxFactory.ParseExpression(reflected);
    }

    private ExpressionSyntax BuildCapturedValueExpression(ITypeSymbol? type)
    {
        int slot = capturedValueSlots++;
        string capturedType = type != null ? SelectMaterializerEmitter.FormatType(type) : "object";
        return SyntaxFactory.ParseExpression("((" + capturedType + ")ctx.CapturedValues![" + slot + "]!)");
    }

    private ExpressionSyntax BuildRowMaterialization(RowExpansion expansion)
    {
        string typeText = SelectMaterializerEmitter.FormatType(expansion.RowType);
        StringBuilder sb = new();
        sb.Append("new ").Append(typeText).Append(" { ");
        for (int i = 0; i < expansion.Properties.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            sb.Append(expansion.Properties[i].Name).Append(" = ").Append(expansion.Leaves[i].VarName);
        }
        sb.Append(" }");
        return SyntaxFactory.ParseExpression(sb.ToString());
    }
}
