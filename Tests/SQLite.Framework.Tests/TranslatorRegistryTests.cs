using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Tests;

public class TranslatorRegistryTests
{
    private static SQLiteOptions BuildOptions(Action<SQLiteOptionsBuilder>? configure = null)
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        configure?.Invoke(builder);
        return builder.Build();
    }

    private static SQLiteMemberTranslator Identity =>
        ctx => new SQLiteExpression(typeof(int), 0, "x", null);

    [Fact]
    public void TryGetMethodTranslator_ExactMatch_ReturnsTrue()
    {
        MethodInfo method = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;
        SQLiteOptions options = BuildOptions(b => b.MemberTranslators[method] = Identity);

        bool result = options.TryGetMethodTranslator(method, out SQLiteMemberTranslator? translator);

        Assert.True(result);
        Assert.NotNull(translator);
    }

    [Fact]
    public void TryGetMethodTranslator_NoMatch_ReturnsFalse()
    {
        MethodInfo method = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;
        SQLiteOptions options = BuildOptions();

        bool result = options.TryGetMethodTranslator(method, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetMethodTranslator_GenericMethodMatchesViaOpenDefinition_ReturnsTrue()
    {
        MethodInfo openMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.First) && m.GetParameters().Length == 1)
            .GetGenericMethodDefinition();

        MethodInfo closedMethod = openMethod.MakeGenericMethod(typeof(int));

        SQLiteOptions options = BuildOptions(b => b.MemberTranslators[openMethod] = Identity);

        bool result = options.TryGetMethodTranslator(closedMethod, out SQLiteMemberTranslator? translator);

        Assert.True(result);
        Assert.NotNull(translator);
    }

    [Fact]
    public void TryGetMethodTranslator_ConstructedGenericTypeMatchesViaOpenMethod_ReturnsTrue()
    {
        MethodInfo openMethod = typeof(GenericHolder<>).GetMethod(nameof(GenericHolder<int>.Take))!;
        MethodInfo closedMethod = typeof(GenericHolder<int>).GetMethod(nameof(GenericHolder<int>.Take))!;

        SQLiteOptions options = BuildOptions(b => b.MemberTranslators[openMethod] = Identity);

        bool result = options.TryGetMethodTranslator(closedMethod, out SQLiteMemberTranslator? translator);

        Assert.True(result);
        Assert.NotNull(translator);
    }

    [Fact]
    public void TryGetMethodTranslator_ConstructedGenericType_OpenMethodMissing_ReturnsFalse()
    {
        MethodInfo closedMethod = typeof(GenericHolder<int>).GetMethod(nameof(GenericHolder<int>.Take))!;
        SQLiteOptions options = BuildOptions();

        bool result = options.TryGetMethodTranslator(closedMethod, out _);

        Assert.False(result);
    }

    [Fact]
    public void TranslateProperty_NoTranslators_ReturnsNull()
    {
        SQLiteOptions options = BuildOptions();

        string? result = options.TranslateProperty("Foo", "obj");

        Assert.Null(result);
    }

    [Fact]
    public void TranslateProperty_TranslatorReturnsNull_FallsThroughToNext()
    {
        SQLiteOptions options = BuildOptions(b =>
        {
            b.PropertyTranslators.Add((_, _) => null);
            b.PropertyTranslators.Add((name, instance) => $"{instance}.handled_{name}");
        });

        string? result = options.TranslateProperty("Bar", "obj");

        Assert.Equal("obj.handled_Bar", result);
    }

    [Fact]
    public void TranslateProperty_FirstTranslatorMatches_ReturnsItsResult()
    {
        SQLiteOptions options = BuildOptions(b =>
        {
            b.PropertyTranslators.Add((name, instance) => $"first({instance}, {name})");
            b.PropertyTranslators.Add((_, _) => "should_not_run");
        });

        string? result = options.TranslateProperty("Baz", "obj");

        Assert.Equal("first(obj, Baz)", result);
    }

    private sealed class GenericHolder<T>
    {
        public T Take(T value) => value;
    }
}
