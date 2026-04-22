using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator;

internal readonly struct RowBinding
{
    public RowBinding(string? memberName, ITypeSymbol rangeType)
        : this(memberName == null ? null : [(memberName, rangeType)], rangeType)
    {
    }

    public RowBinding(IReadOnlyList<(string Name, ITypeSymbol Type)>? memberPath, ITypeSymbol rangeType)
    {
        MemberPath = memberPath;
        RangeType = rangeType;
    }

    public IReadOnlyList<(string Name, ITypeSymbol Type)>? MemberPath { get; }

    public string? MemberName => MemberPath is { Count: 1 } p ? p[0].Name : null;

    public ITypeSymbol RangeType { get; }
}
