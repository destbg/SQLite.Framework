using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

internal readonly struct SelectSignatureCtx
{
    public SelectSignatureCtx(ITypeSymbol outerRowType, Dictionary<ISymbol, RowBinding> rowBindings, SemanticModel model)
        : this(outerRowType, rowBindings, model, null)
    {
    }

    public SelectSignatureCtx(ITypeSymbol outerRowType, Dictionary<ISymbol, RowBinding> rowBindings, SemanticModel model, Dictionary<ITypeParameterSymbol, ITypeSymbol>? typeArgSubstitutions)
    {
        OuterRowType = outerRowType;
        RowBindings = rowBindings;
        Model = model;
        ParameterSubstitutions = new Dictionary<ISymbol, ExpressionSyntax>(SymbolEqualityComparer.Default);
        NullableRangeVars = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        TypeArgSubstitutions = typeArgSubstitutions ?? new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
    }

    public ITypeSymbol OuterRowType { get; }
    public Dictionary<ISymbol, RowBinding> RowBindings { get; }
    public SemanticModel Model { get; }
    public Dictionary<ISymbol, ExpressionSyntax> ParameterSubstitutions { get; }
    public HashSet<ISymbol> NullableRangeVars { get; }
    public Dictionary<ITypeParameterSymbol, ITypeSymbol> TypeArgSubstitutions { get; }
}
