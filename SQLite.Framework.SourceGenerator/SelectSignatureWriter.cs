using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

/// <summary>
/// Walks a Select lambda at build time and writes the same signature string that
/// <c>SQLite.Framework.Internals.SelectSignature.Compute</c> writes at run time for the
/// same expression tree. The two strings must match so the runtime can find the method.
/// </summary>
internal static class SelectSignatureWriter
{
    public static string? TryCompute(ExpressionSyntax expression, SelectSignatureCtx ctx)
    {
        StringBuilder sb = new();
        if (!TryAppend(sb, expression, ctx))
        {
            return null;
        }

        return sb.ToString();
    }

    internal static ITypeSymbol? ResolveRangeVariableType(IRangeVariableSymbol rs, SemanticModel model)
    {
        if (rs.DeclaringSyntaxReferences.Length == 0)
        {
            return null;
        }

        SyntaxNode decl = rs.DeclaringSyntaxReferences[0].GetSyntax();
        return decl switch
        {
            FromClauseSyntax from => ElementTypeOf(model.GetTypeInfo(from.Expression).Type),
            JoinClauseSyntax join => ElementTypeOf(model.GetTypeInfo(join.InExpression).Type),
            _ => null
        };
    }

    private static ITypeSymbol? ElementTypeOf(ITypeSymbol? source) => source switch
    {
        INamedTypeSymbol nt when nt.IsGenericType => nt.TypeArguments.FirstOrDefault(),
        IArrayTypeSymbol ats => ats.ElementType,
        _ => null
    };

    private static bool TryAppend(StringBuilder sb, ExpressionSyntax? node, SelectSignatureCtx ctx)
    {
        if (node == null)
        {
            sb.Append("null");
            return true;
        }

        while (node is ParenthesizedExpressionSyntax paren)
        {
            node = paren.Expression;
        }

        if (node is IdentifierNameSyntax substIdent
            && ctx.Model.GetSymbolInfo(substIdent).Symbol is { } substSym
            && ctx.ParameterSubstitutions.TryGetValue(substSym, out ExpressionSyntax? substExpr))
        {
            return TryAppend(sb, substExpr, ctx);
        }

        TypeInfo typeInfo = ctx.Model.GetTypeInfo(node);
        ITypeSymbol? declaredType = typeInfo.Type;
        ITypeSymbol? convertedType = typeInfo.ConvertedType;
        if (declaredType != null
            && convertedType != null
            && !SymbolEqualityComparer.Default.Equals(declaredType, convertedType)
            && node is not LiteralExpressionSyntax
            && node is not CastExpressionSyntax)
        {
            Conversion conversion = ctx.Model.ClassifyConversion(node, convertedType);
            bool needsTreeConvert = conversion.IsBoxing
                || conversion.IsNumeric
                || conversion.IsUnboxing
                || conversion.IsUserDefined
                || conversion.IsNullable
                || (conversion.IsExplicit && !conversion.IsReference);
            if (needsTreeConvert)
            {
                sb.Append("(Convert ").Append(FormatType(convertedType, ctx.TypeArgSubstitutions)).Append(' ');
                if (!AppendWithType(sb, node, declaredType, ctx))
                {
                    return false;
                }
                sb.Append(')');
                return true;
            }
        }

        return AppendWithType(sb, node, convertedType ?? declaredType, ctx);
    }

    private static bool AppendWithType(StringBuilder sb, ExpressionSyntax node, ITypeSymbol? type, SelectSignatureCtx ctx)
    {

        switch (node)
        {
            case BinaryExpressionSyntax bin:
                return AppendBinary(sb, bin, type, ctx);

            case PrefixUnaryExpressionSyntax preUnary:
                return AppendPrefixUnary(sb, preUnary, type, ctx);

            case ConditionalExpressionSyntax cond:
                return AppendConditional(sb, cond, type, ctx);

            case MemberAccessExpressionSyntax memberAccess
                when memberAccess.Kind() == SyntaxKind.SimpleMemberAccessExpression:
                return AppendMemberAccess(sb, memberAccess, type, ctx);

            case InvocationExpressionSyntax invocation:
                return AppendInvocation(sb, invocation, type, ctx);

            case ObjectCreationExpressionSyntax objCreate:
                return AppendObjectCreation(sb, objCreate, type, ctx);

            case ImplicitObjectCreationExpressionSyntax implicitObj:
                return AppendImplicitObjectCreation(sb, implicitObj, type, ctx);

            case AnonymousObjectCreationExpressionSyntax anonObj:
                return AppendAnonymousObjectCreation(sb, anonObj, type, ctx);

            case ArrayCreationExpressionSyntax arrayCreate:
                return AppendArrayCreation(sb, arrayCreate, type, ctx);

            case ImplicitArrayCreationExpressionSyntax implicitArray:
                return AppendImplicitArrayCreation(sb, implicitArray, type, ctx);

            case IdentifierNameSyntax ident when TryGetRowBinding(ident, ctx, out RowBinding binding):
                AppendRowReference(sb, type, binding, ctx);
                return true;

            case IdentifierNameSyntax capturedIdent when IsCapturedValue(capturedIdent, ctx):
                AppendCapturedValue(sb, ctx.Model.GetTypeInfo(capturedIdent).Type ?? type, ctx);
                return true;

            case MemberAccessExpressionSyntax capturedMember when capturedMember.Kind() == SyntaxKind.SimpleMemberAccessExpression
                && IsCapturedValue(capturedMember, ctx):
                AppendCapturedValue(sb, ctx.Model.GetTypeInfo(capturedMember).Type ?? type, ctx);
                return true;

            case LiteralExpressionSyntax:
                AppendConstant(sb, type, ctx);
                return true;

            case CastExpressionSyntax cast:
                return AppendCast(sb, cast, type, ctx);

            case ElementAccessExpressionSyntax indexer:
                return AppendElementAccess(sb, indexer, type, ctx);
        }

        return false;
    }

    internal static bool IsCapturedValue(ExpressionSyntax node, SelectSignatureCtx ctx)
    {
        if (node.Parent is AssignmentExpressionSyntax assignment
            && assignment.Left == node
            && assignment.Parent is InitializerExpressionSyntax initializer
            && initializer.Kind() == SyntaxKind.ObjectInitializerExpression)
        {
            return false;
        }

        if (node.Parent is MemberAccessExpressionSyntax parentMa && parentMa.Name == node)
        {
            return false;
        }

        if (node.Parent is NameEqualsSyntax nameEquals && nameEquals.Name == node)
        {
            return false;
        }

        switch (node)
        {
            case IdentifierNameSyntax ident:
            {
                ISymbol? sym = ctx.Model.GetSymbolInfo(ident).Symbol;
                return sym switch
                {
                    ILocalSymbol => true,
                    IFieldSymbol { IsStatic: false } => true,
                    IPropertySymbol { IsStatic: false } => true,
                    _ => false
                };
            }
            case MemberAccessExpressionSyntax ma:
                ISymbol? member = ctx.Model.GetSymbolInfo(ma).Symbol;
                if (member is IFieldSymbol or IPropertySymbol)
                {
                    return IsCapturedValue(ma.Expression, ctx);
                }
                return false;
            case ThisExpressionSyntax:
                return true;
            default:
                return false;
        }
    }

    private static void AppendCapturedValue(StringBuilder sb, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        sb.Append("(CapturedValue ").Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(')');
    }

    private static bool AppendElementAccess(StringBuilder sb, ElementAccessExpressionSyntax indexer, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        ITypeSymbol? receiverType = ctx.Model.GetTypeInfo(indexer.Expression).Type;
        bool isArray = receiverType is IArrayTypeSymbol;

        if (isArray && indexer.ArgumentList.Arguments.Count == 1)
        {
            sb.Append("(ArrayIndex ").Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(' ');
            if (!TryAppend(sb, indexer.Expression, ctx))
            {
                return false;
            }
            sb.Append(' ');
            if (!TryAppend(sb, indexer.ArgumentList.Arguments[0].Expression, ctx))
            {
                return false;
            }
            sb.Append(')');
            return true;
        }

        if (ctx.Model.GetSymbolInfo(indexer).Symbol is IPropertySymbol { IsIndexer: true } prop
            && prop.GetMethod is IMethodSymbol getter)
        {
            sb.Append("(Call ").Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(' ')
                .Append(FormatType(getter.ContainingType, ctx.TypeArgSubstitutions)).Append('.').Append(getter.Name).Append(' ');
            if (!TryAppend(sb, indexer.Expression, ctx))
            {
                return false;
            }
            foreach (ArgumentSyntax arg in indexer.ArgumentList.Arguments)
            {
                sb.Append(' ');
                if (!TryAppend(sb, arg.Expression, ctx))
                {
                    return false;
                }
            }
            sb.Append(')');
            return true;
        }

        return false;
    }

    internal static bool TryGetRowBinding(IdentifierNameSyntax ident, SelectSignatureCtx ctx, out RowBinding binding)
    {
        ISymbol? sym = ctx.Model.GetSymbolInfo(ident).Symbol;
        if (sym != null && ctx.RowBindings.TryGetValue(sym, out binding))
        {
            return true;
        }

        binding = default;
        return false;
    }

    private static void AppendRowReference(StringBuilder sb, ITypeSymbol? exprType, RowBinding binding, SelectSignatureCtx ctx)
    {
        if (binding.MemberPath == null || binding.MemberPath.Count == 0)
        {
            sb.Append("(Parameter ").Append(FormatType(exprType, ctx.TypeArgSubstitutions)).Append(' ').Append(FormatType(ctx.OuterRowType, ctx.TypeArgSubstitutions)).Append(')');
            return;
        }

        for (int i = binding.MemberPath.Count - 1; i >= 0; i--)
        {
            ITypeSymbol type = binding.MemberPath[i].Type;
            sb.Append("(MemberAccess ").Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(' ').Append(binding.MemberPath[i].Name).Append(' ');
        }
        sb.Append("(Parameter ").Append(FormatType(ctx.OuterRowType, ctx.TypeArgSubstitutions)).Append(' ').Append(FormatType(ctx.OuterRowType, ctx.TypeArgSubstitutions)).Append(')');
        for (int i = 0; i < binding.MemberPath.Count; i++)
        {
            sb.Append(')');
        }
    }

    private static bool AppendBinary(StringBuilder sb, BinaryExpressionSyntax bin, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        string? nodeType = MapBinaryKind(bin.Kind());
        if (nodeType == null)
        {
            return false;
        }

        sb.Append('(').Append(nodeType).Append(' ').Append(FormatType(type, ctx.TypeArgSubstitutions));
        sb.Append(' ');
        if (!TryAppend(sb, bin.Left, ctx))
        {
            return false;
        }
        sb.Append(' ');
        if (!TryAppend(sb, bin.Right, ctx))
        {
            return false;
        }
        sb.Append(')');
        return true;
    }

    private static bool AppendPrefixUnary(StringBuilder sb, PrefixUnaryExpressionSyntax unary, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        string? nodeType = unary.Kind() switch
        {
            SyntaxKind.UnaryMinusExpression => "Negate",
            SyntaxKind.LogicalNotExpression => "Not",
            _ => null
        };

        if (nodeType == null)
        {
            return false;
        }

        sb.Append('(').Append(nodeType).Append(' ').Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(' ');
        if (!TryAppend(sb, unary.Operand, ctx))
        {
            return false;
        }
        sb.Append(')');
        return true;
    }

    private static bool AppendConditional(StringBuilder sb, ConditionalExpressionSyntax cond, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        sb.Append("(Conditional ").Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(' ');
        if (!TryAppend(sb, cond.Condition, ctx))
        {
            return false;
        }
        sb.Append(' ');
        if (!TryAppend(sb, cond.WhenTrue, ctx))
        {
            return false;
        }
        sb.Append(' ');
        if (!TryAppend(sb, cond.WhenFalse, ctx))
        {
            return false;
        }
        sb.Append(')');
        return true;
    }

    private static bool AppendMemberAccess(StringBuilder sb, MemberAccessExpressionSyntax access, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        string memberName = access.Name.Identifier.ValueText;

        ISymbol? memberSym = ctx.Model.GetSymbolInfo(access).Symbol;
        bool isStatic = memberSym switch
        {
            IPropertySymbol p => p.IsStatic,
            IFieldSymbol f => f.IsStatic,
            _ => false
        };

        if (isStatic)
        {
            sb.Append("(MemberAccess ").Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(' ').Append(memberName).Append(" null)");
            return true;
        }

        sb.Append("(MemberAccess ").Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(' ').Append(memberName).Append(' ');

        ITypeSymbol? memberContainingType = memberSym?.ContainingType;
        ITypeSymbol? receiverDeclaredType = ctx.Model.GetTypeInfo(access.Expression).Type;
        ITypeSymbol? substitutedReceiverType = Substitute(receiverDeclaredType, ctx.TypeArgSubstitutions);
        ITypeSymbol? substitutedMemberContainingType = Substitute(memberContainingType, ctx.TypeArgSubstitutions);
        bool insertReceiverConvert = substitutedReceiverType != null
            && substitutedMemberContainingType != null
            && !SymbolEqualityComparer.Default.Equals(substitutedReceiverType, substitutedMemberContainingType)
            && memberContainingType is INamedTypeSymbol mct
            && (mct.TypeKind == TypeKind.Interface
                || (substitutedReceiverType is ITypeParameterSymbol tp && tp.ConstraintTypes.Any(c => SymbolEqualityComparer.Default.Equals(c, mct))));

        if (insertReceiverConvert)
        {
            sb.Append("(Convert ").Append(FormatType(memberContainingType, ctx.TypeArgSubstitutions)).Append(' ');
            if (!TryAppend(sb, access.Expression, ctx))
            {
                return false;
            }
            sb.Append(')');
        }
        else if (!TryAppend(sb, access.Expression, ctx))
        {
            return false;
        }
        sb.Append(')');
        return true;
    }

    private static bool AppendInvocation(StringBuilder sb, InvocationExpressionSyntax invocation, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        if (ctx.Model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return false;
        }

        ExpressionSyntax? receiver = null;
        if (!method.IsStatic && invocation.Expression is MemberAccessExpressionSyntax ma)
        {
            receiver = ma.Expression;
        }

        ITypeSymbol? effectiveContainingType = ResolveContainingTypeForExpressionTree(method, receiver, ctx);

        sb.Append("(Call ").Append(FormatType(type, ctx.TypeArgSubstitutions));
        sb.Append(' ').Append(FormatType(effectiveContainingType, ctx.TypeArgSubstitutions)).Append('.').Append(method.Name);

        bool expandRowArgs = !IsFrameworkTranslatedMethod(method);

        if (receiver != null)
        {
            sb.Append(' ');
            if (expandRowArgs && IsRowLikeReference(receiver, ctx))
            {
                if (!AppendExpandedRow(sb, receiver, ctx))
                {
                    return false;
                }
            }
            else if (!TryAppend(sb, receiver, ctx))
            {
                return false;
            }
        }

        foreach (ArgumentSyntax arg in invocation.ArgumentList.Arguments)
        {
            sb.Append(' ');
            if (expandRowArgs && IsRowLikeReference(arg.Expression, ctx))
            {
                if (!AppendExpandedRow(sb, arg.Expression, ctx))
                {
                    return false;
                }
            }
            else if (!TryAppend(sb, arg.Expression, ctx))
            {
                return false;
            }
        }

        sb.Append(')');
        return true;
    }

    private static ITypeSymbol? ResolveContainingTypeForExpressionTree(IMethodSymbol method, ExpressionSyntax? receiver, SelectSignatureCtx ctx)
    {
        if (method.IsStatic || receiver == null || method.OverriddenMethod == null)
        {
            return method.ContainingType;
        }

        ITypeSymbol? receiverType = ctx.Model.GetTypeInfo(receiver).Type;
        if (receiverType is null)
        {
            return method.ContainingType;
        }

        if (receiverType.IsValueType && IsPrimitiveSpecialType(receiverType))
        {
            return method.ContainingType;
        }

        IMethodSymbol baseMethod = method;
        while (baseMethod.OverriddenMethod != null)
        {
            baseMethod = baseMethod.OverriddenMethod;
        }
        return baseMethod.ContainingType;
    }

    private static bool IsPrimitiveSpecialType(ITypeSymbol type)
    {
        return type.SpecialType is
            SpecialType.System_Boolean
            or SpecialType.System_Char
            or SpecialType.System_SByte
            or SpecialType.System_Byte
            or SpecialType.System_Int16
            or SpecialType.System_UInt16
            or SpecialType.System_Int32
            or SpecialType.System_UInt32
            or SpecialType.System_Int64
            or SpecialType.System_UInt64
            or SpecialType.System_IntPtr
            or SpecialType.System_UIntPtr
            or SpecialType.System_Single
            or SpecialType.System_Double;
    }

    private static bool AppendExpandedRow(StringBuilder sb, ExpressionSyntax rowExpr, SelectSignatureCtx ctx)
    {
        ITypeSymbol? type = ctx.Model.GetTypeInfo(rowExpr).Type;
        if (type is not INamedTypeSymbol rowType)
        {
            return false;
        }

        List<IPropertySymbol> props = GetRowProperties(rowType);
        if (props.Count == 0)
        {
            return false;
        }

        StringBuilder inner = new();
        if (!TryAppend(inner, rowExpr, ctx))
        {
            return false;
        }

        string innerSig = inner.ToString();

        sb.Append("(MemberInit ").Append(FormatType(rowType, ctx.TypeArgSubstitutions)).Append(' ');
        sb.Append("(New ").Append(FormatType(rowType, ctx.TypeArgSubstitutions)).Append(' ').Append(FormatType(rowType, ctx.TypeArgSubstitutions)).Append(')');
        foreach (IPropertySymbol prop in props)
        {
            sb.Append(' ').Append("Assignment").Append(':').Append(prop.Name).Append('=');
            sb.Append("(MemberAccess ").Append(FormatType(prop.Type, ctx.TypeArgSubstitutions)).Append(' ').Append(prop.Name).Append(' ');
            sb.Append(innerSig);
            sb.Append(')');
        }
        sb.Append(')');
        return true;
    }

    internal static bool IsRowLikeReference(ExpressionSyntax expr, SelectSignatureCtx ctx)
    {
        if (expr is IdentifierNameSyntax id && TryGetRowBinding(id, ctx, out _))
        {
            ITypeSymbol? t = ctx.Model.GetTypeInfo(id).Type;
            return t is INamedTypeSymbol nt && IsConstructibleEntityType(nt);
        }
        return false;
    }

    internal static List<IPropertySymbol> GetRowProperties(INamedTypeSymbol rowType)
    {
        List<IPropertySymbol> result = new();
        for (ITypeSymbol? t = rowType; t != null; t = t.BaseType)
        {
            foreach (ISymbol member in t.GetMembers())
            {
                if (member is IPropertySymbol prop
                    && !prop.IsStatic
                    && !prop.IsIndexer
                    && prop.SetMethod != null
                    && prop.DeclaredAccessibility == Accessibility.Public
                    && !result.Any(p => p.Name == prop.Name))
                {
                    result.Add(prop);
                }
            }
        }
        return result;
    }

    internal static bool IsConstructibleEntityType(INamedTypeSymbol type)
    {
        if (type.SpecialType is SpecialType.System_String or SpecialType.System_Decimal)
        {
            return false;
        }
        if (type.TypeKind == TypeKind.Enum)
        {
            return false;
        }
        if (type.IsValueType)
        {
            return false;
        }
        if (type.AllInterfaces.Any(i => i.ToDisplayString() == "System.Collections.IEnumerable"))
        {
            return false;
        }
        foreach (IMethodSymbol ctor in type.InstanceConstructors)
        {
            if (ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsFrameworkTranslatedMethod(IMethodSymbol method)
    {
        string? ns = method.ContainingType?.ContainingNamespace?.ToDisplayString();
        string? declType = method.ContainingType?.ToDisplayString();
        return declType is "System.Linq.Queryable" or "System.Linq.Enumerable"
            || ns == "SQLite.Framework.Extensions";
    }

    private static bool AppendObjectCreation(StringBuilder sb, ObjectCreationExpressionSyntax node, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        ITypeSymbol? declaredType = ctx.Model.GetTypeInfo(node.Type).Type;
        return AppendNewOrMemberInit(sb, declaredType, type, node.ArgumentList?.Arguments, node.Initializer, ctx);
    }

    private static bool AppendImplicitObjectCreation(StringBuilder sb, ImplicitObjectCreationExpressionSyntax node, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        ITypeSymbol? declaredType = type;
        return AppendNewOrMemberInit(sb, declaredType, type, node.ArgumentList.Arguments, node.Initializer, ctx);
    }

    private static bool AppendNewOrMemberInit(StringBuilder sb, ITypeSymbol? declaredType, ITypeSymbol? exprType, IReadOnlyList<ArgumentSyntax>? args, InitializerExpressionSyntax? initializer, SelectSignatureCtx ctx)
    {
        List<AssignmentExpressionSyntax>? memberBindings = null;
        if (initializer != null && initializer.Kind() == SyntaxKind.ObjectInitializerExpression)
        {
            memberBindings = initializer.Expressions.OfType<AssignmentExpressionSyntax>().ToList();
            if (memberBindings.Count != initializer.Expressions.Count)
            {
                return false;
            }
        }
        else if (initializer != null && initializer.Kind() == SyntaxKind.CollectionInitializerExpression)
        {
            int saved = sb.Length;
            if (AppendCollectionInitAsCapturedValue(sb, declaredType, exprType, args, initializer, ctx))
            {
                return true;
            }
            sb.Length = saved;
            return AppendListInitWithRowReferences(sb, declaredType, exprType, args, initializer, ctx);
        }

        bool isMemberInit = memberBindings != null && memberBindings.Count > 0;

        if (isMemberInit)
        {
            sb.Append("(MemberInit ").Append(FormatType(exprType, ctx.TypeArgSubstitutions)).Append(' ');
        }

        sb.Append("(New ").Append(FormatType(exprType, ctx.TypeArgSubstitutions)).Append(' ').Append(FormatType(declaredType, ctx.TypeArgSubstitutions));
        if (args != null)
        {
            foreach (ArgumentSyntax a in args)
            {
                sb.Append(' ');
                if (!TryAppend(sb, a.Expression, ctx))
                {
                    return false;
                }
            }
        }
        sb.Append(')');

        if (isMemberInit)
        {
            foreach (AssignmentExpressionSyntax assignment in memberBindings!)
            {
                if (assignment.Left is not IdentifierNameSyntax leftIdent)
                {
                    return false;
                }

                if (assignment.Right is InitializerExpressionSyntax init)
                {
                    if (init.Kind() == SyntaxKind.CollectionInitializerExpression)
                    {
                        sb.Append(' ').Append("ListBinding").Append(':').Append(leftIdent.Identifier.ValueText);
                        if (!AppendCollectionInitializerElements(sb, init, ctx))
                        {
                            return false;
                        }
                        continue;
                    }
                    if (init.Kind() == SyntaxKind.ObjectInitializerExpression)
                    {
                        sb.Append(' ').Append("MemberBinding").Append(':').Append(leftIdent.Identifier.ValueText);
                        if (!AppendNestedObjectInitializer(sb, init, ctx))
                        {
                            return false;
                        }
                        continue;
                    }
                }

                sb.Append(' ').Append("Assignment").Append(':').Append(leftIdent.Identifier.ValueText).Append('=');
                if (!TryAppend(sb, assignment.Right, ctx))
                {
                    return false;
                }
            }

            sb.Append(')');
        }

        return true;
    }

    private static bool AppendListInitWithRowReferences(StringBuilder sb, ITypeSymbol? declaredType, ITypeSymbol? exprType, IReadOnlyList<ArgumentSyntax>? args, InitializerExpressionSyntax initializer, SelectSignatureCtx ctx)
    {
        sb.Append("(ListInit ").Append(FormatType(exprType, ctx.TypeArgSubstitutions)).Append(' ');
        sb.Append("(New ").Append(FormatType(exprType, ctx.TypeArgSubstitutions)).Append(' ').Append(FormatType(declaredType, ctx.TypeArgSubstitutions));
        if (args != null)
        {
            foreach (ArgumentSyntax a in args)
            {
                sb.Append(' ');
                if (!TryAppend(sb, a.Expression, ctx))
                {
                    return false;
                }
            }
        }
        sb.Append(')');

        if (!AppendCollectionInitializerElements(sb, initializer, ctx))
        {
            return false;
        }

        sb.Append(')');
        return true;
    }

    private static bool AppendCollectionInitializerElements(StringBuilder sb, InitializerExpressionSyntax init, SelectSignatureCtx ctx)
    {
        sb.Append("=[");
        bool first = true;
        foreach (ExpressionSyntax elem in init.Expressions)
        {
            if (!first)
            {
                sb.Append(',');
            }
            first = false;

            if (elem is InitializerExpressionSyntax complex && complex.Kind() == SyntaxKind.ComplexElementInitializerExpression)
            {
                bool firstArg = true;
                foreach (ExpressionSyntax sub in complex.Expressions)
                {
                    if (!firstArg)
                    {
                        sb.Append('+');
                    }
                    firstArg = false;
                    if (!TryAppend(sb, sub, ctx))
                    {
                        return false;
                    }
                }
            }
            else if (!TryAppend(sb, elem, ctx))
            {
                return false;
            }
        }
        sb.Append(']');
        return true;
    }

    private static bool AppendNestedObjectInitializer(StringBuilder sb, InitializerExpressionSyntax init, SelectSignatureCtx ctx)
    {
        sb.Append("={");
        foreach (ExpressionSyntax elem in init.Expressions)
        {
            if (elem is not AssignmentExpressionSyntax sub || sub.Left is not IdentifierNameSyntax leftIdent)
            {
                return false;
            }

            if (sub.Right is InitializerExpressionSyntax inner)
            {
                if (inner.Kind() == SyntaxKind.CollectionInitializerExpression)
                {
                    sb.Append(' ').Append("ListBinding").Append(':').Append(leftIdent.Identifier.ValueText);
                    if (!AppendCollectionInitializerElements(sb, inner, ctx))
                    {
                        return false;
                    }
                    continue;
                }
                if (inner.Kind() == SyntaxKind.ObjectInitializerExpression)
                {
                    sb.Append(' ').Append("MemberBinding").Append(':').Append(leftIdent.Identifier.ValueText);
                    if (!AppendNestedObjectInitializer(sb, inner, ctx))
                    {
                        return false;
                    }
                    continue;
                }
            }

            sb.Append(' ').Append("Assignment").Append(':').Append(leftIdent.Identifier.ValueText).Append('=');
            if (!TryAppend(sb, sub.Right, ctx))
            {
                return false;
            }
        }
        sb.Append('}');
        return true;
    }

    private static bool AppendCollectionInitAsCapturedValue(StringBuilder sb, ITypeSymbol? declaredType, ITypeSymbol? exprType, IReadOnlyList<ArgumentSyntax>? args, InitializerExpressionSyntax initializer, SelectSignatureCtx ctx)
    {
        if (args != null)
        {
            foreach (ArgumentSyntax arg in args)
            {
                if (!IsStableConstantSubtree(arg.Expression, ctx))
                {
                    return false;
                }
            }
        }

        foreach (ExpressionSyntax elem in initializer.Expressions)
        {
            if (elem is InitializerExpressionSyntax nested && nested.Kind() == SyntaxKind.ComplexElementInitializerExpression)
            {
                foreach (ExpressionSyntax e in nested.Expressions)
                {
                    if (!IsStableConstantSubtree(e, ctx))
                    {
                        return false;
                    }
                }
            }
            else if (!IsStableConstantSubtree(elem, ctx))
            {
                return false;
            }
        }

        sb.Append("(CapturedValue ").Append(FormatType(exprType, ctx.TypeArgSubstitutions)).Append(')');
        return true;
    }

    internal static bool IsConstantCollectionInit(BaseObjectCreationExpressionSyntax node, SelectSignatureCtx ctx)
    {
        if (node.Initializer == null || node.Initializer.Kind() != SyntaxKind.CollectionInitializerExpression)
        {
            return false;
        }

        if (node.ArgumentList != null)
        {
            foreach (ArgumentSyntax a in node.ArgumentList.Arguments)
            {
                if (!IsStableConstantSubtree(a.Expression, ctx))
                {
                    return false;
                }
            }
        }

        foreach (ExpressionSyntax elem in node.Initializer.Expressions)
        {
            if (elem is InitializerExpressionSyntax nested && nested.Kind() == SyntaxKind.ComplexElementInitializerExpression)
            {
                foreach (ExpressionSyntax e in nested.Expressions)
                {
                    if (!IsStableConstantSubtree(e, ctx))
                    {
                        return false;
                    }
                }
            }
            else if (!IsStableConstantSubtree(elem, ctx))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsStableConstantSubtree(ExpressionSyntax expression, SelectSignatureCtx ctx)
    {
        while (expression is ParenthesizedExpressionSyntax paren)
        {
            expression = paren.Expression;
        }

        switch (expression)
        {
            case LiteralExpressionSyntax:
                return true;
            case IdentifierNameSyntax ident:
                if (TryGetRowBinding(ident, ctx, out _))
                {
                    return false;
                }
                return IsCapturedValue(ident, ctx) || ctx.Model.GetSymbolInfo(ident).Symbol is ITypeSymbol;
            case MemberAccessExpressionSyntax ma:
                return IsCapturedValue(ma, ctx) || IsStableConstantSubtree(ma.Expression, ctx);
            case CastExpressionSyntax cast:
                return IsStableConstantSubtree(cast.Expression, ctx);
            case PrefixUnaryExpressionSyntax pre:
                return IsStableConstantSubtree(pre.Operand, ctx);
            case BinaryExpressionSyntax bin:
                return IsStableConstantSubtree(bin.Left, ctx) && IsStableConstantSubtree(bin.Right, ctx);
            case ObjectCreationExpressionSyntax oc:
                if (oc.ArgumentList != null)
                {
                    foreach (ArgumentSyntax a in oc.ArgumentList.Arguments)
                    {
                        if (!IsStableConstantSubtree(a.Expression, ctx))
                        {
                            return false;
                        }
                    }
                }
                return oc.Initializer == null
                    || oc.Initializer.Expressions.All(e => IsStableConstantSubtree(e, ctx));
            case ImplicitObjectCreationExpressionSyntax ioc:
                foreach (ArgumentSyntax a in ioc.ArgumentList.Arguments)
                {
                    if (!IsStableConstantSubtree(a.Expression, ctx))
                    {
                        return false;
                    }
                }
                return ioc.Initializer == null
                    || ioc.Initializer.Expressions.All(e => IsStableConstantSubtree(e, ctx));
            default:
                return false;
        }
    }

    private static bool AppendArrayCreation(StringBuilder sb, ArrayCreationExpressionSyntax node, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        if (node.Initializer == null)
        {
            sb.Append("(NewArrayBounds ").Append(FormatType(type, ctx.TypeArgSubstitutions));
            foreach (ExpressionSyntax sizeExpr in node.Type.RankSpecifiers.SelectMany(r => r.Sizes).OfType<ExpressionSyntax>())
            {
                sb.Append(' ');
                if (!TryAppend(sb, sizeExpr, ctx))
                {
                    return false;
                }
            }
            sb.Append(')');
            return true;
        }

        return AppendArrayInit(sb, type, node.Initializer.Expressions, ctx);
    }

    private static bool AppendImplicitArrayCreation(StringBuilder sb, ImplicitArrayCreationExpressionSyntax node, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        return AppendArrayInit(sb, type, node.Initializer.Expressions, ctx);
    }

    private static bool AppendArrayInit(StringBuilder sb, ITypeSymbol? type, SeparatedSyntaxList<ExpressionSyntax> elements, SelectSignatureCtx ctx)
    {
        sb.Append("(NewArrayInit ").Append(FormatType(type, ctx.TypeArgSubstitutions));
        foreach (ExpressionSyntax el in elements)
        {
            sb.Append(' ');
            if (!TryAppend(sb, el, ctx))
            {
                return false;
            }
        }
        sb.Append(')');
        return true;
    }

    private static bool AppendAnonymousObjectCreation(StringBuilder sb, AnonymousObjectCreationExpressionSyntax node, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        List<string> memberNames = new();
        List<ExpressionSyntax> memberValues = new();

        foreach (AnonymousObjectMemberDeclaratorSyntax decl in node.Initializers)
        {
            string name;
            if (decl.NameEquals != null)
            {
                name = decl.NameEquals.Name.Identifier.ValueText;
            }
            else if (decl.Expression is MemberAccessExpressionSyntax ma)
            {
                name = ma.Name.Identifier.ValueText;
            }
            else if (decl.Expression is IdentifierNameSyntax id)
            {
                name = id.Identifier.ValueText;
            }
            else
            {
                return false;
            }

            memberNames.Add(name);
            memberValues.Add(decl.Expression);
        }

        ITypeSymbol? actualType = ctx.Model.GetTypeInfo(node).Type ?? type;
        sb.Append("(New ").Append(FormatType(actualType, ctx.TypeArgSubstitutions)).Append(' ').Append(FormatType(actualType, ctx.TypeArgSubstitutions));
        foreach (ExpressionSyntax val in memberValues)
        {
            sb.Append(' ');
            if (!TryAppend(sb, val, ctx))
            {
                return false;
            }
        }

        sb.Append(" members=[");
        for (int i = 0; i < memberNames.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append(memberNames[i]);
        }
        sb.Append("])");
        return true;
    }

    private static void AppendConstant(StringBuilder sb, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        sb.Append("(Constant ").Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(')');
    }

    private static bool AppendCast(StringBuilder sb, CastExpressionSyntax cast, ITypeSymbol? type, SelectSignatureCtx ctx)
    {
        sb.Append("(Convert ").Append(FormatType(type, ctx.TypeArgSubstitutions)).Append(' ');
        if (!TryAppend(sb, cast.Expression, ctx))
        {
            return false;
        }
        sb.Append(')');
        return true;
    }

    private static string? MapBinaryKind(SyntaxKind kind) => kind switch
    {
        SyntaxKind.AddExpression => "Add",
        SyntaxKind.SubtractExpression => "Subtract",
        SyntaxKind.MultiplyExpression => "Multiply",
        SyntaxKind.DivideExpression => "Divide",
        SyntaxKind.ModuloExpression => "Modulo",
        SyntaxKind.EqualsExpression => "Equal",
        SyntaxKind.NotEqualsExpression => "NotEqual",
        SyntaxKind.LessThanExpression => "LessThan",
        SyntaxKind.LessThanOrEqualExpression => "LessThanOrEqual",
        SyntaxKind.GreaterThanExpression => "GreaterThan",
        SyntaxKind.GreaterThanOrEqualExpression => "GreaterThanOrEqual",
        SyntaxKind.LogicalAndExpression => "AndAlso",
        SyntaxKind.LogicalOrExpression => "OrElse",
        SyntaxKind.BitwiseAndExpression => "And",
        SyntaxKind.BitwiseOrExpression => "Or",
        SyntaxKind.CoalesceExpression => "Coalesce",
        _ => null
    };

    internal static string FormatType(ITypeSymbol? symbol)
    {
        return FormatType(symbol, null);
    }

    internal static string FormatType(ITypeSymbol? symbol, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol>? substitutions)
    {
        symbol = Substitute(symbol, substitutions);

        if (symbol == null)
        {
            return "null";
        }

        if (symbol is IArrayTypeSymbol array)
        {
            return FormatType(array.ElementType, substitutions) + "[]";
        }

        if (symbol is INamedTypeSymbol anon && anon.IsAnonymousType)
        {
            if (anon.IsGenericType && anon.TypeArguments.Length > 0)
            {
                string args = string.Join(",", anon.TypeArguments.Select(t => FormatType(t, substitutions)));
                return "<anonymous<" + args + ">>";
            }

            if (anon.GetMembers().OfType<IPropertySymbol>().Any())
            {
                string args = string.Join(",", anon.GetMembers().OfType<IPropertySymbol>().Select(p => FormatType(p.Type, substitutions)));
                return "<anonymous<" + args + ">>";
            }

            return "<anonymous>";
        }

        if (symbol is INamedTypeSymbol named && named.IsGenericType)
        {
            INamedTypeSymbol def = named.ConstructedFrom;
            string defName = GetFullName(def);
            int tick = defName.IndexOf('`');
            if (tick >= 0)
            {
                defName = defName.Substring(0, tick);
            }

            string args = string.Join(",", named.TypeArguments.Select(t => FormatType(t, substitutions)));
            return defName + "<" + args + ">";
        }

        return GetFullName(symbol);
    }

    internal static ITypeSymbol? Substitute(ITypeSymbol? symbol, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol>? substitutions)
    {
        if (symbol == null || substitutions == null || substitutions.Count == 0)
        {
            return symbol;
        }

        if (symbol is ITypeParameterSymbol tp && substitutions.TryGetValue(tp, out ITypeSymbol? mapped))
        {
            return mapped;
        }

        if (symbol is IArrayTypeSymbol array)
        {
            ITypeSymbol? mappedElem = Substitute(array.ElementType, substitutions);
            return SymbolEqualityComparer.Default.Equals(mappedElem, array.ElementType)
                ? array
                : symbol;
        }

        if (symbol is INamedTypeSymbol named && named.IsGenericType && !named.IsAnonymousType)
        {
            ITypeSymbol[] args = named.TypeArguments.ToArray();
            bool changed = false;
            for (int i = 0; i < args.Length; i++)
            {
                ITypeSymbol? sub = Substitute(args[i], substitutions);
                if (sub != null && !SymbolEqualityComparer.Default.Equals(sub, args[i]))
                {
                    args[i] = sub;
                    changed = true;
                }
            }
            if (changed)
            {
                return named.OriginalDefinition.Construct(args);
            }
        }

        return symbol;
    }

    private static string GetFullName(ITypeSymbol symbol)
    {
        if (symbol.ContainingType != null)
        {
            return GetFullName(symbol.ContainingType) + "+" + symbol.Name;
        }

        string ns = symbol.ContainingNamespace is { IsGlobalNamespace: false } namespaceSymbol
            ? namespaceSymbol.ToDisplayString()
            : string.Empty;

        return string.IsNullOrEmpty(ns) ? symbol.Name : ns + "." + symbol.Name;
    }
}
