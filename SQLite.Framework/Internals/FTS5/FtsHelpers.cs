namespace SQLite.Framework.Internals.FTS5;

internal static class FtsHelpers
{
    public static List<FtsQueryPart> RenderFTSMatch(Expression predicate, SQLVisitor visitor)
    {
        FtsRenderState state = new(visitor);
        try
        {
            state.Write(predicate, parentPrecedence: 0);
            state.FlushLiteral();
            return state.Parts;
        }
        finally
        {
            state.ReleaseBuffer();
        }
    }
}
