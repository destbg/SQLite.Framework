using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.Internals;

namespace SQLite.Framework.DependencyInjection;

/// <summary>
/// Builds the migration activator that resolves a migration class's constructor arguments from a
/// service provider.
/// </summary>
internal static class MigrationServiceActivator
{
    /// <summary>
    /// Returns an activator that builds a migration through <see cref="ActivatorUtilities" />, so
    /// the migration's constructor arguments come from <paramref name="services" />.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2111", Justification = "Add<T> invokes the activator with a PublicConstructors-annotated type.")]
    public static SQLiteMigrationActivator For(IServiceProvider services)
    {
        return Create;

        object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            return ActivatorUtilities.CreateInstance(services, type);
        }
    }
}
