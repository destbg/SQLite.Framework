using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
using SQLite.Framework.Generated;
#endif

namespace SQLite.Framework.Tests;

public class NcfWorkerConnection
{
    public NcfWorkerConnection(IDisposable socket, string machineName, IEnumerable<string> allowedProjects, string version, string? remoteIp = null)
    {
        Socket = socket;
        MachineName = machineName;
        AllowedProjects = new HashSet<string>(allowedProjects, StringComparer.OrdinalIgnoreCase);
        Version = version;
        RemoteIp = remoteIp;
    }

    public IDisposable Socket { get; }
    public string MachineName { get; }
    public IReadOnlySet<string> AllowedProjects { get; private set; }
    public string Version { get; }
    public string? RemoteIp { get; }
    public string? CurrentSessionId { get; set; }
    public int Priority { get; set; }
}

[Table("ncf_parent")]
public class NcfParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("ncf_note")]
public class NcfNote
{
    private NcfNote()
    {
    }

    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string? Text { get; set; }

    public static NcfNote Create(int id, int parentId, string? text)
    {
        return new NcfNote { Id = id, ParentId = parentId, Text = text };
    }
}

public class NcfNoteWrap
{
    public NcfNoteWrap(string tag, NcfNote? entity)
    {
        Tag = tag;
        Entity = entity;
    }

    public string Tag { get; }

    public NcfNote? Entity { get; }
}

public class NestedEntityConstructorFallbackTests
{
    private static List<NcfParent> Parents()
    {
        return
        [
            new NcfParent { Id = 1, Name = "Ann" },
            new NcfParent { Id = 2, Name = "Bob" },
            new NcfParent { Id = 3, Name = "Cid" },
        ];
    }

    private static List<NcfNote> Notes()
    {
        return
        [
            NcfNote.Create(10, 1, "t1"),
            NcfNote.Create(11, 3, null),
        ];
    }

    [Fact]
    public void TupleProjectionWithEntityLackingUsableConstructor()
    {
        List<NcfWorkerConnection> connections =
        [
            new NcfWorkerConnection(null!, "m1", ["projA"], "1.0") { CurrentSessionId = "s1", Priority = 3 },
            new NcfWorkerConnection(null!, "m2", ["projB"], "1.1", "10.0.0.1") { Priority = 1 },
        ];

        List<(string Machine, NcfWorkerConnection Conn)> projected = connections
            .Select(c => (c.MachineName, c))
            .ToList();

        Assert.Equal(2, projected.Count);
        Assert.Equal("m1", projected[0].Machine);
        Assert.Same(connections[0], projected[0].Conn);
        Assert.Equal("m2", projected[1].Machine);
        Assert.Same(connections[1], projected[1].Conn);
    }

    [Fact]
    public void PrivateConstructorEntityMaterializesThroughConstructorArgument()
    {
        using TestDatabase db = new();
        db.Table<NcfParent>().Schema.CreateTable();
        db.Table<NcfParent>().AddRange(Parents());
        db.Table<NcfNote>().Schema.CreateTable();
        db.Table<NcfNote>().AddRange(Notes());

        List<NcfParent> ps = Parents();
        List<NcfNote> ns = Notes();

        List<string> expected = (from p in ps
                                 join n in ns on p.Id equals n.ParentId into g
                                 from n in g.DefaultIfEmpty()
                                 select new NcfNoteWrap(p.Name, n))
            .OrderBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Tag + "|" + (x.Entity == null ? "nullN" : "N" + x.Entity.Id + ":" + (x.Entity.Text ?? "-")))
            .ToList();

        List<string> actual = (from p in db.Table<NcfParent>()
                               join n in db.Table<NcfNote>() on p.Id equals n.ParentId into g
                               from n in g.DefaultIfEmpty()
                               select new NcfNoteWrap(p.Name, n))
            .AsEnumerable()
            .OrderBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Tag + "|" + (x.Entity == null ? "nullN" : "N" + x.Entity.Id + ":" + (x.Entity.Text ?? "-")))
            .ToList();

        Assert.Equal(expected, actual);
    }

#if SQLITE_FRAMEWORK_SOURCE_GENERATOR && SQLITE_FRAMEWORK_TESTING
    [Fact]
    public void GeneratedMaterializer_RegisteredForTupleWithEntityLackingUsableConstructor()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:")
            .UseGeneratedMaterializers()
            .Build();

        Assert.Contains(typeof((string, NcfWorkerConnection)), options.EntityMaterializers.Keys);
        Assert.Contains(typeof(NcfNoteWrap), options.EntityMaterializers.Keys);
    }
#endif
}
