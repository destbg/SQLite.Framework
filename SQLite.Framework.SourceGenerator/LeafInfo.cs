using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator;

internal sealed class LeafInfo
{
    public LeafInfo(SyntaxNode node, ITypeSymbol type, string varName, bool isNullable = false)
    {
        Node = node;
        Type = type;
        VarName = varName;
        IsNullable = isNullable;
    }

    public SyntaxNode Node { get; }
    public ITypeSymbol Type { get; }
    public string VarName { get; }
    public bool IsNullable { get; }
}
