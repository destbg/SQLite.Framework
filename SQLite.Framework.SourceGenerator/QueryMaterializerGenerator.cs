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
    private static readonly HashSet<string> EntityReturningGenericMethods = new()
    {
        "Table",
        "With",
        "WithRecursive",
        "Query",
        "QueryAsync",
        "QueryFirst",
        "QueryFirstAsync",
        "QueryFirstOrDefault",
        "QueryFirstOrDefaultAsync",
        "QuerySingle",
        "QuerySingleAsync",
        "QuerySingleOrDefault",
        "QuerySingleOrDefaultAsync",
        "FromSql",
        "ExecuteScalar",
        "ExecuteScalarAsync",
        "ExecuteQuery",
        "Values",
        "Cast",
        "OfType",
    };

    private const string SQLiteDatabaseFullName = "SQLite.Framework.SQLiteDatabase";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol?> fromInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateInvocation(node),
                transform: static (ctx, _) => ExtractEntityFromInvocation(ctx))
            .Where(static t => t is not null);

        IncrementalValuesProvider<UnresolvedGenericEntity?> fromGenericInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateInvocation(node),
                transform: static (ctx, _) => ExtractUnresolvedGenericEntity(ctx))
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

        IncrementalValuesProvider<INamedTypeSymbol?> fromQueryProjections = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is SelectClauseSyntax,
                transform: static (ctx, _) => ExtractProjectionTypeFromQuery(ctx))
            .Where(static t => t is not null);

        IncrementalValuesProvider<SelectInvocation?> selectInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateSelectInvocation(node),
                transform: static (ctx, _) => ExtractSelectInvocation(ctx))
            .Where(static t => t is not null);

        IncrementalValuesProvider<UnresolvedGenericSelect?> unresolvedSelects = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateSelectInvocation(node),
                transform: static (ctx, _) => ExtractUnresolvedSelectInvocation(ctx))
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

        IncrementalValuesProvider<(INamedTypeSymbol Container, string PropName, INamedTypeSymbol Nested)?> nestedInits = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ObjectCreationExpressionSyntax,
                transform: static (ctx, _) => ExtractNestedInits(ctx))
            .SelectMany(static (list, _) => list)
            .Where(static t => t.HasValue);

        IncrementalValuesProvider<(IMethodSymbol Method, ImmutableArray<INamedTypeSymbol> TypeArgs)?> methodInstantiations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsGenericInvocationCandidate(node),
                transform: static (ctx, _) => ExtractMethodInstantiation(ctx))
            .Where(static t => t.HasValue);

        IncrementalValuesProvider<(INamedTypeSymbol Type, ImmutableArray<INamedTypeSymbol> TypeArgs)?> typeInstantiations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ObjectCreationExpressionSyntax || node is GenericNameSyntax,
                transform: static (ctx, _) => ExtractTypeInstantiation(ctx))
            .Where(static t => t.HasValue);

        IncrementalValueProvider<ImmutableArray<INamedTypeSymbol?>> allEntities =
            fromInvocations.Collect()
                .Combine(fromMembers.Collect())
                .Combine(fromSelectProjections.Collect())
                .Combine(fromQueryProjections.Collect())
                .Select(static (pair, _) => pair.Left.Left.Left
                    .AddRange(pair.Left.Left.Right)
                    .AddRange(pair.Left.Right)
                    .AddRange(pair.Right));

        IncrementalValueProvider<GenericInstantiationIndex> genericIndex =
            methodInstantiations.Collect()
                .Combine(typeInstantiations.Collect())
                .Select(static (pair, _) =>
                {
                    GenericInstantiationIndex index = new();
                    foreach ((IMethodSymbol Method, ImmutableArray<INamedTypeSymbol> TypeArgs)? entry in pair.Left)
                    {
                        if (entry is { } e)
                        {
                            index.AddMethod(e.Method, e.TypeArgs);
                        }
                    }
                    foreach ((INamedTypeSymbol Type, ImmutableArray<INamedTypeSymbol> TypeArgs)? entry in pair.Right)
                    {
                        if (entry is { } e)
                        {
                            index.AddType(e.Type, e.TypeArgs);
                        }
                    }
                    return index;
                });

        IncrementalValueProvider<GeneratorPipelineModel> source =
            context.CompilationProvider.Combine(allEntities)
                .Combine(fromGenericInvocations.Collect())
                .Combine(selectInvocations.Collect())
                .Combine(querySelects.Collect())
                .Combine(unresolvedSelects.Collect())
                .Combine(groupByKeys.Collect())
                .Combine(nestedInits.Collect())
                .Combine(genericIndex)
                .Select(static (p, _) => new GeneratorPipelineModel(
                    compilation: p.Left.Left.Left.Left.Left.Left.Left.Left,
                    entities: p.Left.Left.Left.Left.Left.Left.Left.Right,
                    unresolvedEntities: p.Left.Left.Left.Left.Left.Left.Right,
                    selects: p.Left.Left.Left.Left.Left.Right.AddRange(p.Left.Left.Left.Left.Right),
                    unresolvedSelects: p.Left.Left.Left.Right,
                    groupKeys: p.Left.Left.Right,
                    nestedInits: p.Left.Right,
                    genericIndex: p.Right));

        context.RegisterSourceOutput(source, (spc, model) =>
        {
            ImmutableArray<INamedTypeSymbol?> entities = model.Entities;
            ImmutableArray<UnresolvedGenericEntity?> unresolvedEntities = model.UnresolvedEntities;
            ImmutableArray<SelectInvocation?> selects = model.Selects;
            ImmutableArray<UnresolvedGenericSelect?> unresolvedSelects = model.UnresolvedSelects;
            ImmutableArray<(GroupByKeyInvocation Invocation, SemanticModel Model)?> groupKeys = model.GroupKeys;
            ImmutableArray<(INamedTypeSymbol Container, string PropName, INamedTypeSymbol Nested)?> nestedInitsArr = model.NestedInits;
            GenericInstantiationIndex genericIndex = model.GenericIndex;

            HashSet<(INamedTypeSymbol, string)> nestedInitSet = new();
            foreach ((INamedTypeSymbol Container, string PropName, INamedTypeSymbol Nested)? entry in nestedInitsArr)
            {
                if (entry is { } e)
                {
                    nestedInitSet.Add((e.Container, e.PropName));
                }
            }

            HashSet<INamedTypeSymbol> unique = new(SymbolEqualityComparer.Default);
            foreach (INamedTypeSymbol? symbol in entities)
            {
                if (symbol != null
                    && !symbol.IsAbstract
                    && (!symbol.IsGenericType || symbol.IsAnonymousType)
                    && !IsFrameworkType(symbol))
                {
                    unique.Add(symbol);
                }
            }

            foreach (UnresolvedGenericEntity? unresolved in unresolvedEntities)
            {
                if (unresolved is null)
                {
                    continue;
                }
                foreach (INamedTypeSymbol concrete in ExpandTypeParameter(unresolved.TypeParameter, genericIndex))
                {
                    if (!concrete.IsAbstract
                        && (!concrete.IsGenericType || concrete.IsAnonymousType)
                        && !IsFrameworkType(concrete))
                    {
                        unique.Add(concrete);
                    }
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

            foreach (UnresolvedGenericSelect? unresolved in unresolvedSelects)
            {
                if (unresolved is null)
                {
                    continue;
                }
                foreach (SelectInvocation expanded in ExpandUnresolvedSelect(unresolved, genericIndex))
                {
                    if (!uniqueSelects.ContainsKey(expanded.Signature))
                    {
                        uniqueSelects[expanded.Signature] = expanded;

                        if (!expanded.ProjectionType.IsAbstract
                            && (!expanded.ProjectionType.IsGenericType || expanded.ProjectionType.IsAnonymousType)
                            && !IsFrameworkType(expanded.ProjectionType))
                        {
                            unique.Add(expanded.ProjectionType);
                        }
                    }
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

            string generated = EntityMaterializerEmitter.Emit("SQLite.Framework.Generated", unique, uniqueSelects.Values, uniqueGroupKeys.Values, nestedInitSet);
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
            switch (current.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    continue;
                default:
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
            && EntityReturningGenericMethods.Contains(generic.Identifier.ValueText))
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

        if (!EntityReturningGenericMethods.Contains(method.Name) || method.TypeArguments.Length != 1)
        {
            return null;
        }

        string containingType = method.ContainingType.ToDisplayString();
        if (containingType != SQLiteDatabaseFullName
            && containingType != "SQLite.Framework.Extensions.AsyncDatabaseExtensions"
            && containingType != "SQLite.Framework.Extensions.SQLiteCommandExtensions"
            && containingType != "System.Linq.Queryable"
            && containingType != "System.Linq.Enumerable")
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

        return name == "Select" || name == "SelectMany";
    }

    private static INamedTypeSymbol? ExtractProjectionTypeFromSelect(GeneratorSyntaxContext ctx)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)ctx.Node;
        if (ctx.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return null;
        }

        if (method.Name != "Select" && method.Name != "SelectMany")
        {
            return null;
        }

        string containingType = method.ContainingType.ToDisplayString();
        if (containingType != "System.Linq.Queryable" && containingType != "System.Linq.Enumerable")
        {
            return null;
        }

        INamedTypeSymbol? projection = null;
        if (method.ReturnType is INamedTypeSymbol namedReturn && namedReturn.IsGenericType && namedReturn.TypeArguments.Length > 0)
        {
            projection = namedReturn.TypeArguments[namedReturn.TypeArguments.Length - 1] as INamedTypeSymbol;
        }
        projection ??= method.TypeArguments.LastOrDefault() as INamedTypeSymbol;

        return projection;
    }

    private static ImmutableArray<(INamedTypeSymbol Container, string PropName, INamedTypeSymbol Nested)?> ExtractNestedInits(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not ObjectCreationExpressionSyntax outer || outer.Initializer == null)
        {
            return ImmutableArray<(INamedTypeSymbol, string, INamedTypeSymbol)?>.Empty;
        }

        if (!IsInsideLinqProjection(outer))
        {
            return ImmutableArray<(INamedTypeSymbol, string, INamedTypeSymbol)?>.Empty;
        }

        if (ctx.SemanticModel.GetTypeInfo(outer).Type is not INamedTypeSymbol containerType)
        {
            return ImmutableArray<(INamedTypeSymbol, string, INamedTypeSymbol)?>.Empty;
        }

        List<(INamedTypeSymbol, string, INamedTypeSymbol)?> results = new();
        foreach (ExpressionSyntax child in outer.Initializer.Expressions)
        {
            if (child is not AssignmentExpressionSyntax assign)
            {
                continue;
            }
            if (assign.Left is not IdentifierNameSyntax leftId)
            {
                continue;
            }

            INamedTypeSymbol? nestedType = null;
            if (assign.Right is ObjectCreationExpressionSyntax nestedCreate)
            {
                nestedType = ctx.SemanticModel.GetTypeInfo(nestedCreate).Type as INamedTypeSymbol;
            }
            else if (ctx.SemanticModel.GetTypeInfo(assign.Right).Type is INamedTypeSymbol rightType
                && rightType.TypeKind == TypeKind.Class
                && !IsPrimitiveLike(rightType))
            {
                nestedType = rightType;
            }

            if (nestedType == null)
            {
                continue;
            }
            results.Add((containerType, leftId.Identifier.ValueText, nestedType));
        }
        return results.ToImmutableArray();
    }

    private static bool IsPrimitiveLike(INamedTypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_Char:
            case SpecialType.System_String:
            case SpecialType.System_DateTime:
                return true;
        }
        string full = type.ToDisplayString();
        return full is "System.DateTimeOffset"
            or "System.TimeSpan"
            or "System.DateOnly"
            or "System.TimeOnly"
            or "System.Guid";
    }

    private static bool IsInsideLinqProjection(SyntaxNode node)
    {
        for (SyntaxNode? current = node; current != null; current = current.Parent)
        {
            if (current is SelectClauseSyntax)
            {
                return true;
            }

            if (current is LambdaExpressionSyntax lambda
                && lambda.Parent is ArgumentSyntax arg
                && arg.Parent is ArgumentListSyntax argList
                && argList.Parent is InvocationExpressionSyntax inv
                && inv.Expression is MemberAccessExpressionSyntax ma)
            {
                string name = ma.Name switch
                {
                    GenericNameSyntax g => g.Identifier.ValueText,
                    IdentifierNameSyntax i => i.Identifier.ValueText,
                    _ => string.Empty
                };
                if (name is "Select" or "SelectMany" or "Join" or "LeftJoin" or "RightJoin" or "GroupJoin" or "GroupBy")
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static INamedTypeSymbol? ExtractProjectionTypeFromQuery(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not SelectClauseSyntax selectClause)
        {
            return null;
        }

        ITypeSymbol? type = ctx.SemanticModel.GetTypeInfo(selectClause.Expression).Type
            ?? ctx.SemanticModel.GetTypeInfo(selectClause.Expression).ConvertedType;
        return type as INamedTypeSymbol;
    }

    private static IMethodSymbol? FindEnclosingSourceMethod(SemanticModel model, SyntaxNode startingNode)
    {
        for (SyntaxNode? cur = startingNode.Parent; cur != null; cur = cur.Parent)
        {
            switch (cur)
            {
                case MethodDeclarationSyntax methodDecl:
                    return model.GetDeclaredSymbol(methodDecl)?.OriginalDefinition;
                case ConstructorDeclarationSyntax ctorDecl:
                    return model.GetDeclaredSymbol(ctorDecl)?.OriginalDefinition;
                case AccessorDeclarationSyntax accessorDecl:
                    return model.GetDeclaredSymbol(accessorDecl)?.OriginalDefinition;
                case LocalFunctionStatementSyntax localFn:
                    return model.GetDeclaredSymbol(localFn)?.OriginalDefinition;
                case PropertyDeclarationSyntax propDecl when propDecl.ExpressionBody != null:
                    return model.GetDeclaredSymbol(propDecl)?.GetMethod?.OriginalDefinition;
            }
        }
        return null;
    }

    private static UnresolvedGenericSelect? ExtractUnresolvedSelectInvocation(GeneratorSyntaxContext ctx)
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

        if (method.TypeArguments.Length < 2 || method.TypeArguments[1] is not ITypeParameterSymbol projectionParam)
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

        IMethodSymbol? enclosingMethod = FindEnclosingSourceMethod(ctx.SemanticModel, invocation);
        INamedTypeSymbol? enclosingType = enclosingMethod?.ContainingType?.OriginalDefinition;

        SelectSignatureCtx baseCtx = BuildFluentCtx(rowSymbol, ctx.SemanticModel);

        return new UnresolvedGenericSelect(bodyExpr, baseCtx, ctx.SemanticModel, projectionParam, enclosingMethod?.OriginalDefinition, enclosingType);
    }

    private static IEnumerable<SelectInvocation> ExpandUnresolvedSelect(UnresolvedGenericSelect unresolved, GenericInstantiationIndex index)
    {
        HashSet<string> seenSignatures = new();
        foreach (Dictionary<ITypeParameterSymbol, ITypeSymbol> subs in EnumerateSubstitutionMaps(unresolved.EnclosingMethod, unresolved.EnclosingType, index))
        {
            ITypeSymbol? mapped = SelectSignatureWriter.Substitute(unresolved.ProjectionParam, subs);
            if (mapped is not INamedTypeSymbol concrete)
            {
                continue;
            }

            SelectSignatureCtx substitutedCtx = new(unresolved.BaseCtx.OuterRowType, unresolved.BaseCtx.RowBindings, unresolved.Model, subs);
            string? signature = SelectSignatureWriter.TryCompute(unresolved.Body, substitutedCtx);
            if (signature == null || !seenSignatures.Add(signature))
            {
                continue;
            }

            yield return new SelectInvocation(signature, unresolved.Body, substitutedCtx, unresolved.Model, concrete);
        }
    }

    private static IEnumerable<Dictionary<ITypeParameterSymbol, ITypeSymbol>> EnumerateSubstitutionMaps(IMethodSymbol? enclosingMethod, INamedTypeSymbol? enclosingType, GenericInstantiationIndex index)
    {
        bool typeIsGeneric = enclosingType is { TypeParameters.Length: > 0 };
        bool methodIsGeneric = enclosingMethod is { TypeParameters.Length: > 0 };

        IEnumerable<ImmutableArray<INamedTypeSymbol>> typeTuples = typeIsGeneric
            ? index.GetTypeInstantiations(enclosingType!)
            : new[] { ImmutableArray<INamedTypeSymbol>.Empty };
        IEnumerable<ImmutableArray<INamedTypeSymbol>> methodTuples = methodIsGeneric
            ? index.GetMethodInstantiations(enclosingMethod!)
            : new[] { ImmutableArray<INamedTypeSymbol>.Empty };

        foreach (ImmutableArray<INamedTypeSymbol> typeTuple in typeTuples)
        {
            foreach (ImmutableArray<INamedTypeSymbol> methodTuple in methodTuples)
            {
                Dictionary<ITypeParameterSymbol, ITypeSymbol> map = new(SymbolEqualityComparer.Default);
                if (typeIsGeneric && enclosingType!.TypeParameters.Length == typeTuple.Length)
                {
                    for (int i = 0; i < typeTuple.Length; i++)
                    {
                        map[enclosingType.TypeParameters[i]] = typeTuple[i];
                    }
                }
                if (methodIsGeneric && enclosingMethod!.TypeParameters.Length == methodTuple.Length)
                {
                    for (int i = 0; i < methodTuple.Length; i++)
                    {
                        map[enclosingMethod.TypeParameters[i]] = methodTuple[i];
                    }
                }
                if (map.Count == 0)
                {
                    continue;
                }
                yield return map;
            }
        }
    }

    private static UnresolvedGenericEntity? ExtractUnresolvedGenericEntity(GeneratorSyntaxContext ctx)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)ctx.Node;
        if (ctx.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return null;
        }

        if (!EntityReturningGenericMethods.Contains(method.Name) || method.TypeArguments.Length != 1)
        {
            return null;
        }

        string containingType = method.ContainingType.ToDisplayString();
        if (containingType != SQLiteDatabaseFullName
            && containingType != "SQLite.Framework.Extensions.AsyncDatabaseExtensions"
            && containingType != "SQLite.Framework.Extensions.SQLiteCommandExtensions"
            && containingType != "System.Linq.Queryable"
            && containingType != "System.Linq.Enumerable")
        {
            return null;
        }

        if (method.TypeArguments[0] is not ITypeParameterSymbol typeParam)
        {
            return null;
        }

        return new UnresolvedGenericEntity(typeParam);
    }

    private static IEnumerable<INamedTypeSymbol> ExpandTypeParameter(ITypeParameterSymbol param, GenericInstantiationIndex index)
    {
        HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default);
        Stack<ITypeSymbol> work = new();
        work.Push(param);
        HashSet<ITypeParameterSymbol> visited = new(SymbolEqualityComparer.Default);
        int budget = 64;

        while (work.Count > 0 && budget-- > 0)
        {
            ITypeSymbol cur = work.Pop();
            switch (cur)
            {
                case INamedTypeSymbol named when !named.IsGenericType || named.TypeArguments.All(a => a is not ITypeParameterSymbol):
                    if (named.IsGenericType)
                    {
                        seen.Add(named);
                    }
                    else
                    {
                        seen.Add(named);
                    }
                    break;
                case INamedTypeSymbol partiallyOpen:
                    foreach (ITypeSymbol arg in partiallyOpen.TypeArguments)
                    {
                        if (arg is ITypeParameterSymbol)
                        {
                            work.Push(arg);
                        }
                    }
                    break;
                case ITypeParameterSymbol tp:
                    if (!visited.Add(tp))
                    {
                        break;
                    }
                    foreach (ITypeSymbol substitution in EnumerateSubstitutions(tp, index))
                    {
                        work.Push(substitution);
                    }
                    break;
            }
        }

        return seen;
    }

    private static IEnumerable<ITypeSymbol> EnumerateSubstitutions(ITypeParameterSymbol tp, GenericInstantiationIndex index)
    {
        switch (tp.TypeParameterKind)
        {
            case TypeParameterKind.Method:
            {
                IMethodSymbol? owner = tp.DeclaringMethod?.OriginalDefinition;
                if (owner == null)
                {
                    yield break;
                }
                foreach (ImmutableArray<INamedTypeSymbol> tuple in index.GetMethodInstantiations(owner))
                {
                    if (tp.Ordinal < tuple.Length)
                    {
                        yield return tuple[tp.Ordinal];
                    }
                }
                break;
            }
            case TypeParameterKind.Type:
            {
                INamedTypeSymbol? owner = tp.DeclaringType?.OriginalDefinition;
                if (owner == null)
                {
                    yield break;
                }
                foreach (ImmutableArray<INamedTypeSymbol> tuple in index.GetTypeInstantiations(owner))
                {
                    if (tp.Ordinal < tuple.Length)
                    {
                        yield return tuple[tp.Ordinal];
                    }
                }
                break;
            }
        }
    }

    private static bool IsGenericInvocationCandidate(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax;
    }

    private static (IMethodSymbol Method, ImmutableArray<INamedTypeSymbol> TypeArgs)? ExtractMethodInstantiation(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        if (ctx.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return null;
        }

        if (method.TypeArguments.Length == 0)
        {
            return null;
        }

        IAssemblySymbol? compilationAsm = ctx.SemanticModel.Compilation.Assembly;
        if (!SymbolEqualityComparer.Default.Equals(method.OriginalDefinition.ContainingAssembly, compilationAsm))
        {
            return null;
        }

        ImmutableArray<INamedTypeSymbol>.Builder builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(method.TypeArguments.Length);
        foreach (ITypeSymbol arg in method.TypeArguments)
        {
            if (arg is not INamedTypeSymbol named)
            {
                return null;
            }
            builder.Add(named);
        }

        return (method.OriginalDefinition, builder.ToImmutable());
    }

    private static (INamedTypeSymbol Type, ImmutableArray<INamedTypeSymbol> TypeArgs)? ExtractTypeInstantiation(GeneratorSyntaxContext ctx)
    {
        INamedTypeSymbol? constructed = ctx.Node switch
        {
            ObjectCreationExpressionSyntax oce
                => ctx.SemanticModel.GetTypeInfo(oce).Type as INamedTypeSymbol,
            GenericNameSyntax gns when gns.Parent is not InvocationExpressionSyntax
                                       && gns.Parent is not MemberAccessExpressionSyntax { Name: GenericNameSyntax }
                => ctx.SemanticModel.GetSymbolInfo(gns).Symbol as INamedTypeSymbol,
            _ => null
        };

        if (constructed == null || !constructed.IsGenericType || constructed.IsUnboundGenericType)
        {
            return null;
        }

        IAssemblySymbol? compilationAsm = ctx.SemanticModel.Compilation.Assembly;
        if (!SymbolEqualityComparer.Default.Equals(constructed.OriginalDefinition.ContainingAssembly, compilationAsm))
        {
            return null;
        }

        ImmutableArray<INamedTypeSymbol>.Builder builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(constructed.TypeArguments.Length);
        foreach (ITypeSymbol arg in constructed.TypeArguments)
        {
            if (arg is not INamedTypeSymbol named)
            {
                return null;
            }
            builder.Add(named);
        }

        return (constructed.OriginalDefinition, builder.ToImmutable());
    }

    private static INamedTypeSymbol? ExtractEntityFromMember(GeneratorSyntaxContext ctx)
    {
        ITypeSymbol? type = ctx.Node switch
        {
            PropertyDeclarationSyntax prop => ctx.SemanticModel.GetTypeInfo(prop.Type).Type,
            FieldDeclarationSyntax field => ctx.SemanticModel.GetTypeInfo(field.Declaration.Type).Type,
            _ => null
        };

        for (INamedTypeSymbol? current = type as INamedTypeSymbol; current != null; current = current.BaseType)
        {
            if (current.IsGenericType
                && current.OriginalDefinition.ToDisplayString() == "SQLite.Framework.SQLiteTable<T>"
                && current.TypeArguments.Length == 1)
            {
                return current.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        return null;
    }
}
