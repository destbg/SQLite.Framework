using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// Describes a single leaf value read from a row.
/// </summary>
public sealed class LeafInfo
{
    /// <summary>
    /// Creates a new leaf info.
    /// </summary>
    public LeafInfo(SyntaxNode node, ITypeSymbol type, string varName, bool isNullable = false, bool isReflected = false)
    {
        Node = node;
        Type = type;
        VarName = varName;
        IsNullable = isNullable;
        IsReflected = isReflected;
    }

    /// <summary>
    /// The syntax node this leaf comes from.
    /// </summary>
    public SyntaxNode Node { get; }

    /// <summary>
    /// The type of the leaf value.
    /// </summary>
    public ITypeSymbol Type { get; }

    /// <summary>
    /// The variable name used for this leaf.
    /// </summary>
    public string VarName { get; }

    /// <summary>
    /// True if the leaf value can be null.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// True if the leaf is read through reflection.
    /// </summary>
    public bool IsReflected { get; }
}
