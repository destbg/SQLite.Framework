using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Tests;

public class SameDatabaseFileResolutionTests
{
    [Fact]
    public void AnEmptyDestinationPathNeverMatches()
    {
        Assert.False(DatabaseFilePath.IsSame("a.db", string.Empty, ignoreCase: false));
    }

    [Fact]
    public void AnEmptySourcePathNeverMatches()
    {
        Assert.False(DatabaseFilePath.IsSame(string.Empty, "a.db", ignoreCase: false));
    }

    [Fact]
    public void AnInMemorySourcePathNeverMatches()
    {
        Assert.False(DatabaseFilePath.IsSame(":memory:", "a.db", ignoreCase: false));
    }

    [Fact]
    public void AnInMemoryDestinationPathNeverMatches()
    {
        Assert.False(DatabaseFilePath.IsSame("a.db", ":memory:", ignoreCase: false));
    }

    [Fact]
    public void PathsDifferingOnlyByCaseMatchWhenTheFileSystemIgnoresCase()
    {
        Assert.True(DatabaseFilePath.IsSame("a.db", "A.db", ignoreCase: true));
    }

    [Fact]
    public void PathsDifferingOnlyByCaseDifferWhenTheFileSystemKeepsCase()
    {
        Assert.False(DatabaseFilePath.IsSame("a.db", "A.db", ignoreCase: false));
    }

    [Fact]
    public void AnExistingFileComparesByItsResolvedPath()
    {
        string directory = Path.Combine(Path.GetTempPath(), "sqlitefw_same_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string real = Path.Combine(directory, "real.db");
            File.WriteAllText(real, "x");

            Assert.True(DatabaseFilePath.IsSame(real, real, ignoreCase: false));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ASymbolicLinkAndItsTargetAreTheSameFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "sqlitefw_link_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string real = Path.Combine(directory, "real.db");
            string link = Path.Combine(directory, "link.db");
            File.WriteAllText(real, "x");
            File.CreateSymbolicLink(link, real);

            Assert.True(DatabaseFilePath.IsSame(link, real, ignoreCase: false));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
