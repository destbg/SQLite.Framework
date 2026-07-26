using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class DatabaseFileLinkIdentityTests
{
    [Fact]
    public void APathReachedThroughALinkedFolderNamesTheSameFile()
    {
        string root = Path.Combine(Path.GetTempPath(), $"h22d_ident_{Guid.NewGuid():N}");
        string real = Path.Combine(root, "real");
        string link = Path.Combine(root, "link");

        Directory.CreateDirectory(real);

        try
        {
            string target = Path.Combine(real, "store.db3");
            File.WriteAllText(target, "same");
            Directory.CreateSymbolicLink(link, real);
            string linked = Path.Combine(link, "store.db3");

            Assert.Equal("same", File.ReadAllText(linked));
            Assert.True(DatabaseFilePath.IsSame(target, linked, false));
            Assert.True(DatabaseFilePath.IsSame(linked, target, false));
        }
        finally
        {
            Remove(root);
        }
    }

    private static void Remove(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
        catch (IOException)
        {
        }
    }
}
