namespace SQLite.Framework.Internals.Enums;

/// <summary>
/// Discriminator for the forms an ON CONFLICT clause can take.
/// </summary>
internal enum UpsertActionKind
{
    DoNothing,
    DoUpdate,
    DoUpdateAll,
    DoUpdateSet,
}
