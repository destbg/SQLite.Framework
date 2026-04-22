using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SQLite.Framework.SourceGenerator;

internal readonly struct SelectSignatureCtx
{
    public SelectSignatureCtx(ITypeSymbol outerRowType, Dictionary<ISymbol, RowBinding> rowBindings, SemanticModel model)
    {
        OuterRowType = outerRowType;
        RowBindings = rowBindings;
        Model = model;
        ParameterSubstitutions = new Dictionary<ISymbol, ExpressionSyntax>(SymbolEqualityComparer.Default);
        NullableRangeVars = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
    }

    public ITypeSymbol OuterRowType { get; }
    public Dictionary<ISymbol, RowBinding> RowBindings { get; }
    public SemanticModel Model { get; }
    public Dictionary<ISymbol, ExpressionSyntax> ParameterSubstitutions { get; }
    public HashSet<ISymbol> NullableRangeVars { get; }
}
