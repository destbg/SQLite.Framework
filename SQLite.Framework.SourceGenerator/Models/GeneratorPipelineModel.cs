using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using SQLite.Framework.SourceGenerator.Writers;

namespace SQLite.Framework.SourceGenerator.Models;

/// <summary>
/// Bundles every input the <see cref="QueryMaterializerGenerator"/> needs in its final
/// emit stage. Created by combining the per-syntax extractors plus the compilation and
/// the cross-syntax <see cref="GenericInstantiationIndex"/>.
/// </summary>
public sealed class GeneratorPipelineModel
{
    /// <summary>
    /// Creates a new pipeline model from the collected stage inputs.
    /// </summary>
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

    /// <summary>
    /// The current compilation.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// The entity types found at callsites.
    /// </summary>
    public ImmutableArray<INamedTypeSymbol?> Entities { get; }

    /// <summary>
    /// The entity references that still use open type parameters.
    /// </summary>
    public ImmutableArray<UnresolvedGenericEntity?> UnresolvedEntities { get; }

    /// <summary>
    /// The select invocations found at callsites.
    /// </summary>
    public ImmutableArray<SelectInvocation?> Selects { get; }

    /// <summary>
    /// The select invocations that still use open type parameters.
    /// </summary>
    public ImmutableArray<UnresolvedGenericSelect?> UnresolvedSelects { get; }

    /// <summary>
    /// The group-by key invocations found at callsites.
    /// </summary>
    public ImmutableArray<(GroupByKeyInvocation Invocation, SemanticModel Model)?> GroupKeys { get; }

    /// <summary>
    /// The nested initializer pairs found at callsites.
    /// </summary>
    public ImmutableArray<(INamedTypeSymbol Container, string PropName, INamedTypeSymbol Nested)?> NestedInits { get; }

    /// <summary>
    /// The index of generic instantiations seen across callsites.
    /// </summary>
    public GenericInstantiationIndex GenericIndex { get; }
}
