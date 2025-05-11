namespace SQLite.Framework.Internals.Models;

internal class TableIndexWrapper
{
    private readonly Dictionary<char, int> dict = [];

    public int this[char c]
    {
        get
        {
            if (!dict.TryGetValue(c, out int index))
            {
                index = dict.Count;
                dict[c] = index;
            }

            return index;
        }
        set { dict[c] = value; }
    }
}