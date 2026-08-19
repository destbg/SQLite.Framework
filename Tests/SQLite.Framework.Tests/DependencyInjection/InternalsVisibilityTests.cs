using System.Reflection;
using System.Runtime.CompilerServices;

namespace SQLite.Framework.Tests;

public class InternalsVisibilityTests
{
    [Fact]
    public void FrameworkAssemblyKeepsTheSharedAssemblyName()
    {
        Assert.Equal("SQLite.Framework", typeof(SQLiteDatabase).Assembly.GetName().Name);
    }

    [Fact]
    public void FrameworkAssemblyGrantsInternalsToDependencyInjection()
    {
        string[] friends = typeof(SQLiteDatabase).Assembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(a => a.AssemblyName)
            .ToArray();

        Assert.Contains(friends, name => name == "SQLite.Framework.DependencyInjection");
    }

    [Fact]
    public void MigrationActivatorDelegateIsInternal()
    {
        Type? type = typeof(SQLiteDatabase).Assembly.GetType("SQLite.Framework.Internals.SQLiteMigrationActivator");

        Assert.NotNull(type);
        Assert.True(type is { IsPublic: false, IsNotPublic: true });
    }

    [Fact]
    public void MigrationActivatorIsAnInternalProperty()
    {
        PropertyInfo? property = typeof(SQLiteDatabase).GetProperty("MigrationActivator", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(property);
        Assert.NotNull(property.GetMethod);
        Assert.NotNull(property.SetMethod);
        Assert.True(property.GetMethod.IsAssembly);
        Assert.True(property.SetMethod.IsAssembly);
    }

    [Fact]
    public void UseMigrationActivatorIsAnInternalMethod()
    {
        MethodInfo? method = typeof(SQLiteMigrationRunner).GetMethod("UseMigrationActivator", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.True(method.IsAssembly);
    }
}
