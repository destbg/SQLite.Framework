using System.Collections;
using System.Reflection;
using System.Text.Json;
using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class JsonCollectionMaterializerPrimitiveTests
{
    [Fact]
    public void PrimitiveAndFrameworkElementTypesMaterializeWithoutJsonMetadata()
    {
        Guid guid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        DateTime dateTime = new(2026, 9, 2, 3, 4, 5, DateTimeKind.Utc);
        DateTimeOffset dateTimeOffset = new(2026, 9, 2, 3, 4, 5, TimeSpan.FromHours(3));

        Assert.Equal(true, ReadSingle(typeof(bool), "true"));
        Assert.Equal((byte)7, ReadSingle(typeof(byte), "7"));
        Assert.Equal((sbyte)-7, ReadSingle(typeof(sbyte), "-7"));
        Assert.Equal((short)-70, ReadSingle(typeof(short), "-70"));
        Assert.Equal((ushort)70, ReadSingle(typeof(ushort), "70"));
        Assert.Equal(700, ReadSingle(typeof(int), "700"));
        Assert.Equal(700U, ReadSingle(typeof(uint), "700"));
        Assert.Equal(700L, ReadSingle(typeof(long), "700"));
        Assert.Equal(700UL, ReadSingle(typeof(ulong), "700"));
        Assert.Equal(1.25F, ReadSingle(typeof(float), "1.25"));
        Assert.Equal(2.5D, ReadSingle(typeof(double), "2.5"));
        Assert.Equal(3.75M, ReadSingle(typeof(decimal), "3.75"));
        Assert.Equal('x', ReadSingle(typeof(char), "\"x\""));
        Assert.Equal("text", ReadSingle(typeof(string), "\"text\""));
        Assert.Equal(dateTime, ReadSingle(typeof(DateTime), $"\"{dateTime:O}\""));
        Assert.Equal(dateTimeOffset, ReadSingle(typeof(DateTimeOffset), $"\"{dateTimeOffset:O}\""));
        Assert.Equal(TimeSpan.FromMinutes(90), ReadSingle(typeof(TimeSpan), "\"01:30:00\""));
        Assert.Equal(new DateOnly(2026, 9, 2), ReadSingle(typeof(DateOnly), "\"2026-09-02\""));
        Assert.Equal(new TimeOnly(3, 4, 5), ReadSingle(typeof(TimeOnly), "\"03:04:05\""));
        Assert.Equal(guid, ReadSingle(typeof(Guid), $"\"{guid}\""));
        Assert.Equal(new byte[] { 1, 2, 3 }, (byte[])ReadSingle(typeof(byte[]), "\"AQID\"")!);

        JsonElement element = (JsonElement)ReadSingle(typeof(JsonElement), "{\"value\":7}")!;
        Assert.Equal(7, element.GetProperty("value").GetInt32());

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            PrimitivePayload payload = (PrimitivePayload)ReadSingle(typeof(PrimitivePayload), "{\"Value\":9}")!;
            Assert.Equal(9, payload.Value);
        }
    }

    [Fact]
    public void NullAndEveryEnumUnderlyingTypeMaterializeWithoutJsonMetadata()
    {
        Assert.Equal(0, ReadSingle(typeof(int), "null"));
        Assert.Null(ReadSingle(typeof(int?), "null"));
        Assert.Null(ReadSingle(typeof(string), "null"));
        Assert.Equal(IntEnum.One, ReadSingle(typeof(IntEnum), "\"One\""));
        Assert.Equal(ByteEnum.One, ReadSingle(typeof(ByteEnum), "1"));
        Assert.Equal(SByteEnum.One, ReadSingle(typeof(SByteEnum), "1"));
        Assert.Equal(ShortEnum.One, ReadSingle(typeof(ShortEnum), "1"));
        Assert.Equal(UShortEnum.One, ReadSingle(typeof(UShortEnum), "1"));
        Assert.Equal(IntEnum.One, ReadSingle(typeof(IntEnum), "1"));
        Assert.Equal(UIntEnum.One, ReadSingle(typeof(UIntEnum), "1"));
        Assert.Equal(LongEnum.One, ReadSingle(typeof(LongEnum), "1"));
        Assert.Equal(ULongEnum.One, ReadSingle(typeof(ULongEnum), "1"));

        using JsonDocument document = JsonDocument.Parse("4");
        MethodInfo readInteger = typeof(JsonCollectionMaterializer).GetMethod(
            "ReadInteger",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal(4L, readInteger.Invoke(null, [document.RootElement, typeof(string)]));
    }

    [Fact]
    public void RegisteredCollectionMaterializerIsUsedAndCopiedByTheOptionsBuilder()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.JsonCollectionMaterializers[typeof(List<int>)] = (_, _) => new List<int> { 42 };
        SQLiteOptions options = builder.Build();
        SQLiteOptionsBuilder copy = new(options);

        Func<string, object?> materializer = JsonCollectionMaterializer.TryBuild(typeof(List<int>), copy.Build())!;
        List<int> values = (List<int>)materializer("[]")!;

        Assert.Equal([42], values);
    }

    private static object? ReadSingle(Type elementType, string jsonElement)
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:").Build();
        Type collectionType = typeof(List<>).MakeGenericType(elementType);
        Func<string, object?> materializer = JsonCollectionMaterializer.TryBuild(collectionType, options)!;
        IList values = (IList)materializer($"[{jsonElement}]")!;
        return values[0];
    }

    private sealed class PrimitivePayload
    {
        public int Value { get; set; }
    }

    private enum ByteEnum : byte { One = 1 }
    private enum SByteEnum : sbyte { One = 1 }
    private enum ShortEnum : short { One = 1 }
    private enum UShortEnum : ushort { One = 1 }
    private enum IntEnum { One = 1 }
    private enum UIntEnum : uint { One = 1 }
    private enum LongEnum : long { One = 1 }
    private enum ULongEnum : ulong { One = 1 }
}
