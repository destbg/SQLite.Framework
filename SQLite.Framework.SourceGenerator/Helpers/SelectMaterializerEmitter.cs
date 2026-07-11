using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SQLite.Framework.SourceGenerator.Models;
using SQLite.Framework.SourceGenerator.Writers;

namespace SQLite.Framework.SourceGenerator.Helpers;

/// <summary>
/// Writes the body of a method that builds one Select result from a SQLite row.
/// </summary>
public static class SelectMaterializerEmitter
{
    /// <summary>
    /// Emits a materializer method body that builds one Select result from a row.
    /// </summary>
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

        string? bodyText;
        if (body is ImplicitArrayCreationExpressionSyntax or ArrayCreationExpressionSyntax)
        {
            bodyText = CollectArrayLeaves(body, emitCtx);
            if (bodyText == null)
            {
                return false;
            }
        }
        else
        {
            if (!CollectLeaves(body, emitCtx))
            {
                return false;
            }

            bool hasReflectedLeaf = emitCtx.Leaves.Any(l => l.IsReflected);
            bool anonHasNonAccessibleArg = body is AnonymousObjectCreationExpressionSyntax anonForCheck
                && anonForCheck.Initializers.Any(init =>
                {
                    ITypeSymbol? argType = writerCtx.Model.GetTypeInfo(init.Expression).Type;
                    return argType != null && !IsTypeAccessibleFromGenerator(argType, emitCtx.GeneratorAssembly);
                });
            bool anonCtorPath = (hasReflectedLeaf || anonHasNonAccessibleArg)
                && body is AnonymousObjectCreationExpressionSyntax;

            if (anonCtorPath)
            {
                AnonymousObjectCreationExpressionSyntax anonExpr = (AnonymousObjectCreationExpressionSyntax)body;
                FullyQualifiedRewriter rewriter = new(emitCtx);

                StringBuilder args = new();
                args.Append("ctx.ReflectedConstructors![0].Invoke(new object?[] { ");
                int leafCursor = 0;
                for (int i = 0; i < anonExpr.Initializers.Count; i++)
                {
                    if (i > 0)
                    {
                        args.Append(", ");
                    }

                    ExpressionSyntax initExpr = anonExpr.Initializers[i].Expression;
                    int initLeafCount = CountLeavesUnder(emitCtx.Leaves, leafCursor, initExpr);
                    ITypeSymbol? initType = writerCtx.Model.GetTypeInfo(initExpr).Type;
                    bool initIsInaccessibleBoc = initExpr is BaseObjectCreationExpressionSyntax
                        && initType != null
                        && !IsTypeAccessibleFromGenerator(initType, emitCtx.GeneratorAssembly);

                    if (initIsInaccessibleBoc)
                    {
                        SyntaxNode? rewritten = rewriter.Visit(initExpr);
                        if (rewriter.Failed || rewritten is not ExpressionSyntax rewrittenExpr)
                        {
                            return false;
                        }
                        args.Append(rewrittenExpr.NormalizeWhitespace(indentation: "", eol: " ").ToFullString());
                    }
                    else if (initLeafCount == 1)
                    {
                        args.Append(emitCtx.Leaves[leafCursor].VarName);
                    }
                    else
                    {
                        return false;
                    }

                    leafCursor += initLeafCount;
                }
                args.Append(" })");
                bodyText = args.ToString();
            }
            else
            {
                bodyText = RewriteBody(body, emitCtx);
                if (bodyText == null)
                {
                    return false;
                }
            }
        }

        foreach (LeafInfo guardLeaf in emitCtx.Leaves)
        {
            if (!guardLeaf.IsReflected && ContainsAnonymousType(guardLeaf.Type))
            {
                return false;
            }
        }

        sb.Append("        private static object? ").Append(methodName).AppendLine("(SQLite.Framework.Models.SQLiteQueryContext ctx)");
        sb.AppendLine("        {");
        if (emitCtx.Leaves.Count > 0)
        {
            sb.AppendLine("            var reader = ctx.Reader!;");
        }

        int reflectedLeafIndex = 0;
        for (int i = 0; i < emitCtx.Leaves.Count; i++)
        {
            LeafInfo leaf = emitCtx.Leaves[i];
            string typeOfText;
            if (leaf.IsReflected)
            {
                typeOfText = "ctx.ReflectedTypes![" + reflectedLeafIndex + "]";
                reflectedLeafIndex++;
            }
            else
            {
                typeOfText = "typeof(" + FormatType(StripNullableSymbol(leaf.Type), writerCtx.TypeArgSubstitutions) + ")";
            }

            if (!leaf.IsReflected
                && IsNullableValueType(leaf.Type)
                && TryGetFastPathAccessor(leaf.Type, out string? vnAccessor, out bool vnHandlesNull)
                && !vnHandlesNull)
            {
                string nullableTypeText = FormatType(leaf.Type, writerCtx.TypeArgSubstitutions);
                sb.Append("            ").Append(nullableTypeText).Append(' ').Append(leaf.VarName).AppendLine(";");
                sb.Append("            if (reader.HasConverter(").Append(typeOfText).AppendLine("))");
                sb.Append("                ").Append(leaf.VarName).Append(" = (").Append(nullableTypeText).Append(")reader.GetValue(").Append(i).Append(", reader.GetColumnType(").Append(i).Append("), ").Append(typeOfText).AppendLine(");");
                sb.AppendLine("            else");
                sb.Append("                ").Append(leaf.VarName)
                    .Append(" = reader.IsDBNull(").Append(i).Append(") ? (").Append(nullableTypeText).Append(")null : ")
                    .Append("reader.").Append(vnAccessor).Append("(").Append(i).AppendLine(");");
            }
            else if (leaf.IsNullable || leaf.IsReflected)
            {
                if (TryGetFastPathAccessor(leaf.Type, out string? nAccessor, out bool nHandlesNull))
                {
                    sb.Append("            object? ").Append(leaf.VarName).AppendLine(";");
                    sb.Append("            if (reader.HasConverter(").Append(typeOfText).AppendLine("))");
                    sb.Append("                ").Append(leaf.VarName).Append(" = reader.GetValue(").Append(i).Append(", reader.GetColumnType(").Append(i).Append("), ").Append(typeOfText).AppendLine(");");
                    sb.AppendLine("            else");
                    if (nHandlesNull)
                    {
                        sb.Append("                ").Append(leaf.VarName).Append(" = ")
                            .Append("reader.").Append(nAccessor).Append("(").Append(i).AppendLine(");");
                    }
                    else
                    {
                        sb.Append("                ").Append(leaf.VarName)
                            .Append(" = reader.IsDBNull(").Append(i).Append(") ? null : (object)")
                            .Append("reader.").Append(nAccessor).Append("(").Append(i).AppendLine(");");
                    }
                }
                else
                {
                    sb.Append("            object? ").Append(leaf.VarName).Append(" = reader.GetValue(").Append(i)
                        .Append(", reader.GetColumnType(").Append(i).Append("), ")
                        .Append(typeOfText).AppendLine(");");
                }
            }
            else if (TryGetFastPathAccessor(leaf.Type, out string? accessor))
            {
                string typeText = FormatType(leaf.Type, writerCtx.TypeArgSubstitutions);
                sb.Append("            ").Append(typeText).Append(' ').Append(leaf.VarName).AppendLine(";");
                sb.Append("            if (reader.HasConverter(").Append(typeOfText).AppendLine("))");
                sb.Append("                ").Append(leaf.VarName).Append(" = (").Append(typeText).Append(")reader.GetValue(").Append(i).Append(", reader.GetColumnType(").Append(i).Append("), ").Append(typeOfText).AppendLine(")!;");
                sb.AppendLine("            else");
                sb.Append("                ")
                    .Append(leaf.VarName).Append(" = ")
                    .Append("reader.").Append(accessor).Append("(").Append(i).AppendLine(");");
            }
            else
            {
                string typeText = FormatType(leaf.Type, writerCtx.TypeArgSubstitutions);
                sb.Append("            ")
                    .Append(typeText).Append(' ').Append(leaf.VarName).Append(" = (")
                    .Append(typeText).Append(")reader.GetValue(").Append(i)
                    .Append(", reader.GetColumnType(").Append(i).Append("), ")
                    .Append(typeOfText).AppendLine(")!;");
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

    /// <summary>
    /// Tells whether a method can only be translated to SQL and not run in code.
    /// </summary>
    public static bool IsSqlOnlyCallable(IMethodSymbol method)
    {
        string? declType = method.ContainingType?.ToDisplayString();
        return declType is "SQLite.Framework.SQLiteFunctions"
            or "SQLite.Framework.SQLiteFTS5Functions"
            or "SQLite.Framework.SQLiteJsonFunctions"
            or "SQLite.Framework.SQLiteWindowFunctions"
            or "SQLite.Framework.SQLiteFrameBoundary";
    }

    /// <summary>
    /// Tells whether a symbol is reachable using only public access.
    /// </summary>
    public static bool IsSymbolPubliclyReachable(ISymbol? symbol)
    {
        return IsSymbolAccessibleFromGenerator(symbol, generatorAssembly: null);
    }

    /// <summary>
    /// Tells whether a member access reads a static field or property that the generated
    /// code cannot reference directly and must read as a captured value instead.
    /// </summary>
    public static bool IsInaccessibleStaticCapture(MemberAccessExpressionSyntax access, EmitContext ctx)
    {
        if (access.Kind() != SyntaxKind.SimpleMemberAccessExpression)
        {
            return false;
        }

        if (ctx.Model.GetConstantValue(access).HasValue)
        {
            return false;
        }

        ISymbol? symbol = ctx.Model.GetSymbolInfo(access).Symbol;
        if (symbol is not (IFieldSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true }))
        {
            return false;
        }

        if (ctx.Model.GetSymbolInfo(access.Expression).Symbol is not ITypeSymbol)
        {
            return false;
        }

        return !IsSymbolAccessibleFromGenerator(symbol, ctx.GeneratorAssembly);
    }

    /// <summary>
    /// Tells whether a symbol is accessible from the generator assembly.
    /// </summary>
    public static bool IsSymbolAccessibleFromGenerator(ISymbol? symbol, IAssemblySymbol? generatorAssembly)
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

            Accessibility acc = current.DeclaredAccessibility;
            if (acc == Accessibility.Public || acc == Accessibility.NotApplicable)
            {
                continue;
            }

            if (acc == Accessibility.Internal
                && generatorAssembly != null
                && SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, generatorAssembly)
                && !(current is INamedTypeSymbol named && named.IsFileLocal))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Tells whether a type is reachable using only public access.
    /// </summary>
    public static bool IsTypePubliclyReachable(ITypeSymbol type)
    {
        return IsTypeAccessibleFromGenerator(type, generatorAssembly: null);
    }

    /// <summary>
    /// Tells whether a type is accessible from the generator assembly.
    /// </summary>
    public static bool IsTypeAccessibleFromGenerator(ITypeSymbol type, IAssemblySymbol? generatorAssembly)
    {
        if (type is INamedTypeSymbol nullable
            && nullable.IsGenericType
            && nullable.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
            && nullable.TypeArguments.Length == 1)
        {
            return IsTypeAccessibleFromGenerator(nullable.TypeArguments[0], generatorAssembly);
        }

        if (type is IArrayTypeSymbol array)
        {
            return IsTypeAccessibleFromGenerator(array.ElementType, generatorAssembly);
        }

        if (type is INamedTypeSymbol named)
        {
            foreach (ITypeSymbol arg in named.TypeArguments)
            {
                if (!IsTypeAccessibleFromGenerator(arg, generatorAssembly))
                {
                    return false;
                }
            }
        }

        return IsSymbolAccessibleFromGenerator(type, generatorAssembly);
    }

    /// <summary>
    /// Formats a type as its fully qualified name.
    /// </summary>
    public static string FormatType(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// Formats a type as its fully qualified name after applying substitutions.
    /// </summary>
    public static string FormatType(ITypeSymbol symbol, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol>? substitutions)
    {
        ITypeSymbol? mapped = SelectSignatureWriter.Substitute(symbol, substitutions);
        return (mapped ?? symbol).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// Tells whether a type is a nullable value type such as <c>int?</c>.
    /// </summary>
    public static bool IsNullableValueType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named
            && named.IsGenericType
            && named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
    }

    /// <summary>
    /// Returns the underlying type of a nullable value type or the type itself.
    /// </summary>
    public static ITypeSymbol StripNullableSymbol(ITypeSymbol type)
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

    /// <summary>
    /// Looks up the fast-path reader accessor for a CLR type.
    /// </summary>
    public static bool TryGetFastPathAccessor(ITypeSymbol type, out string? accessor)
    {
        return TryGetFastPathAccessor(type, out accessor, out _);
    }

    /// <summary>
    /// Looks up the fast-path reader accessor for a CLR type. <paramref name="handlesNull"/>
    /// is <see langword="true"/> when the accessor itself returns <see langword="null"/> for
    /// SQL NULL, which lets the emitter skip a redundant <c>IsDBNull</c> guard.
    /// </summary>
    public static bool TryGetFastPathAccessor(ITypeSymbol type, out string? accessor, out bool handlesNull)
    {
        handlesNull = false;
        ITypeSymbol stripped = StripNullableSymbol(type);
        switch (stripped.SpecialType)
        {
            case SpecialType.System_Int32:
                accessor = "GetInt32";
                return true;
            case SpecialType.System_Int64:
                accessor = "GetInt64";
                return true;
            case SpecialType.System_Int16:
                accessor = "GetInt16";
                return true;
            case SpecialType.System_Byte:
                accessor = "GetByteValue";
                return true;
            case SpecialType.System_SByte:
                accessor = "GetSByteValue";
                return true;
            case SpecialType.System_UInt16:
                accessor = "GetUInt16";
                return true;
            case SpecialType.System_UInt32:
                accessor = "GetUInt32";
                return true;
            case SpecialType.System_UInt64:
                accessor = "GetUInt64";
                return true;
            case SpecialType.System_Double:
                accessor = "GetDouble";
                return true;
            case SpecialType.System_Single:
                accessor = "GetSingle";
                return true;
            case SpecialType.System_Boolean:
                accessor = "GetBoolean";
                return true;
            case SpecialType.System_String:
                accessor = "GetString";
                handlesNull = true;
                return true;
            case SpecialType.System_DateTime:
                accessor = "GetDateTimeValue";
                return true;
            case SpecialType.System_Decimal:
                accessor = "GetDecimalValue";
                return true;
            default:
                accessor = TryGetStructAccessor(stripped);
                return accessor != null;
        }
    }

    /// <summary>
    /// Returns true when the type is an anonymous type or has one among its generic arguments
    /// or its array element type.
    /// </summary>
    public static bool ContainsAnonymousType(ITypeSymbol type)
    {
        if (type.IsAnonymousType)
        {
            return true;
        }

        if (type is IArrayTypeSymbol array)
        {
            return ContainsAnonymousType(array.ElementType);
        }

        if (type is INamedTypeSymbol named)
        {
            foreach (ITypeSymbol arg in named.TypeArguments)
            {
                if (ContainsAnonymousType(arg))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string? CollectArrayLeaves(ExpressionSyntax body, EmitContext ctx)
    {
        InitializerExpressionSyntax? initializer = body switch
        {
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer,
            ArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
            _ => null
        };

        if (initializer == null || initializer.Expressions.Count == 0)
        {
            return null;
        }

        ITypeSymbol? arraySymbol = ctx.Model.GetTypeInfo(body).Type ?? ctx.Model.GetTypeInfo(body).ConvertedType;
        if (arraySymbol is not IArrayTypeSymbol arrayType)
        {
            return null;
        }

        ITypeSymbol elementType = arrayType.ElementType;
        if (!IsTypeAccessibleFromGenerator(elementType, ctx.GeneratorAssembly))
        {
            return null;
        }

        bool isNullableElement = elementType.IsReferenceType || IsNullableValueType(elementType);
        string elementTypeText = FormatType(elementType, ctx.WriterCtx.TypeArgSubstitutions);

        StringBuilder bodyBuilder = new();
        FullyQualifiedRewriter rewriter = new(ctx);
        bodyBuilder.Append("new ").Append(elementTypeText).Append("[] { ");
        for (int i = 0; i < initializer.Expressions.Count; i++)
        {
            if (i > 0)
            {
                bodyBuilder.Append(", ");
            }

            ExpressionSyntax element = initializer.Expressions[i];
            bool elementBuildsObject = element is BaseObjectCreationExpressionSyntax;
            if (!ContainsClientEvalCall(element, ctx) && !elementBuildsObject)
            {
                int idx = ctx.Leaves.Count;
                string varName = "__leaf_" + idx;
                ctx.Leaves.Add(new LeafInfo(element, elementType, varName, isNullableElement));
                bodyBuilder.Append('(').Append(elementTypeText).Append(')').Append(varName);
                continue;
            }

            if (elementBuildsObject)
            {
                if (!CollectLeaves(element, ctx))
                {
                    return null;
                }
            }
            else if (!TryCollectClientElementLeaves(element, ctx))
            {
                return null;
            }

            SyntaxNode? rewritten = rewriter.Visit(element);
            if (rewriter.Failed || rewritten is not ExpressionSyntax rewrittenElement)
            {
                return null;
            }

            bodyBuilder.Append('(').Append(elementTypeText).Append(")(")
                .Append(rewrittenElement.NormalizeWhitespace(indentation: "", eol: " ").ToFullString())
                .Append(')');
        }
        bodyBuilder.Append(" }");

        return bodyBuilder.ToString();
    }

    private static bool ContainsClientEvalCall(SyntaxNode node, EmitContext ctx)
    {
        if (node is IdentifierNameSyntax substIdent
            && ctx.Model.GetSymbolInfo(substIdent).Symbol is { } substSym
            && ctx.WriterCtx.ParameterSubstitutions.TryGetValue(substSym, out ExpressionSyntax? substExpr))
        {
            return ContainsClientEvalCall(substExpr, ctx);
        }

        if (node is InvocationExpressionSyntax invoke
            && ctx.Model.GetSymbolInfo(invoke).Symbol is IMethodSymbol method
            && IsClientEvalMethod(method))
        {
            return true;
        }

        foreach (SyntaxNode child in node.ChildNodes())
        {
            if (ContainsClientEvalCall(child, ctx))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsClientEvalMethod(IMethodSymbol method)
    {
        if (IsSqlOnlyCallable(method) || IsFrameworkTranslatedMethod(method))
        {
            return false;
        }

        INamedTypeSymbol? containing = method.ContainingType;
        if (containing == null)
        {
            return false;
        }

        if (containing.TypeKind == TypeKind.Enum)
        {
            return false;
        }

        if (containing.IsGenericType && containing.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return false;
        }

        switch (containing.SpecialType)
        {
            case SpecialType.System_Object:
            case SpecialType.System_Enum:
            case SpecialType.System_String:
            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                return false;
        }

        string display = containing.ToDisplayString();
        return display is not ("System.Math"
            or "System.DateTime"
            or "System.DateTimeOffset"
            or "System.TimeSpan"
            or "System.DateOnly"
            or "System.TimeOnly"
            or "System.Guid");
    }

    private static bool TryCollectClientElementLeaves(ExpressionSyntax node, EmitContext ctx)
    {
        while (node is ParenthesizedExpressionSyntax paren)
        {
            node = paren.Expression;
        }

        if (node is IdentifierNameSyntax substIdent
            && ctx.Model.GetSymbolInfo(substIdent).Symbol is { } substSym
            && ctx.WriterCtx.ParameterSubstitutions.TryGetValue(substSym, out ExpressionSyntax? substExpr))
        {
            return TryCollectClientElementLeaves(substExpr, ctx);
        }

        switch (node)
        {
            case InvocationExpressionSyntax invoke:
            {
                if (ctx.Model.GetSymbolInfo(invoke).Symbol is not IMethodSymbol method || !IsClientEvalMethod(method))
                {
                    return false;
                }

                if (invoke.Expression is MemberAccessExpressionSyntax recvMa)
                {
                    if (ctx.Model.GetSymbolInfo(recvMa.Expression).Symbol is not ITypeSymbol
                        && !TryCollectClientOperand(recvMa.Expression, ctx))
                    {
                        return false;
                    }
                }
                else if (!method.IsStatic)
                {
                    return false;
                }

                foreach (ArgumentSyntax arg in invoke.ArgumentList.Arguments)
                {
                    if (!TryCollectClientOperand(arg.Expression, ctx))
                    {
                        return false;
                    }
                }

                return true;
            }

            case BinaryExpressionSyntax bin:
                if (bin.Kind() == SyntaxKind.AsExpression || bin.Kind() == SyntaxKind.IsExpression)
                {
                    return false;
                }

                return TryCollectClientOperand(bin.Left, ctx)
                    && TryCollectClientOperand(bin.Right, ctx);

            case PrefixUnaryExpressionSyntax unary
                when unary.Kind() == SyntaxKind.LogicalNotExpression || unary.Kind() == SyntaxKind.UnaryMinusExpression:
                return TryCollectClientOperand(unary.Operand, ctx);

            case CastExpressionSyntax cast:
                return ContainsClientEvalCall(cast.Expression, ctx)
                    && TryCollectClientOperand(cast.Expression, ctx);

            default:
                return false;
        }
    }

    private static bool TryCollectClientOperand(ExpressionSyntax node, EmitContext ctx)
    {
        while (node is ParenthesizedExpressionSyntax paren)
        {
            node = paren.Expression;
        }

        if (node is IdentifierNameSyntax substIdent
            && ctx.Model.GetSymbolInfo(substIdent).Symbol is { } substSym
            && ctx.WriterCtx.ParameterSubstitutions.TryGetValue(substSym, out ExpressionSyntax? substExpr))
        {
            return TryCollectClientOperand(substExpr, ctx);
        }

        if (ContainsClientEvalCall(node, ctx))
        {
            return TryCollectClientElementLeaves(node, ctx);
        }

        if (SelectSignatureWriter.IsRowLikeReference(node, ctx.WriterCtx))
        {
            return RegisterRowExpansion(node, ctx);
        }

        if (SelectSignatureWriter.IsStableConstantSubtree(node, ctx.WriterCtx))
        {
            return true;
        }

        if (node is MemberAccessExpressionSyntax access
            && access.Kind() == SyntaxKind.SimpleMemberAccessExpression
            && access.Expression is IdentifierNameSyntax rowIdent
            && IsRowReference(rowIdent, ctx)
            && !SelectSignatureWriter.IsUnmappedRowMember(ctx.Model.GetSymbolInfo(access).Symbol))
        {
            return TryRegisterRowMemberLeaf(access, rowIdent, ctx, allowReflected: false);
        }

        return false;
    }

    private static bool IsGroupingAggregateInvocation(InvocationExpressionSyntax invocation, EmitContext ctx)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax access)
        {
            return false;
        }

        if (ctx.Model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || method.ContainingType?.ToDisplayString() is not ("System.Linq.Queryable" or "System.Linq.Enumerable"))
        {
            return false;
        }

        return ctx.Model.GetTypeInfo(access.Expression).Type is INamedTypeSymbol { IsGenericType: true } receiverType
            && receiverType.ConstructedFrom.ToDisplayString() == "System.Linq.IGrouping<TKey, TElement>";
    }

    private static bool IsGroupingConcatInvocation(InvocationExpressionSyntax invocation, EmitContext ctx)
    {
        if (ctx.Model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || method.ContainingType?.SpecialType != SpecialType.System_String
            || method.Name is not (nameof(string.Join) or nameof(string.Concat))
            || invocation.ArgumentList.Arguments.Count == 0)
        {
            return false;
        }

        ExpressionSyntax current = invocation.ArgumentList.Arguments[invocation.ArgumentList.Arguments.Count - 1].Expression;
        while (true)
        {
            if (ctx.Model.GetTypeInfo(current).Type is INamedTypeSymbol { IsGenericType: true } sourceType
                && sourceType.ConstructedFrom.ToDisplayString() == "System.Linq.IGrouping<TKey, TElement>")
            {
                return true;
            }

            if (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax chained })
            {
                current = chained.Expression;
                continue;
            }

            return false;
        }
    }

    private static bool TryRegisterRowMemberLeaf(MemberAccessExpressionSyntax access, IdentifierNameSyntax rowIdent, EmitContext ctx, bool allowReflected)
    {
        ITypeSymbol? declaredLeafType = ctx.Model.GetTypeInfo(access).Type;
        ITypeSymbol? convertedLeafType = ctx.Model.GetTypeInfo(access).ConvertedType;
        ITypeSymbol? leafType = declaredLeafType is { IsValueType: true }
            && convertedLeafType is { IsValueType: false }
            ? declaredLeafType
            : declaredLeafType != null && convertedLeafType is { TypeKind: TypeKind.Interface }
                ? declaredLeafType
                : convertedLeafType ?? declaredLeafType;
        if (leafType == null)
        {
            return false;
        }

        bool isReflected = !IsTypeAccessibleFromGenerator(leafType, ctx.GeneratorAssembly);
        if (isReflected && !allowReflected)
        {
            return false;
        }

        bool isNullable = IsNullableRangeVarIdentifier(rowIdent, ctx);
        int idx = ctx.Leaves.Count;
        string varName = "__leaf_" + idx;
        ctx.Leaves.Add(new LeafInfo(access, leafType, varName, isNullable, isReflected));
        ctx.LeafIndexBySyntax[access] = idx;
        if (isNullable
            && ctx.Model.GetSymbolInfo(rowIdent).Symbol is { } rowSym
            && !ctx.NullableRangeFirstLeaf.ContainsKey(rowSym))
        {
            ctx.NullableRangeFirstLeaf[rowSym] = idx;
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
                && ctx.Model.GetTypeInfo(rangeIdent).Type is INamedTypeSymbol rangeType)
            {
                List<IPropertySymbol> props = SelectSignatureWriter.GetRowProperties(rangeType);
                if (props.Count > 0)
                {
                    int idx = ctx.Leaves.Count;
                    string varName = "__leaf_" + idx;
                    ctx.Leaves.Add(new LeafInfo(bin, props[0].Type, varName, isNullable: true));
                    ctx.LeafIndexBySyntax[bin] = idx;
                    if (!ctx.NullableRangeFirstLeaf.ContainsKey(binRangeSym))
                    {
                        ctx.NullableRangeFirstLeaf[binRangeSym] = idx;
                    }

                    return true;
                }
            }
        }

        if (node is BaseObjectCreationExpressionSyntax privBoc
            && privBoc.Initializer != null
            && privBoc.Initializer.Kind() == SyntaxKind.ObjectInitializerExpression
            && ctx.Model.GetTypeInfo(privBoc).Type is ITypeSymbol privType
            && !IsTypeAccessibleFromGenerator(privType, ctx.GeneratorAssembly))
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

        if (node is InvocationExpressionSyntax aggregateInvoke
            && (IsGroupingAggregateInvocation(aggregateInvoke, ctx) || IsGroupingConcatInvocation(aggregateInvoke, ctx))
            && ctx.Model.GetTypeInfo(aggregateInvoke).Type is { } aggregateType)
        {
            int aggregateIdx = ctx.Leaves.Count;
            string aggregateVar = "__leaf_" + aggregateIdx;
            ctx.Leaves.Add(new LeafInfo(aggregateInvoke, aggregateType, aggregateVar));
            ctx.LeafIndexBySyntax[aggregateInvoke] = aggregateIdx;
            return true;
        }

        switch (node)
        {
            case MemberAccessExpressionSyntax access when access.Kind() == SyntaxKind.SimpleMemberAccessExpression:
                if (access.Expression is CastExpressionSyntax castReceiver
                    && castReceiver.Expression is IdentifierNameSyntax castIdent
                    && IsRowReference(castIdent, ctx)
                    && SelectSignatureWriter.IsExplicitInterfaceOnlyMember(
                        ctx.Model.GetSymbolInfo(access).Symbol, ctx.Model.GetTypeInfo(castIdent).Type))
                {
                    return IsExpandableRow(castIdent, ctx) && RegisterRowExpansion(castIdent, ctx);
                }

                if (access.Expression is IdentifierNameSyntax rowIdent && IsRowReference(rowIdent, ctx))
                {
                    if (SelectSignatureWriter.IsUnmappedRowMember(ctx.Model.GetSymbolInfo(access).Symbol))
                    {
                        return IsExpandableRow(rowIdent, ctx) && RegisterRowExpansion(access.Expression, ctx);
                    }

                    return TryRegisterRowMemberLeaf(access, rowIdent, ctx, allowReflected: true);
                }

                if (SelectSignatureWriter.IsCapturedValue(access, ctx.WriterCtx))
                {
                    return true;
                }

                if (IsInaccessibleStaticCapture(access, ctx))
                {
                    return true;
                }

                if (!IsSymbolAccessibleFromGenerator(ctx.Model.GetSymbolInfo(access).Symbol, ctx.GeneratorAssembly))
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
                        if (ctx.WriterCtx.RowBindings.ContainsKey(sym)
                            && ctx.Model.GetTypeInfo(ident).Type is INamedTypeSymbol nullableRowType
                            && SelectSignatureWriter.IsConstructibleEntityType(nullableRowType))
                        {
                            return RegisterRowExpansion(ident, ctx);
                        }
                        return true;
                    }
                    if (ctx.WriterCtx.RowBindings.ContainsKey(sym)
                        && ctx.Model.GetTypeInfo(ident).Type is INamedTypeSymbol rowIdentType
                        && SelectSignatureWriter.IsConstructibleEntityType(rowIdentType))
                    {
                        return RegisterRowExpansion(ident, ctx);
                    }
                    return !ctx.WriterCtx.RowBindings.ContainsKey(sym);
                }

                if (sym is ITypeSymbol
                    && ident.Parent is MemberAccessExpressionSyntax typeReceiverMa
                    && typeReceiverMa.Expression == ident
                    && typeReceiverMa.Parent is InvocationExpressionSyntax)
                {
                    return true;
                }

                if (SelectSignatureWriter.IsCapturedValue(ident, ctx.WriterCtx))
                {
                    return true;
                }

                if (sym is ITypeSymbol identType)
                {
                    ITypeSymbol? substitutedType = SelectSignatureWriter.Substitute(identType, ctx.WriterCtx.TypeArgSubstitutions);
                    return substitutedType is not (null or ITypeParameterSymbol)
                        && IsTypeAccessibleFromGenerator(substitutedType, ctx.GeneratorAssembly);
                }

                return IsSymbolAccessibleFromGenerator(sym, ctx.GeneratorAssembly);

            case InvocationExpressionSyntax invoke:
                if (ctx.Model.GetSymbolInfo(invoke).Symbol is not IMethodSymbol method)
                {
                    return false;
                }

                if (IsSqlOnlyCallable(method))
                {
                    return false;
                }

                bool expand = !IsFrameworkTranslatedMethod(method);
                if (expand)
                {
                    if (invoke.Expression is MemberAccessExpressionSyntax recvMa)
                    {
                        if (SelectSignatureWriter.IsRowLikeReference(recvMa.Expression, ctx.WriterCtx))
                        {
                            if (!RegisterRowExpansion(recvMa.Expression, ctx))
                            {
                                return false;
                            }
                        }
                        else if (!CollectLeaves(recvMa.Expression, ctx))
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

    private static bool IsExpandableRow(IdentifierNameSyntax ident, EmitContext ctx)
    {
        return ctx.Model.GetTypeInfo(ident).Type is INamedTypeSymbol rowType
            && SelectSignatureWriter.IsConstructibleEntityType(rowType);
    }

    private static int CountLeavesUnder(List<LeafInfo> leaves, int startIndex, SyntaxNode container)
    {
        int count = 0;
        for (int j = startIndex; j < leaves.Count; j++)
        {
            SyntaxNode node = leaves[j].Node;
            if (node == container || node.Ancestors().Contains(container))
            {
                count++;
            }
            else
            {
                break;
            }
        }
        return count;
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
            if (!IsTypeAccessibleFromGenerator(prop.Type, ctx.GeneratorAssembly))
            {
                return false;
            }

            if (prop.SetMethod!.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
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

    private static string? TryGetStructAccessor(ITypeSymbol type)
    {
        if (type.ContainingNamespace?.ToDisplayString() != "System")
        {
            return null;
        }

        return type.Name switch
        {
            "DateTimeOffset" => "GetDateTimeOffsetValue",
            "TimeSpan" => "GetTimeSpanValue",
            "DateOnly" => "GetDateOnlyValue",
            "TimeOnly" => "GetTimeOnlyValue",
            "Guid" => "GetGuidValue",
            _ => null,
        };
    }
}
