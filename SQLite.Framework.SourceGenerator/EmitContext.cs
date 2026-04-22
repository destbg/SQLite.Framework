using System.Text;
using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator;

internal sealed class EmitContext
{
    public EmitContext(SelectSignatureCtx writerCtx)
    {
        WriterCtx = writerCtx;
    }

    public SelectSignatureCtx WriterCtx { get; }
    public SemanticModel Model => WriterCtx.Model;
    public List<LeafInfo> Leaves { get; } = new();
    public Dictionary<SyntaxNode, int> LeafIndexBySyntax { get; } = new();
    public Dictionary<SyntaxNode, RowExpansion> RowExpansions { get; } = new();
    public StringBuilder HelperMethods { get; } = new();
    public int HelperCounter;
    public int TypeSlotCounter;
    public int MemberSlotCounter;
    public string OwnerMethodName = "";
    public Dictionary<ISymbol, int> NullableRangeFirstLeaf { get; } = new(SymbolEqualityComparer.Default);
}
