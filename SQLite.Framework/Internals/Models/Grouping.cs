using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Concrete <see cref="IGrouping{TKey, TElement}" /> implementation used by the framework when materializing
/// results of <c>IQueryable.GroupBy(...)</c> without a projection.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    private readonly IReadOnlyList<TElement> elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="Grouping{TKey, TElement}" /> class.
    /// </summary>
    public Grouping(TKey key, IReadOnlyList<TElement> elements)
    {
        Key = key;
        this.elements = elements;
    }

    /// <inheritdoc />
    public TKey Key { get; }

    /// <inheritdoc />
    public IEnumerator<TElement> GetEnumerator()
    {
        return elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
