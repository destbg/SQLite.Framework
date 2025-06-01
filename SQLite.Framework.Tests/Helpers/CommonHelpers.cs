namespace SQLite.Framework.Tests.Helpers;

public static class CommonHelpers
{
    public static int ConvertString(string s)
    {
        return -1;
    }

    public static long ConvertStringLong(string s)
    {
        return -1;
    }

    public static int GetValue()
    {
        return 1;
    }

    public static int[] GetArray()
    {
        return [1, 2, 3];
    }

    public static IndexElement GetIndexer()
    {
        return new IndexElement();
    }
    
    public class IndexElement
    {
        public int this[int index] => index + 1;
    }
}