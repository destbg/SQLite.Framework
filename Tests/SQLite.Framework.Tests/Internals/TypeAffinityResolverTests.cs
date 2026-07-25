using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class TypeAffinityResolverTests
{
    [Theory]
    [InlineData("INT", "Integer")]
    [InlineData("INTEGER", "Integer")]
    [InlineData("BIGINT", "Integer")]
    [InlineData("UNSIGNED BIG INT", "Integer")]
    [InlineData("CHARACTER(20)", "Text")]
    [InlineData("VARCHAR(255)", "Text")]
    [InlineData("CLOB", "Text")]
    [InlineData("TEXT", "Text")]
    [InlineData("", "Blob")]
    [InlineData("BLOB", "Blob")]
    [InlineData("REAL", "Real")]
    [InlineData("FLOAT", "Real")]
    [InlineData("DOUBLE PRECISION", "Real")]
    [InlineData("NUMERIC", "Numeric")]
    [InlineData("DECIMAL(10,5)", "Numeric")]
    [InlineData("BOOLEAN", "Numeric")]
    [InlineData("DATETIME", "Numeric")]
    public void DeclaredTypeResolvesToDocumentedAffinity(string declaredType, string expected)
    {
        Assert.Equal(expected, TypeAffinityResolver.Resolve(declaredType).ToString());
    }
}
