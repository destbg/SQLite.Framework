using System.Linq.Expressions;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Tests;

public class TypeMetadataTests
{
    private static SQLiteOptions BuildOptions(Action<SQLiteOptionsBuilder>? configure = null)
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        configure?.Invoke(builder);
        return builder.Build();
    }

    [Fact]
    public void HasTextOrBlobConverter_NoConverterRegistered_ReturnsFalse()
    {
        SQLiteOptions options = BuildOptions();

        Assert.False(options.HasTextOrBlobConverter(typeof(string)));
    }

    [Fact]
    public void HasTextOrBlobConverter_TextConverter_ReturnsTrue()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(MyEntity)] = new TextConverter());

        Assert.True(options.HasTextOrBlobConverter(typeof(MyEntity)));
    }

    [Fact]
    public void HasTextOrBlobConverter_BlobConverter_ReturnsTrue()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(MyEntity)] = new BlobConverter());

        Assert.True(options.HasTextOrBlobConverter(typeof(MyEntity)));
    }

    [Fact]
    public void HasTextOrBlobConverter_IntegerConverter_ReturnsFalse()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(MyEntity)] = new IntegerConverter());

        Assert.False(options.HasTextOrBlobConverter(typeof(MyEntity)));
    }

    [Fact]
    public void HasTextOrBlobConverter_NullableType_StripsAndChecks()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(MyValue)] = new TextConverter());

        Assert.True(options.HasTextOrBlobConverter(typeof(MyValue?)));
    }

    [Fact]
    public void HasJsonConverter_NullType_ReturnsFalse()
    {
        SQLiteOptions options = BuildOptions();

        Assert.False(options.HasJsonConverter(null));
    }

    [Fact]
    public void HasJsonConverter_NoConverter_ReturnsFalse()
    {
        SQLiteOptions options = BuildOptions();

        Assert.False(options.HasJsonConverter(typeof(MyEntity)));
    }

    [Fact]
    public void HasJsonConverter_NonGenericConverter_ReturnsFalse()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(MyEntity)] = new TextConverter());

        Assert.False(options.HasJsonConverter(typeof(MyEntity)));
    }

    [Fact]
    public void HasJsonConverter_GenericNonJsonConverter_ReturnsFalse()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(MyEntity)] = new GenericNonJsonConverter<MyEntity>());

        Assert.False(options.HasJsonConverter(typeof(MyEntity)));
    }

    [Fact]
    public void HasJsonConverter_JsonConverter_ReturnsTrue()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address));

        Assert.True(options.HasJsonConverter(typeof(Address)));
    }

#if !SQLITECIPHER
    [Fact]
    public void HasJsonConverter_JsonbConverter_ReturnsTrue()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));

        Assert.True(options.HasJsonConverter(typeof(Address)));
    }
#endif

    [Fact]
    public void CoercedResultType_NoConverter_ReturnsDeclared()
    {
        SQLiteOptions options = BuildOptions();

        Type result = options.CoercedResultType(typeof(IList<int>), typeof(List<int>));

        Assert.Equal(typeof(IList<int>), result);
    }

    [Fact]
    public void CoercedResultType_DeclaredAssignableFromSource_ReturnsSource()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(MyEntity)] = new TextConverter());

        Type result = options.CoercedResultType(typeof(object), typeof(MyEntity));

        Assert.Equal(typeof(MyEntity), result);
    }

    [Fact]
    public void CoercedResultType_EnumerableElementMatch_ReturnsSource()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(List<int>)] = new TextConverter());

        Type result = options.CoercedResultType(typeof(IEnumerable<int>), typeof(List<int>));

        Assert.Equal(typeof(List<int>), result);
    }

    [Fact]
    public void CoercedResultType_EnumerableElementMismatch_ReturnsDeclared()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(List<int>)] = new TextConverter());

        Type result = options.CoercedResultType(typeof(IEnumerable<long>), typeof(List<int>));

        Assert.Equal(typeof(IEnumerable<long>), result);
    }

    [Fact]
    public void CoercedResultType_NotEnumerableNorAssignable_ReturnsDeclared()
    {
        SQLiteOptions options = BuildOptions(b =>
            b.TypeConverters[typeof(MyEntity)] = new TextConverter());

        Type result = options.CoercedResultType(typeof(string), typeof(MyEntity));

        Assert.Equal(typeof(string), result);
    }

    private sealed class MyEntity
    {
    }

    private struct MyValue
    {
    }

    private sealed class TextConverter : ISQLiteTypeConverter
    {
        public SQLiteColumnType ColumnType => SQLiteColumnType.Text;
        public object? ToDatabase(object? value) => value;
        public object? FromDatabase(object? value) => value;
    }

    private sealed class BlobConverter : ISQLiteTypeConverter
    {
        public SQLiteColumnType ColumnType => SQLiteColumnType.Blob;
        public object? ToDatabase(object? value) => value;
        public object? FromDatabase(object? value) => value;
    }

    private sealed class IntegerConverter : ISQLiteTypeConverter
    {
        public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;
        public object? ToDatabase(object? value) => value;
        public object? FromDatabase(object? value) => value;
    }

    private sealed class GenericNonJsonConverter<T> : ISQLiteTypeConverter
    {
        public SQLiteColumnType ColumnType => SQLiteColumnType.Text;
        public object? ToDatabase(object? value) => value;
        public object? FromDatabase(object? value) => value;
    }
}
