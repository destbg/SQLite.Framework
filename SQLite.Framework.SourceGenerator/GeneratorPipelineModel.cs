using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SQLite.Framework.SourceGenerator;

/// <summary>
/// Bundles every input the <see cref="QueryMaterializerGenerator"/> needs in its final
/// emit stage. Created by combining the per-syntax extractors plus the compilation and
/// the cross-syntax <see cref="GenericInstantiationIndex"/>.
/// </summary>
internal sealed class GeneratorPipelineModel
{
    public GeneratorPipelineModel(Compilation compilation, ImmutableArray<INamedTypeSymbol?> entities, ImmutableArray<UnresolvedGenericEntity?> unresolvedEntities, ImmutableArray<SelectInvocation?> selects, ImmutableArray<UnresolvedGenericSelect?> unresolvedSelects, ImmutableArray<(GroupByKeyInvocation Invocation, SemanticModel Model)?> groupKeys, ImmutableArray<(INamedTypeSymbol Container, string PropName, INamedTypeSymbol Nested)?> nestedInits, GenericInstantiationIndex genericIndex)
    {
        Compilation = compilation;
        Entities = entities;
        UnresolvedEntities = unresolvedEntities;
        Selects = selects;
        UnresolvedSelects = unresolvedSelects;
        GroupKeys = groupKeys;
        NestedInits = nestedInits;
        GenericIndex = genericIndex;
    }

    public Compilation Compilation { get; }
    public ImmutableArray<INamedTypeSymbol?> Entities { get; }
    public ImmutableArray<UnresolvedGenericEntity?> UnresolvedEntities { get; }
    public ImmutableArray<SelectInvocation?> Selects { get; }
    public ImmutableArray<UnresolvedGenericSelect?> UnresolvedSelects { get; }
    public ImmutableArray<(GroupByKeyInvocation Invocation, SemanticModel Model)?> GroupKeys { get; }
    public ImmutableArray<(INamedTypeSymbol Container, string PropName, INamedTypeSymbol Nested)?> NestedInits { get; }
    public GenericInstantiationIndex GenericIndex { get; }
}
