namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Stack-allocated buffer that accumulates up to 8 <see cref="SQLiteParameter"/> references
/// without heap allocation, then materializes a single exact-size array on finalize.
/// Falls back to a <see cref="List{T}"/> for entries beyond 8.
/// </summary>
internal ref struct InlineParameterBuffer8
{
    private InlineParamSlots inline;
    private List<SQLiteParameter>? overflow;

    public int Count { get; private set; }

    public void Add(SQLiteParameter parameter)
    {
        if (Count < 8)
        {
            ((Span<SQLiteParameter?>)inline)[Count] = parameter;
        }
        else
        {
            (overflow ??= new List<SQLiteParameter>()).Add(parameter);
        }

        Count++;
    }

    public void AddRange(SQLiteParameter[] parameters)
    {
        for (int i = 0; i < parameters.Length; i++)
        {
            Add(parameters[i]);
        }
    }

    public SQLiteParameter[] ToArray()
    {
        SQLiteParameter[] result = new SQLiteParameter[Count];
        int inlineCount = Count < 8 ? Count : 8;
        Span<SQLiteParameter?> span = inline;
        for (int i = 0; i < inlineCount; i++)
        {
            result[i] = span[i]!;
        }

        overflow?.CopyTo(result, inlineCount);

        return result;
    }
}

[InlineArray(8)]
internal struct InlineParamSlots
{
    private SQLiteParameter? slot;
}
