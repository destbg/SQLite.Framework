namespace SQLite.Framework.Internals;

/// <summary>
/// Builds a migration instance for its CLR type. The dependency injection package wires one that
/// resolves the migration's constructor arguments from the service provider. When none is set, the
/// runner creates the migration through its public constructor with no arguments.
/// </summary>
/// <param name="migrationType">The migration type to construct.</param>
internal delegate object SQLiteMigrationActivator([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type migrationType);
