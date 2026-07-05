using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class GroupPageNumContext : JsonSerializerContext;

internal sealed class GroupPageNumRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Nums { get; set; } = [];
}

public class JsonGroupThenPageTests
{
    [Fact]
    public void GroupSumsTakenThenFilteredFollowKeyOrder()
    {
        List<int> local = [1, 2, 3];
        using TestDatabase db = new(b => b.AddJsonContext(GroupPageNumContext.Default));
        db.Table<GroupPageNumRow>().Schema.CreateTable();
        db.Table<GroupPageNumRow>().Add(new GroupPageNumRow { Id = 1, Nums = local });

        List<int> expected = local.GroupBy(x => x % 2).OrderBy(g => g.Key).Select(g => g.Sum()).Take(1).Where(s => s > 0).ToList();
        List<int> actual = db.Table<GroupPageNumRow>().Select(r => r.Nums.GroupBy(x => x % 2).Select(g => g.Sum()).Take(1).Where(s => s > 0).ToList()).First();

        Assert.Equal(expected, actual);
    }
}
