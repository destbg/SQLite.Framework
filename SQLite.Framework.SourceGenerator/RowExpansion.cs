using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator;

internal sealed class RowExpansion
{
    public RowExpansion(INamedTypeSymbol rowType, List<IPropertySymbol> properties, List<LeafInfo> leaves)
    {
        RowType = rowType;
        Properties = properties;
        Leaves = leaves;
    }

    public INamedTypeSymbol RowType { get; }
    public List<IPropertySymbol> Properties { get; }
    public List<LeafInfo> Leaves { get; }
}
