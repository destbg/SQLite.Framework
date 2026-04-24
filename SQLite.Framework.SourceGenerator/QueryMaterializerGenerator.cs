using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

/// <summary>
/// Reads the user code and writes a class that knows how to read each entity and Select result
/// from a SQLite row without using reflection when possible.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class QueryMaterializerGenerator : IIncrementalGenerator
{
    private const string SQLiteDatabaseFullName = "SQLite.Framework.SQLiteDatabase";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol?> fromInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateInvocation(node),
                transform: static (ctx, _) => ExtractEntityFromInvocation(ctx))
            .Where(static t => t is not null);

        IncrementalValuesProvider<INamedTypeSymbol?> fromMembers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateMember(node),
                transform: static (ctx, _) => ExtractEntityFromMember(ctx))
            .Where(static t => t is not null);

        IncrementalValuesProvider<INamedTypeSymbol?> fromSelectProjections = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateSelectInvocation(node),
                transform: static (ctx, _) => ExtractProjectionTypeFromSelect(ctx))
            .Where(static t => t is not null);

        IncrementalValuesProvider<SelectInvocation?> selectInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateSelectInvocation(node),
                transform: static (ctx, _) => ExtractSelectInvocation(ctx))
            .Where(static t => t is not null);

        IncrementalValuesProvider<SelectInvocation?> querySelects = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is SelectClauseSyntax,
                transform: static (ctx, _) => ExtractSelectFromQuery(ctx))
            .Where(static t => t is not null);

        IncrementalValuesProvider<(GroupByKeyInvocation Invocation, SemanticModel Model)?> groupByKeys = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateGroupByInvocation(node),
                transform: static (ctx, _) => ExtractGroupByKey(ctx))
            .Where(static t => t.HasValue);

        IncrementalValueProvider<ImmutableArray<INamedTypeSymbol?>> allEntities =
            fromInvocations.Collect()
                .Combine(fromMembers.Collect())
                .Combine(fromSelectProjections.Collect())
                .Select(static (pair, _) => pair.Left.Left.AddRange(pair.Left.Right).AddRange(pair.Right));

        IncrementalValueProvider<(Compilation Compilation, ImmutableArray<INamedTypeSymbol?> Entities, ImmutableArray<SelectInvocation?> Selects, ImmutableArray<(GroupByKeyInvocation Invocation, SemanticModel Model)?> GroupKeys)> source =
            context.CompilationProvider.Combine(allEntities)
                .Combine(selectInvocations.Collect())
                .Combine(querySelects.Collect())
                .Combine(groupByKeys.Collect())
                .Select(static (p, _) => (p.Left.Left.Left.Left, p.Left.Left.Left.Right, p.Left.Left.Right.AddRange(p.Left.Right), p.Right));

        context.RegisterSourceOutput(source, (spc, pair) =>
        {
            Compilation compilation = pair.Compilation;
            ImmutableArray<INamedTypeSymbol?> entities = pair.Entities;
            ImmutableArray<SelectInvocation?> selects = pair.Selects;
            ImmutableArray<(GroupByKeyInvocation Invocation, SemanticModel Model)?> groupKeys = pair.GroupKeys;

            HashSet<INamedTypeSymbol> unique = new(SymbolEqualityComparer.Default);
            foreach (INamedTypeSymbol? symbol in entities)
            {
                if (symbol != null
                    && !symbol.IsAbstract
                    && !symbol.IsGenericType
                    && IsPubliclyReachable(symbol)
                    && !IsFrameworkType(symbol))
                {
                    unique.Add(symbol);
                }
            }

            Dictionary<string, SelectInvocation> uniqueSelects = new();
            foreach (SelectInvocation? invocation in selects)
            {
                if (invocation is { } sel && !uniqueSelects.ContainsKey(sel.Signature))
                {
                    uniqueSelects[sel.Signature] = sel;
                }
            }

            Dictionary<string, (GroupByKeyInvocation Invocation, SemanticModel Model)> uniqueGroupKeys = new();
            foreach ((GroupByKeyInvocation Invocation, SemanticModel Model)? entry in groupKeys)
            {
                if (entry is { } pair2 && !uniqueGroupKeys.ContainsKey(pair2.Invocation.Signature))
                {
                    uniqueGroupKeys[pair2.Invocation.Signature] = pair2;
                }
            }

            if (unique.Count == 0 && uniqueSelects.Count == 0 && uniqueGroupKeys.Count == 0)
            {
                return;
            }

            string generated = EntityMaterializerEmitter.Emit("SQLite.Framework.Generated", unique, uniqueSelects.Values, uniqueGroupKeys.Values);
            spc.AddSource("SQLiteFrameworkGeneratedMaterializers.g.cs", generated);
        });
    }

    private static bool IsCandidateGroupByInvocation(SyntaxNode node)
    {
        if (node is QueryExpressionSyntax query && query.Body.SelectOrGroup is GroupClauseSyntax)
        {
            return true;
        }

        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax access)
        {
            return false;
        }

        string name = access.Name switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            IdentifierNameSyntax ident => ident.Identifier.ValueText,
            _ => string.Empty
        };

        return name == "GroupBy";
    }

    private static (GroupByKeyInvocation Invocation, SemanticModel Model)? ExtractGroupByKey(GeneratorSyntaxContext ctx)
    {
        LambdaExpressionSyntax? lambda = null;

        if (ctx.Node is InvocationExpressionSyntax invocation)
        {
            if (ctx.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            {
                return null;
            }

            if (method.Name != "GroupBy")
            {
                return null;
            }

            string containingType = method.ContainingType.ToDisplayString();
            if (containingType != "System.Linq.Queryable" && containingType != "System.Linq.Enumerable")
            {
                return null;
            }

            if (invocation.ArgumentList.Arguments.Count < 1)
            {
                return null;
            }

            ExpressionSyntax keyArg = invocation.ArgumentList.Arguments[0].Expression;
            while (keyArg is ParenthesizedExpressionSyntax paren)
            {
                keyArg = paren.Expression;
            }

            if (keyArg is not LambdaExpressionSyntax methodLambda)
            {
                return null;
            }

            lambda = methodLambda;
        }
        else if (ctx.Node is QueryExpressionSyntax query
            && query.Body.SelectOrGroup is GroupClauseSyntax groupClause)
        {
            return ExtractGroupFromQuery(ctx, query, groupClause);
        }

        if (lambda == null)
        {
            return null;
        }

        ParameterSyntax? paramSyntax = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count == 1
                => paren.ParameterList.Parameters[0],
            _ => null
        };

        if (paramSyntax == null)
        {
            return null;
        }

        if (ctx.SemanticModel.GetDeclaredSymbol(paramSyntax) is not IParameterSymbol rowSymbol)
        {
            return null;
        }

        if (lambda.Body is not ExpressionSyntax bodyExpr)
        {
            return null;
        }

        SelectSignatureCtx writerCtx = BuildFluentCtx(rowSymbol, ctx.SemanticModel);
        string? signature = SelectSignatureWriter.TryCompute(bodyExpr, writerCtx);
        if (signature == null)
        {
            return null;
        }

        ITypeSymbol? keyType = ctx.SemanticModel.GetTypeInfo(bodyExpr).Type
            ?? ctx.SemanticModel.GetTypeInfo(bodyExpr).ConvertedType;
        if (keyType == null)
        {
            return null;
        }

        return (new GroupByKeyInvocation(signature, bodyExpr, paramSyntax, rowSymbol, keyType), ctx.SemanticModel);
    }

    private static (GroupByKeyInvocation Invocation, SemanticModel Model)? ExtractGroupFromQuery(GeneratorSyntaxContext ctx, QueryExpressionSyntax query, GroupClauseSyntax groupClause)
    {
        FromClauseSyntax fromClause = query.FromClause;
        if (ctx.SemanticModel.GetDeclaredSymbol(fromClause) is not IRangeVariableSymbol rangeVar)
        {
            return null;
        }

        ITypeSymbol? rangeType = SelectSignatureWriter.ResolveRangeVariableType(rangeVar, ctx.SemanticModel);
        if (rangeType == null)
        {
            return null;
        }

        ExpressionSyntax bodyExpr = groupClause.ByExpression;
        while (bodyExpr is ParenthesizedExpressionSyntax paren)
        {
            bodyExpr = paren.Expression;
        }

        Dictionary<ISymbol, RowBinding> bindings = new(SymbolEqualityComparer.Default)
        {
            [rangeVar] = new RowBinding((string?)null, rangeType)
        };
        SelectSignatureCtx writerCtx = new(rangeType, bindings, ctx.SemanticModel);

        string? signature = SelectSignatureWriter.TryCompute(bodyExpr, writerCtx);
        if (signature == null)
        {
            return null;
        }

        ITypeSymbol? keyType = ctx.SemanticModel.GetTypeInfo(bodyExpr).Type
            ?? ctx.SemanticModel.GetTypeInfo(bodyExpr).ConvertedType;
        if (keyType == null)
        {
            return null;
        }

        return null;
    }

    private static bool UpstreamContainsSelect(ExpressionSyntax expr, SemanticModel model)
    {
        SyntaxNode? current = expr;
        while (current != null)
        {
            switch (current)
            {
                case InvocationExpressionSyntax upstream:
                    if (model.GetSymbolInfo(upstream).Symbol is IMethodSymbol m
                        && m.Name == "Select")
                    {
                        string declType = m.ContainingType.ToDisplayString();
                        if (declType is "System.Linq.Queryable" or "System.Linq.Enumerable")
                        {
                            return true;
                        }
                    }
                    current = (upstream.Expression as MemberAccessExpressionSyntax)?.Expression;
                    continue;
                case MemberAccessExpressionSyntax ma:
                    current = ma.Expression;
                    continue;
                case ParenthesizedExpressionSyntax paren:
                    current = paren.Expression;
                    continue;
                case QueryExpressionSyntax query:
                    return query.Body.SelectOrGroup is SelectClauseSyntax;
                default:
                    return false;
            }
        }
        return false;
    }

    private static SelectSignatureCtx BuildFluentCtx(IParameterSymbol rowSymbol, SemanticModel model)
    {
        Dictionary<ISymbol, RowBinding> bindings = new(SymbolEqualityComparer.Default)
        {
            [rowSymbol] = new RowBinding((string?)null, rowSymbol.Type)
        };
        return new SelectSignatureCtx(rowSymbol.Type, bindings, model);
    }

    private static SelectInvocation? ExtractSelectInvocation(GeneratorSyntaxContext ctx)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)ctx.Node;
        if (ctx.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return null;
        }

        if (method.Name != "Select")
        {
            return null;
        }

        string containingType = method.ContainingType.ToDisplayString();
        if (containingType != "System.Linq.Queryable" && containingType != "System.Linq.Enumerable")
        {
            return null;
        }

        if (method.TypeArguments.Length < 2 || method.TypeArguments[1] is not INamedTypeSymbol projection)
        {
            return null;
        }

        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        ExpressionSyntax selectorExpr = invocation.ArgumentList.Arguments[invocation.ArgumentList.Arguments.Count - 1].Expression;
        while (selectorExpr is ParenthesizedExpressionSyntax paren)
        {
            selectorExpr = paren.Expression;
        }

        if (selectorExpr is not LambdaExpressionSyntax lambda)
        {
            return null;
        }

        if (lambda.Body is not ExpressionSyntax bodyExpr)
        {
            return null;
        }

        ParameterSyntax? paramSyntax = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count == 1
                => paren.ParameterList.Parameters[0],
            _ => null
        };

        if (paramSyntax == null)
        {
            return null;
        }

        if (ctx.SemanticModel.GetDeclaredSymbol(paramSyntax) is not IParameterSymbol rowSymbol)
        {
            return null;
        }

        if (invocation.Expression is MemberAccessExpressionSyntax chainHead
            && UpstreamContainsSelect(chainHead.Expression, ctx.SemanticModel))
        {
            return TryFlattenChainedSelect(bodyExpr, rowSymbol, chainHead.Expression, projection, ctx.SemanticModel);
        }

        SelectSignatureCtx writerCtx = BuildFluentCtx(rowSymbol, ctx.SemanticModel);
        string? signature = SelectSignatureWriter.TryCompute(bodyExpr, writerCtx);
        if (signature == null)
        {
            return null;
        }

        return new SelectInvocation(signature, bodyExpr, writerCtx, ctx.SemanticModel, projection);
    }

    private static List<(string Name, ITypeSymbol Type)>? FindMemberPath(INamedTypeSymbol type, string targetName, ITypeSymbol targetType)
    {
        foreach (ISymbol member in type.GetMembers())
        {
            if (member is not IPropertySymbol prop)
            {
                continue;
            }

            if (prop.Name == targetName && SymbolEqualityComparer.Default.Equals(prop.Type, targetType))
            {
                return new List<(string, ITypeSymbol)> { (prop.Name, prop.Type) };
            }
        }

        foreach (ISymbol member in type.GetMembers())
        {
            if (member is IPropertySymbol prop
                && prop.Type is INamedTypeSymbol nested
                && nested.IsAnonymousType)
            {
                List<(string Name, ITypeSymbol Type)>? sub = FindMemberPath(nested, targetName, targetType);
                if (sub != null)
                {
                    sub.Insert(0, (prop.Name, prop.Type));
                    return sub;
                }
            }
        }

        return null;
    }

    private static SelectInvocation? TryFlattenChainedSelect(ExpressionSyntax outerBody, IParameterSymbol outerRow, ExpressionSyntax receiver, INamedTypeSymbol projection, SemanticModel model)
    {
        (ExpressionSyntax Body, ISymbol Row)? inner = FindUpstreamInnerSelector(receiver, model);
        if (inner is not { } innerInfo)
        {
            return null;
        }

        if (outerBody is MemberAccessExpressionSyntax outerMa
            && outerMa.Kind() == SyntaxKind.SimpleMemberAccessExpression
            && outerMa.Expression is IdentifierNameSyntax outerRowIdent
            && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(outerRowIdent).Symbol, outerRow))
        {
            string memberName = outerMa.Name.Identifier.ValueText;

            InitializerExpressionSyntax? initializer = innerInfo.Body switch
            {
                ObjectCreationExpressionSyntax oc when oc.Initializer?.Kind() == SyntaxKind.ObjectInitializerExpression
                    => oc.Initializer,
                ImplicitObjectCreationExpressionSyntax ioc when ioc.Initializer?.Kind() == SyntaxKind.ObjectInitializerExpression
                    => ioc.Initializer,
                _ => null
            };

            if (initializer != null)
            {
                foreach (ExpressionSyntax exprItem in initializer.Expressions)
                {
                    if (exprItem is AssignmentExpressionSyntax assign
                        && assign.Left is IdentifierNameSyntax leftId
                        && leftId.Identifier.ValueText == memberName)
                    {
                        ExpressionSyntax substitutedBody = assign.Right;
                        while (substitutedBody is ParenthesizedExpressionSyntax p)
                        {
                            substitutedBody = p.Expression;
                        }

                        SelectSignatureCtx ctxSimple = BuildCtxForRowSymbol(innerInfo.Row, model);
                        if (ctxSimple.OuterRowType == null)
                        {
                            return null;
                        }

                        string? sigSimple = SelectSignatureWriter.TryCompute(substitutedBody, ctxSimple);
                        if (sigSimple == null)
                        {
                            return null;
                        }

                        return new SelectInvocation(sigSimple, substitutedBody, ctxSimple, model, projection);
                    }
                }
            }
        }

        SelectSignatureCtx ctx = BuildCtxForRowSymbol(innerInfo.Row, model);
        if (ctx.OuterRowType == null)
        {
            return null;
        }
        ctx.ParameterSubstitutions[outerRow] = innerInfo.Body;

        string? signature = SelectSignatureWriter.TryCompute(outerBody, ctx);
        if (signature == null)
        {
            return null;
        }

        return new SelectInvocation(signature, outerBody, ctx, model, projection);
    }

    private static (ExpressionSyntax Body, ISymbol Row)? FindUpstreamInnerSelector(ExpressionSyntax receiver, SemanticModel model)
    {
        SyntaxNode? current = receiver;
        while (current != null)
        {
            switch (current)
            {
                case InvocationExpressionSyntax inv:
                    if (model.GetSymbolInfo(inv).Symbol is IMethodSymbol m
                        && m.Name == "Select"
                        && (m.ContainingType.ToDisplayString() is "System.Linq.Queryable" or "System.Linq.Enumerable"))
                    {
                        if (inv.ArgumentList.Arguments.Count == 0)
                        {
                            return null;
                        }

                        ExpressionSyntax arg = inv.ArgumentList.Arguments[inv.ArgumentList.Arguments.Count - 1].Expression;
                        while (arg is ParenthesizedExpressionSyntax p)
                        {
                            arg = p.Expression;
                        }

                        if (arg is not LambdaExpressionSyntax innerLambda)
                        {
                            return null;
                        }

                        ParameterSyntax? innerParam = innerLambda switch
                        {
                            SimpleLambdaExpressionSyntax s => s.Parameter,
                            ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count == 1
                                => paren.ParameterList.Parameters[0],
                            _ => null
                        };

                        if (innerParam == null)
                        {
                            return null;
                        }

                        if (model.GetDeclaredSymbol(innerParam) is not IParameterSymbol innerRow)
                        {
                            return null;
                        }

                        if (innerLambda.Body is not ExpressionSyntax innerBody)
                        {
                            return null;
                        }

                        return (innerBody, innerRow);
                    }
                    current = (inv.Expression as MemberAccessExpressionSyntax)?.Expression;
                    continue;
                case MemberAccessExpressionSyntax ma:
                    current = ma.Expression;
                    continue;
                case ParenthesizedExpressionSyntax paren:
                    current = paren.Expression;
                    continue;
                case QueryExpressionSyntax query when query.Body.SelectOrGroup is SelectClauseSyntax sel:
                    if (model.GetDeclaredSymbol(query.FromClause) is not IRangeVariableSymbol range)
                    {
                        return null;
                    }
                    return (sel.Expression, range);
                default:
                    return null;
            }
        }

        return null;
    }

    private static SelectSignatureCtx BuildCtxForRowSymbol(ISymbol row, SemanticModel model)
    {
        ITypeSymbol? rowType = row switch
        {
            IParameterSymbol p => p.Type,
            IRangeVariableSymbol r => SelectSignatureWriter.ResolveRangeVariableType(r, model),
            _ => null
        };

        Dictionary<ISymbol, RowBinding> bindings = new(SymbolEqualityComparer.Default);
        if (rowType != null)
        {
            bindings[row] = new RowBinding((string?)null, rowType);
        }
        return new SelectSignatureCtx(rowType!, bindings, model);
    }

    private static SelectInvocation? ExtractSelectFromQuery(GeneratorSyntaxContext ctx)
    {
        SelectClauseSyntax selectClause = (SelectClauseSyntax)ctx.Node;
        if (selectClause.Parent is not QueryBodySyntax body)
        {
            return null;
        }

        if (body.Parent is not QueryExpressionSyntax query)
        {
            return null;
        }

        FromClauseSyntax fromClause = query.FromClause;
        if (ctx.SemanticModel.GetDeclaredSymbol(fromClause) is not IRangeVariableSymbol primaryRange)
        {
            return null;
        }

        List<(IRangeVariableSymbol Symbol, ITypeSymbol Type)> ranges = new();
        HashSet<ISymbol> nullableRanges = new(SymbolEqualityComparer.Default);
        ITypeSymbol? primaryType = SelectSignatureWriter.ResolveRangeVariableType(primaryRange, ctx.SemanticModel);
        if (primaryType == null)
        {
            return null;
        }
        ranges.Add((primaryRange, primaryType));

        foreach (QueryClauseSyntax clause in body.Clauses)
        {
            switch (clause)
            {
                case WhereClauseSyntax:
                case OrderByClauseSyntax:
                    continue;
                case JoinClauseSyntax join when join.Into == null:
                    if (ctx.SemanticModel.GetDeclaredSymbol(join) is not IRangeVariableSymbol joinRange)
                    {
                        return null;
                    }
                    ITypeSymbol? joinType = SelectSignatureWriter.ResolveRangeVariableType(joinRange, ctx.SemanticModel);
                    if (joinType == null)
                    {
                        return null;
                    }
                    ranges.Add((joinRange, joinType));
                    continue;
                case JoinClauseSyntax:
                    continue;
                case FromClauseSyntax nestedFrom:
                    if (ctx.SemanticModel.GetDeclaredSymbol(nestedFrom) is not IRangeVariableSymbol nestedRange)
                    {
                        return null;
                    }
                    ITypeSymbol? nestedType = SelectSignatureWriter.ResolveRangeVariableType(nestedRange, ctx.SemanticModel);
                    if (nestedType == null)
                    {
                        return null;
                    }
                    ranges.Add((nestedRange, nestedType));
                    if (nestedFrom.Expression is InvocationExpressionSyntax nestedInv
                        && nestedInv.Expression is MemberAccessExpressionSyntax nestedMa
                        && nestedMa.Name.Identifier.ValueText == "DefaultIfEmpty")
                    {
                        nullableRanges.Add(nestedRange);
                    }
                    continue;
                default:
                    return null;
            }
        }

        SymbolInfo selectInfo = ctx.SemanticModel.GetSymbolInfo(selectClause);
        IMethodSymbol? method = selectInfo.Symbol as IMethodSymbol
            ?? selectInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

        INamedTypeSymbol projection;
        INamedTypeSymbol outerRowType;

        if (method != null)
        {
            if (method.Name != "Select")
            {
                return null;
            }

            string containingType = method.ContainingType.ToDisplayString();
            if (containingType != "System.Linq.Queryable" && containingType != "System.Linq.Enumerable")
            {
                return null;
            }

            if (method.TypeArguments.Length < 2 || method.TypeArguments[1] is not INamedTypeSymbol projFromMethod)
            {
                return null;
            }
            projection = projFromMethod;

            if (method.TypeArguments[0] is not INamedTypeSymbol outerFromMethod)
            {
                return null;
            }
            outerRowType = outerFromMethod;
        }
        else
        {
            ITypeSymbol? projFromSyntax = ctx.SemanticModel.GetTypeInfo(selectClause.Expression).Type
                ?? ctx.SemanticModel.GetTypeInfo(selectClause.Expression).ConvertedType;
            if (projFromSyntax is not INamedTypeSymbol projN)
            {
                return null;
            }
            projection = projN;

            if (ranges.Count == 1 && ranges[0].Type is INamedTypeSymbol singleOuter)
            {
                outerRowType = singleOuter;
            }
            else
            {
                ImmutableArray<ITypeSymbol> memberTypes = ranges.Select(r => r.Type).ToImmutableArray();
                ImmutableArray<string> memberNames = ranges.Select(r => r.Symbol.Name).ToImmutableArray();
                try
                {
                    outerRowType = ctx.SemanticModel.Compilation.CreateAnonymousTypeSymbol(memberTypes, memberNames);
                }
                catch
                {
                    return null;
                }
            }
        }

        Dictionary<ISymbol, RowBinding> bindings = new(SymbolEqualityComparer.Default);
        if (ranges.Count == 1)
        {
            bindings[ranges[0].Symbol] = new RowBinding((string?)null, ranges[0].Type);
        }
        else
        {
            foreach ((IRangeVariableSymbol sym, ITypeSymbol rangeType) in ranges)
            {
                List<(string Name, ITypeSymbol Type)>? path = FindMemberPath(outerRowType, sym.Name, rangeType);
                if (path == null)
                {
                    return null;
                }
                bindings[sym] = new RowBinding(path, rangeType);
            }
        }

        ExpressionSyntax bodyExpr = selectClause.Expression;
        while (bodyExpr is ParenthesizedExpressionSyntax paren)
        {
            bodyExpr = paren.Expression;
        }

        if (bodyExpr is IdentifierNameSyntax ident
            && bindings.ContainsKey(ctx.SemanticModel.GetSymbolInfo(ident).Symbol ?? (ISymbol)primaryRange)
            && ranges.Count == 1)
        {
            return null;
        }

        SelectSignatureCtx writerCtx = new(outerRowType, bindings, ctx.SemanticModel);
        foreach (ISymbol nullableSym in nullableRanges)
        {
            writerCtx.NullableRangeVars.Add(nullableSym);
        }
        string? signature = SelectSignatureWriter.TryCompute(bodyExpr, writerCtx);
        if (signature == null)
        {
            return null;
        }

        return new SelectInvocation(signature, bodyExpr, writerCtx, ctx.SemanticModel, projection);
    }

    private static bool IsPubliclyReachable(INamedTypeSymbol symbol)
    {
        for (INamedTypeSymbol? current = symbol; current != null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsFrameworkType(INamedTypeSymbol symbol)
    {
        if (symbol.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
        {
            return true;
        }

        INamespaceSymbol? ns = symbol.ContainingNamespace;
        while (ns is { IsGlobalNamespace: false })
        {
            string? name = ns.Name;
            if (name == "System" || name == "Microsoft")
            {
                return ns.ContainingNamespace?.IsGlobalNamespace == true;
            }
            ns = ns.ContainingNamespace;
        }

        return false;
    }

    private static bool IsCandidateInvocation(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (invocation.Expression is MemberAccessExpressionSyntax access
            && access.Name is GenericNameSyntax generic
            && generic.Identifier.ValueText == "Table")
        {
            return true;
        }

        return false;
    }

    private static INamedTypeSymbol? ExtractEntityFromInvocation(GeneratorSyntaxContext ctx)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)ctx.Node;
        if (ctx.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return null;
        }

        if (method.Name != "Table" || method.TypeArguments.Length != 1)
        {
            return null;
        }

        if (method.ContainingType.ToDisplayString() != SQLiteDatabaseFullName)
        {
            return null;
        }

        return method.TypeArguments[0] as INamedTypeSymbol;
    }

    private static bool IsCandidateMember(SyntaxNode node)
    {
        return node is PropertyDeclarationSyntax || node is FieldDeclarationSyntax;
    }

    private static bool IsCandidateSelectInvocation(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax access)
        {
            return false;
        }

        string name = access.Name switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            IdentifierNameSyntax ident => ident.Identifier.ValueText,
            _ => string.Empty
        };

        return name == "Select";
    }

    private static INamedTypeSymbol? ExtractProjectionTypeFromSelect(GeneratorSyntaxContext ctx)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)ctx.Node;
        if (ctx.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return null;
        }

        if (method.Name != "Select")
        {
            return null;
        }

        string containingType = method.ContainingType.ToDisplayString();
        if (containingType != "System.Linq.Queryable" && containingType != "System.Linq.Enumerable")
        {
            return null;
        }

        if (method.TypeArguments.Length < 2 || method.TypeArguments[1] is not INamedTypeSymbol projection)
        {
            return null;
        }

        return projection;
    }

    private static INamedTypeSymbol? ExtractEntityFromMember(GeneratorSyntaxContext ctx)
    {
        ITypeSymbol? type = ctx.Node switch
        {
            PropertyDeclarationSyntax prop => ctx.SemanticModel.GetTypeInfo(prop.Type).Type,
            FieldDeclarationSyntax field => ctx.SemanticModel.GetTypeInfo(field.Declaration.Type).Type,
            _ => null
        };

        if (type is INamedTypeSymbol named
            && named.IsGenericType
            && named.OriginalDefinition.ToDisplayString() == "SQLite.Framework.SQLiteTable<T>"
            && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0] as INamedTypeSymbol;
        }

        return null;
    }
}
