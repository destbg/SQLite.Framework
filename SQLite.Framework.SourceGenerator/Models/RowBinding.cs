using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// Binds a range variable to a member path and its row type.
/// </summary>
public readonly struct RowBinding
{
    /// <summary>
    /// Creates a row binding from a single member name.
    /// </summary>
    public RowBinding(string? memberName, ITypeSymbol rangeType)
        : this(memberName == null ? null : [(memberName, rangeType)], rangeType)
    {
    }

    /// <summary>
    /// Creates a row binding from a member path.
    /// </summary>
    public RowBinding(IReadOnlyList<(string Name, ITypeSymbol Type)>? memberPath, ITypeSymbol rangeType)
    {
        MemberPath = memberPath;
        RangeType = rangeType;
    }

    /// <summary>
    /// The path of members that reach this binding.
    /// </summary>
    public IReadOnlyList<(string Name, ITypeSymbol Type)>? MemberPath { get; }

    /// <summary>
    /// The single member name when the path has one step.
    /// </summary>
    public string? MemberName => MemberPath is { Count: 1 } p ? p[0].Name : null;

    /// <summary>
    /// The row type of the range variable.
    /// </summary>
    public ITypeSymbol RangeType { get; }
}
