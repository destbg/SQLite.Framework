using System.Collections;

namespace SQLite.Framework.Internals.Models;

internal sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    private readonly IReadOnlyList<TElement> elements;

    public Grouping(TKey key, IReadOnlyList<TElement> elements)
    {
        Key = key;
        this.elements = elements;
    }

    public TKey Key { get; }

    public IEnumerator<TElement> GetEnumerator()
    {
        return elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
