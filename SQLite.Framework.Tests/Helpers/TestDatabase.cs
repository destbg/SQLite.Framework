using System.Runtime.CompilerServices;

namespace SQLite.Framework.Tests.Helpers;

public class TestDatabase : SQLiteDatabase
{
    public TestDatabase([CallerMemberName] string? methodName = null)
        : base($"{methodName}_{Guid.NewGuid():N}.db3")
    {
        File.Delete(DatabasePath);
    }

    public override void Dispose()
    {
        base.Dispose();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        for (int i = 0; i < 10; i++)
        {
            try
            {
                if (File.Exists(DatabasePath))
                {
                    File.Delete(DatabasePath);
                }
                break;
            }
            catch (IOException)
            {
                if (i == 9)
                {
                    return;
                }
                Thread.Sleep(100);
            }
        }
    }
}
