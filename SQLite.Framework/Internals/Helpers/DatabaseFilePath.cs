namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Compares the paths of two databases.
/// </summary>
internal static class DatabaseFilePath
{
    /// <summary>
    /// Tells whether two database paths name the same file. Symbolic links are resolved first, so a
    /// link and its target count as one file. Paths only differ by case on a file system that
    /// ignores case, which the caller states through <paramref name="ignoreCase" />. An empty path
    /// and <c>:memory:</c> each name a private database, so they never match.
    /// </summary>
    public static bool IsSame(string sourcePath, string destinationPath, bool ignoreCase)
    {
        if (sourcePath.Length == 0 || destinationPath.Length == 0
            || sourcePath == ":memory:" || destinationPath == ":memory:")
        {
            return false;
        }

        return string.Equals(
            Resolve(sourcePath),
            Resolve(destinationPath),
            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static string Resolve(string path)
    {
        string full = Path.GetFullPath(path);
        if (!File.Exists(full))
        {
            return full;
        }

        return File.ResolveLinkTarget(full, returnFinalTarget: true)?.FullName ?? full;
    }
}
