using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class TranslationPatternsTests
{
    [Fact]
    public void IsArrayLambdaMethod_Exists_ReturnsTrue()
    {
        Assert.True(TranslationPatterns.IsArrayLambdaMethod(nameof(Array.Exists)));
    }

    [Fact]
    public void IsArrayLambdaMethod_Find_ReturnsTrue()
    {
        Assert.True(TranslationPatterns.IsArrayLambdaMethod(nameof(Array.Find)));
    }

    [Fact]
    public void IsArrayLambdaMethod_FindAll_ReturnsTrue()
    {
        Assert.True(TranslationPatterns.IsArrayLambdaMethod(nameof(Array.FindAll)));
    }

    [Fact]
    public void IsArrayLambdaMethod_FindIndex_ReturnsTrue()
    {
        Assert.True(TranslationPatterns.IsArrayLambdaMethod(nameof(Array.FindIndex)));
    }

    [Fact]
    public void IsArrayLambdaMethod_FindLast_ReturnsTrue()
    {
        Assert.True(TranslationPatterns.IsArrayLambdaMethod(nameof(Array.FindLast)));
    }

    [Fact]
    public void IsArrayLambdaMethod_FindLastIndex_ReturnsTrue()
    {
        Assert.True(TranslationPatterns.IsArrayLambdaMethod(nameof(Array.FindLastIndex)));
    }

    [Fact]
    public void IsArrayLambdaMethod_TrueForAll_ReturnsTrue()
    {
        Assert.True(TranslationPatterns.IsArrayLambdaMethod(nameof(Array.TrueForAll)));
    }

    [Fact]
    public void IsArrayLambdaMethod_ConvertAll_ReturnsTrue()
    {
        Assert.True(TranslationPatterns.IsArrayLambdaMethod(nameof(Array.ConvertAll)));
    }

    [Fact]
    public void IsArrayLambdaMethod_UnknownName_ReturnsFalse()
    {
        Assert.False(TranslationPatterns.IsArrayLambdaMethod("Sort"));
    }
}
