using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// Holds the context needed to build a select signature.
/// </summary>
public readonly struct SelectSignatureCtx
{
    /// <summary>
    /// Creates a new select signature context without type argument substitutions.
    /// </summary>
    public SelectSignatureCtx(ITypeSymbol outerRowType, Dictionary<ISymbol, RowBinding> rowBindings, SemanticModel model)
        : this(outerRowType, rowBindings, model, null)
    {
    }

    /// <summary>
    /// Creates a new select signature context with optional type argument substitutions.
    /// </summary>
    public SelectSignatureCtx(ITypeSymbol outerRowType, Dictionary<ISymbol, RowBinding> rowBindings, SemanticModel model, Dictionary<ITypeParameterSymbol, ITypeSymbol>? typeArgSubstitutions)
    {
        OuterRowType = outerRowType;
        RowBindings = rowBindings;
        Model = model;
        ParameterSubstitutions = new Dictionary<ISymbol, ExpressionSyntax>(SymbolEqualityComparer.Default);
        NullableRangeVars = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        TypeArgSubstitutions = typeArgSubstitutions ?? new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
    }

    private SelectSignatureCtx(ITypeSymbol outerRowType, Dictionary<ISymbol, RowBinding> rowBindings, SemanticModel model, Dictionary<ITypeParameterSymbol, ITypeSymbol> typeArgSubstitutions, Dictionary<ISymbol, ExpressionSyntax> parameterSubstitutions, HashSet<ISymbol> nullableRangeVars, bool isInChecked)
    {
        OuterRowType = outerRowType;
        RowBindings = rowBindings;
        Model = model;
        ParameterSubstitutions = parameterSubstitutions;
        NullableRangeVars = nullableRangeVars;
        TypeArgSubstitutions = typeArgSubstitutions;
        IsInChecked = isInChecked;
    }

    /// <summary>
    /// The row type of the outer query.
    /// </summary>
    public ITypeSymbol OuterRowType { get; }

    /// <summary>
    /// Maps range variable symbols to their row bindings.
    /// </summary>
    public Dictionary<ISymbol, RowBinding> RowBindings { get; }

    /// <summary>
    /// The semantic model for this context.
    /// </summary>
    public SemanticModel Model { get; }

    /// <summary>
    /// Maps parameter symbols to the syntax that replaces them.
    /// </summary>
    public Dictionary<ISymbol, ExpressionSyntax> ParameterSubstitutions { get; }

    /// <summary>
    /// The set of range variables that can be null.
    /// </summary>
    public HashSet<ISymbol> NullableRangeVars { get; }

    /// <summary>
    /// Maps type parameters to the concrete types that replace them.
    /// </summary>
    public Dictionary<ITypeParameterSymbol, ITypeSymbol> TypeArgSubstitutions { get; }

    /// <summary>
    /// Whether the current expression is inside a checked context.
    /// </summary>
    public bool IsInChecked { get; }

    /// <summary>
    /// Returns a copy of this context with <see cref="IsInChecked"/> set to <see langword="true"/>.
    /// </summary>
    public SelectSignatureCtx WithChecked()
    {
        return new SelectSignatureCtx(OuterRowType, RowBindings, Model, TypeArgSubstitutions, ParameterSubstitutions, NullableRangeVars, isInChecked: true);
    }

    /// <summary>
    /// Returns a copy of this context with <see cref="IsInChecked"/> set to <see langword="false"/>.
    /// </summary>
    public SelectSignatureCtx WithUnchecked()
    {
        return new SelectSignatureCtx(OuterRowType, RowBindings, Model, TypeArgSubstitutions, ParameterSubstitutions, NullableRangeVars, isInChecked: false);
    }
}
