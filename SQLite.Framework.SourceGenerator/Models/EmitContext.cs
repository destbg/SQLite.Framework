using System.Text;
using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// Holds the state used while emitting one materializer method.
/// </summary>
public sealed class EmitContext
{
    /// <summary>
    /// Creates a new emit context for the given select signature.
    /// </summary>
    public EmitContext(SelectSignatureCtx writerCtx)
    {
        WriterCtx = writerCtx;
    }

    /// <summary>
    /// The select signature context this emit run is based on.
    /// </summary>
    public SelectSignatureCtx WriterCtx { get; }

    /// <summary>
    /// The semantic model taken from the writer context.
    /// </summary>
    public SemanticModel Model => WriterCtx.Model;

    /// <summary>
    /// The assembly that the generator is running against.
    /// </summary>
    public IAssemblySymbol GeneratorAssembly => Model.Compilation.Assembly;

    /// <summary>
    /// The list of leaf values collected during emit.
    /// </summary>
    public List<LeafInfo> Leaves { get; } = new();

    /// <summary>
    /// Maps a syntax node to its leaf index.
    /// </summary>
    public Dictionary<SyntaxNode, int> LeafIndexBySyntax { get; } = new();

    /// <summary>
    /// Maps a syntax node to its row expansion.
    /// </summary>
    public Dictionary<SyntaxNode, RowExpansion> RowExpansions { get; } = new();

    /// <summary>
    /// The text of the helper methods emitted so far.
    /// </summary>
    public StringBuilder HelperMethods { get; } = new();

    /// <summary>
    /// Counter used to name helper methods.
    /// </summary>
    public int HelperCounter { get; set; }

    /// <summary>
    /// Counter used to name type slots.
    /// </summary>
    public int TypeSlotCounter { get; set; }

    /// <summary>
    /// Counter used to name member slots.
    /// </summary>
    public int MemberSlotCounter { get; set; }

    /// <summary>
    /// The name of the method that owns this emit run.
    /// </summary>
    public string OwnerMethodName { get; set; } = "";

    /// <summary>
    /// Maps a symbol to the index of the first leaf in its nullable range.
    /// </summary>
    public Dictionary<ISymbol, int> NullableRangeFirstLeaf { get; } = new(SymbolEqualityComparer.Default);
}
