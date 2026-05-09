using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;

namespace SQLite.Framework.Tests;

public class AsyncQueryableThrowTests
{
    private static IQueryable<Book> NonSqlite() => new[]
    {
        new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
        new Book { Id = 2, Title = "B", AuthorId = 2, Price = 20 },
    }.AsQueryable();

    private static IQueryable<int> NonSqliteInts() => new[] { 1, 2, 3 }.AsQueryable();
    private static IQueryable<int?> NonSqliteIntNulls() => new int?[] { 1, 2, 3 }.AsQueryable();
    private static IQueryable<long> NonSqliteLongs() => new long[] { 1, 2, 3 }.AsQueryable();
    private static IQueryable<long?> NonSqliteLongNulls() => new long?[] { 1, 2, 3 }.AsQueryable();
    private static IQueryable<float> NonSqliteFloats() => new float[] { 1, 2, 3 }.AsQueryable();
    private static IQueryable<float?> NonSqliteFloatNulls() => new float?[] { 1, 2, 3 }.AsQueryable();
    private static IQueryable<double> NonSqliteDoubles() => new double[] { 1, 2, 3 }.AsQueryable();
    private static IQueryable<double?> NonSqliteDoubleNulls() => new double?[] { 1, 2, 3 }.AsQueryable();
    private static IQueryable<decimal> NonSqliteDecimals() => new decimal[] { 1, 2, 3 }.AsQueryable();
    private static IQueryable<decimal?> NonSqliteDecimalNulls() => new decimal?[] { 1, 2, 3 }.AsQueryable();

    [Fact]
    public async Task ExecuteDeleteAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ExecuteDeleteAsync());
    }

    [Fact]
    public async Task ExecuteDeleteAsync_Predicate_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ExecuteDeleteAsync(b => b.Id > 0));
    }

    [Fact]
    public async Task ExecuteUpdateAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ExecuteUpdateAsync(s => s.Set(b => b.Price, 99)));
    }

    [Fact]
    public async Task ToListAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ToListAsync());
    }

    [Fact]
    public async Task ToArrayAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ToArrayAsync());
    }

    [Fact]
    public async Task ToHashSetAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ToHashSetAsync());
    }

    [Fact]
    public async Task ToDictionaryAsync_KeyOnly_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ToDictionaryAsync(b => b.Id));
    }

    [Fact]
    public async Task ToDictionaryAsync_KeyAndValue_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ToDictionaryAsync(b => b.Id, b => b.Title));
    }

    [Fact]
    public async Task ToLookupAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await ((IEnumerable<Book>)NonSqlite()).ToLookupAsync(b => b.AuthorId, b => b.Title));
    }

    [Fact]
    public async Task FirstAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().FirstAsync());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().FirstOrDefaultAsync());
    }

    [Fact]
    public async Task FirstOrDefaultAsync_DefaultValue_NotSQLite_Throws()
    {
        Book def = new() { Id = 0, Title = "x", AuthorId = 0, Price = 0 };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().FirstOrDefaultAsync(def));
    }

    [Fact]
    public async Task SingleAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SingleAsync());
    }

    [Fact]
    public async Task SingleOrDefaultAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SingleOrDefaultAsync());
    }

    [Fact]
    public async Task SingleOrDefaultAsync_DefaultValue_NotSQLite_Throws()
    {
        Book def = new() { Id = 0, Title = "x", AuthorId = 0, Price = 0 };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SingleOrDefaultAsync(def));
    }

    [Fact]
    public async Task ContainsAsync_NotSQLite_Throws()
    {
        Book item = new() { Id = 1, Title = "A", AuthorId = 1, Price = 10 };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ContainsAsync(item));
    }

    [Fact]
    public async Task AnyAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AnyAsync());
    }

    [Fact]
    public async Task AllAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AllAsync(b => b.Id > 0));
    }

    [Fact]
    public async Task CountAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().CountAsync());
    }

    [Fact]
    public async Task LongCountAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().LongCountAsync());
    }

    [Fact]
    public async Task MinAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().MinAsync());
    }

    [Fact]
    public async Task MinAsync_Selector_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().MinAsync(b => b.Price));
    }

    [Fact]
    public async Task MaxAsync_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().MaxAsync());
    }

    [Fact]
    public async Task MaxAsync_Selector_NotSQLite_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().MaxAsync(b => b.Price));
    }

    [Fact]
    public async Task SumAsync_Int_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteInts().SumAsync());
    [Fact]
    public async Task SumAsync_IntNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteIntNulls().SumAsync());
    [Fact]
    public async Task SumAsync_Long_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteLongs().SumAsync());
    [Fact]
    public async Task SumAsync_LongNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteLongNulls().SumAsync());
    [Fact]
    public async Task SumAsync_Float_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteFloats().SumAsync());
    [Fact]
    public async Task SumAsync_FloatNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteFloatNulls().SumAsync());
    [Fact]
    public async Task SumAsync_Double_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteDoubles().SumAsync());
    [Fact]
    public async Task SumAsync_DoubleNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteDoubleNulls().SumAsync());
    [Fact]
    public async Task SumAsync_Decimal_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteDecimals().SumAsync());
    [Fact]
    public async Task SumAsync_DecimalNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteDecimalNulls().SumAsync());

    [Fact]
    public async Task SumAsync_IntSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => b.Id));
    [Fact]
    public async Task SumAsync_IntNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => (int?)b.Id));
    [Fact]
    public async Task SumAsync_LongSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => (long)b.Id));
    [Fact]
    public async Task SumAsync_LongNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => (long?)b.Id));
    [Fact]
    public async Task SumAsync_FloatSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => (float)b.Price));
    [Fact]
    public async Task SumAsync_FloatNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => (float?)b.Price));
    [Fact]
    public async Task SumAsync_DoubleSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => b.Price));
    [Fact]
    public async Task SumAsync_DoubleNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => (double?)b.Price));
    [Fact]
    public async Task SumAsync_DecimalSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => (decimal)b.Price));
    [Fact]
    public async Task SumAsync_DecimalNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SumAsync(b => (decimal?)b.Price));

    [Fact]
    public async Task AverageAsync_Int_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteInts().AverageAsync());
    [Fact]
    public async Task AverageAsync_IntNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteIntNulls().AverageAsync());
    [Fact]
    public async Task AverageAsync_Long_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteLongs().AverageAsync());
    [Fact]
    public async Task AverageAsync_LongNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteLongNulls().AverageAsync());
    [Fact]
    public async Task AverageAsync_Float_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteFloats().AverageAsync());
    [Fact]
    public async Task AverageAsync_FloatNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteFloatNulls().AverageAsync());
    [Fact]
    public async Task AverageAsync_Double_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteDoubles().AverageAsync());
    [Fact]
    public async Task AverageAsync_DoubleNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteDoubleNulls().AverageAsync());
    [Fact]
    public async Task AverageAsync_Decimal_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteDecimals().AverageAsync());
    [Fact]
    public async Task AverageAsync_DecimalNullable_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqliteDecimalNulls().AverageAsync());

    [Fact]
    public async Task AverageAsync_IntSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => b.Id));
    [Fact]
    public async Task AverageAsync_IntNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => (int?)b.Id));
    [Fact]
    public async Task AverageAsync_FloatSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => (float)b.Price));
    [Fact]
    public async Task AverageAsync_FloatNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => (float?)b.Price));
    [Fact]
    public async Task AverageAsync_LongSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => (long)b.Id));
    [Fact]
    public async Task AverageAsync_LongNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => (long?)b.Id));
    [Fact]
    public async Task AverageAsync_DoubleSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => b.Price));
    [Fact]
    public async Task AverageAsync_DoubleNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => (double?)b.Price));
    [Fact]
    public async Task AverageAsync_DecimalSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => (decimal)b.Price));
    [Fact]
    public async Task AverageAsync_DecimalNullSelector_NotSQLite_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AverageAsync(b => (decimal?)b.Price));

    [Fact]
    public async Task FirstOrDefaultAsync_PredicateAndDefaultValue_NotSQLite_Throws()
    {
        Book def = new() { Id = 0, Title = "x", AuthorId = 0, Price = 0 };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().FirstOrDefaultAsync(b => b.Id > 0, def));
    }

    [Fact]
    public async Task SingleOrDefaultAsync_PredicateAndDefaultValue_NotSQLite_Throws()
    {
        Book def = new() { Id = 0, Title = "x", AuthorId = 0, Price = 0 };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SingleOrDefaultAsync(b => b.Id > 0, def));
    }

    [Fact]
    public async Task LongCountAsync_Predicate_NotSQLite_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().LongCountAsync(b => b.Id > 0));

    [Fact]
    public async Task ElementAtAsync_NotSQLite_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ElementAtAsync(0));

    [Fact]
    public async Task ElementAtOrDefaultAsync_NotSQLite_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().ElementAtOrDefaultAsync(0));

    [Fact]
    public async Task FirstAsync_Predicate_NotSQLite_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().FirstAsync(b => b.Id > 0));

    [Fact]
    public async Task FirstOrDefaultAsync_Predicate_NotSQLite_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().FirstOrDefaultAsync(b => b.Id > 0));

    [Fact]
    public async Task SingleAsync_Predicate_NotSQLite_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SingleAsync(b => b.Id > 0));

    [Fact]
    public async Task SingleOrDefaultAsync_Predicate_NotSQLite_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().SingleOrDefaultAsync(b => b.Id > 0));

    [Fact]
    public async Task AnyAsync_Predicate_NotSQLite_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().AnyAsync(b => b.Id > 0));

    [Fact]
    public async Task CountAsync_Predicate_NotSQLite_Throws()
        => await Assert.ThrowsAsync<InvalidOperationException>(async () => await NonSqlite().CountAsync(b => b.Id > 0));

#if !SQLITE_FRAMEWORK_REFLECTION_AOT_INCOMPATIBLE
    [Fact]
    public void ObsoleteMarker_AllErrorMarkers_Throw()
    {
        System.Reflection.MethodInfo[] methods = typeof(AsyncQueryableExtensions)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m =>
            {
                ObsoleteAttribute? obs = (ObsoleteAttribute?)Attribute.GetCustomAttribute(m, typeof(ObsoleteAttribute));
                return obs is { IsError: true };
            })
            .ToArray();

        Assert.NotEmpty(methods);

        foreach (System.Reflection.MethodInfo method in methods)
        {
            System.Reflection.MethodInfo concrete = MakeConcrete(method);
            object?[] args = BuildArgs(concrete);

            System.Reflection.TargetInvocationException ex = Assert.Throws<System.Reflection.TargetInvocationException>(
                () => concrete.Invoke(null, args));
            Assert.IsType<NotSupportedException>(ex.InnerException);
        }
    }
#endif

    private static System.Reflection.MethodInfo MakeConcrete(System.Reflection.MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
        {
            return method;
        }

        Type[] genericArgs = method.GetGenericArguments();
        Type[] concreteArgs = new Type[genericArgs.Length];
        for (int i = 0; i < genericArgs.Length; i++)
        {
            concreteArgs[i] = typeof(int);
        }

        return method.MakeGenericMethod(concreteArgs);
    }

    private static object?[] BuildArgs(System.Reflection.MethodInfo method)
    {
        System.Reflection.ParameterInfo[] parameters = method.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            Type pt = parameters[i].ParameterType;
            args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
        }

        return args;
    }
}
