using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AsyncReaderCommandOutcomeTests
{
    [Fact]
    public async Task AnAsyncReaderQueryReportsASingleOutcomeWhenTheReaderCloses()
    {
        H26sCommandOutcomeInterceptor interceptor = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(interceptor));

        SQLiteCommand command = db.CreateCommand("SELECT 1 UNION ALL SELECT 2", []);
        SQLiteDataReader reader = await command.ExecuteReaderAsync();
        while (reader.Read())
        {
        }

        long id = interceptor.Executing[^1];

        Assert.Equal(0, interceptor.Executed.Count(x => x == id));

        reader.Dispose();

        Assert.Equal(1, interceptor.Executed.Count(x => x == id));
        Assert.Empty(interceptor.Failed);
    }
}
