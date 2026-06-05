using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// Describes how a row type expands into its properties and leaves.
/// </summary>
public sealed class RowExpansion
{
    /// <summary>
    /// Creates a new row expansion.
    /// </summary>
    public RowExpansion(INamedTypeSymbol rowType, List<IPropertySymbol> properties, List<LeafInfo> leaves)
    {
        RowType = rowType;
        Properties = properties;
        Leaves = leaves;
    }

    /// <summary>
    /// The row type being expanded.
    /// </summary>
    public INamedTypeSymbol RowType { get; }

    /// <summary>
    /// The properties of the row type.
    /// </summary>
    public List<IPropertySymbol> Properties { get; }

    /// <summary>
    /// The leaves produced by the expansion.
    /// </summary>
    public List<LeafInfo> Leaves { get; }
}
