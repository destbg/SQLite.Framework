using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ChainedProjectionNestedMemberParityTests
{
    public class ChainSource
    {
        [Key]
        public int Id { get; set; }
        public int V { get; set; }
        public string Name { get; set; } = "";
    }

    public class ChainDto
    {
        public string Label { get; set; } = "";
        public int Amount { get; set; }
    }

    public record ChainRecord(int Amount, string Label)
    {
        public string Extra { get; set; } = "";
    }

    public record ChainRecordWithNote(int Amount)
    {
        public string Note { get; set; } = "";
    }

    private static readonly ChainSource[] Seed =
    [
        new ChainSource { Id = 1, V = 10, Name = "n1" },
        new ChainSource { Id = 2, V = 20, Name = "n2" },
        new ChainSource { Id = 3, V = 30, Name = "n3" },
        new ChainSource { Id = 4, V = 40, Name = "n4" },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<ChainSource>().Schema.CreateTable();
        foreach (ChainSource r in Seed)
        {
            db.Table<ChainSource>().Add(r);
        }
        return db;
    }

    [Fact]
    public void NestedAnonymousMemberInSecondSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.OrderBy(x => x.Id).Select(x => new { x.Id, Inner = new { x.V } }).Select(a => a.Inner.V).ToList();
        List<int> actual = db.Table<ChainSource>().OrderBy(x => x.Id).Select(x => new { x.Id, Inner = new { x.V } }).Select(a => a.Inner.V).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedAnonymousMemberInWhere_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(x => new { x.Id, Inner = new { x.V } }).Where(a => a.Inner.V >= 20).Select(a => a.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<ChainSource>().Select(x => new { x.Id, Inner = new { x.V } }).Where(a => a.Inner.V >= 20).Select(a => a.Id).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedDtoMemberInSecondSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(x => new { x.Id, D = new ChainDto { Label = x.Name, Amount = x.V } }).Select(a => a.D.Amount).OrderBy(i => i).ToList();
        List<int> actual = db.Table<ChainSource>().Select(x => new { x.Id, D = new ChainDto { Label = x.Name, Amount = x.V } }).Select(a => a.D.Amount).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedRecordMemberInSecondSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(x => new { x.Id, R = new ChainRecord(x.V, x.Name) }).Select(a => a.R.Amount).OrderBy(i => i).ToList();
        List<int> actual = db.Table<ChainSource>().Select(x => new { x.Id, R = new ChainRecord(x.V, x.Name) }).Select(a => a.R.Amount).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedValueTupleMemberInSecondSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(x => new { x.Id, T = new ValueTuple<int, int>(x.V, x.Id) }).Select(a => a.T.Item1).OrderBy(i => i).ToList();
        List<int> actual = db.Table<ChainSource>().Select(x => new { x.Id, T = new ValueTuple<int, int>(x.V, x.Id) }).Select(a => a.T.Item1).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedValueTupleSecondMemberInSecondSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(x => new { x.Id, T = new ValueTuple<int, int>(x.V, x.Id) }).Select(a => a.T.Item2).OrderBy(i => i).ToList();
        List<int> actual = db.Table<ChainSource>().Select(x => new { x.Id, T = new ValueTuple<int, int>(x.V, x.Id) }).Select(a => a.T.Item2).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedRecordWithInitializerCtorMemberInSecondSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Select(x => new { x.Id, R = new ChainRecordWithNote(x.V) { Note = x.Name } }).Select(a => a.R.Amount).OrderBy(i => i).ToList();
        List<int> actual = db.Table<ChainSource>().Select(x => new { x.Id, R = new ChainRecordWithNote(x.V) { Note = x.Name } }).Select(a => a.R.Amount).OrderBy(i => i).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedRecordWithInitializerBindingMemberInSecondSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<string> oracle = Seed.Select(x => new { x.Id, R = new ChainRecordWithNote(x.V) { Note = x.Name } }).Select(a => a.R.Note).OrderBy(s => s).ToList();
        List<string> actual = db.Table<ChainSource>().Select(x => new { x.Id, R = new ChainRecordWithNote(x.V) { Note = x.Name } }).Select(a => a.R.Note).OrderBy(s => s).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedAnonymousMemberInJoinResultSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.Join(Seed, a => a.Id, b => b.Id, (a, b) => new { Pair = new { a.Id, b.V } }).Select(p => p.Pair.V).OrderBy(v => v).ToList();
        List<int> actual = db.Table<ChainSource>().Join(db.Table<ChainSource>(), a => a.Id, b => b.Id, (a, b) => new { Pair = new { a.Id, b.V } }).Select(p => p.Pair.V).OrderBy(v => v).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void NestedAnonymousMemberInGroupResultSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.GroupBy(x => x.V % 20).Select(g => new { K = g.Key, Inner = new { Sum = g.Sum(y => y.V) } }).OrderBy(a => a.K).Select(a => a.Inner.Sum).ToList();
        List<int> actual = db.Table<ChainSource>().GroupBy(x => x.V % 20).Select(g => new { K = g.Key, Inner = new { Sum = g.Sum(y => y.V) } }).OrderBy(a => a.K).Select(a => a.Inner.Sum).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DoublyNestedAnonymousMemberInSecondSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();

        List<int> oracle = Seed.OrderBy(x => x.Id).Select(x => new { x.Id, Mid = new { Inner = new { x.V } } }).Select(a => a.Mid.Inner.V).ToList();
        List<int> actual = db.Table<ChainSource>().OrderBy(x => x.Id).Select(x => new { x.Id, Mid = new { Inner = new { x.V } } }).Select(a => a.Mid.Inner.V).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ChainedSelectReadingConstructorBoundMember_IsNotSupported()
    {
        using TestDatabase db = Create();

        List<string> oracle = Seed.OrderBy(x => x.Id).Select(x => new ChainRecord(x.V, x.Name) { Extra = "e" }).Select(r => r.Label).ToList();
        Assert.Equal(["n1", "n2", "n3", "n4"], oracle);

        Assert.Throws<NotSupportedException>(() =>
            db.Table<ChainSource>().OrderBy(x => x.Id).Select(x => new ChainRecord(x.V, x.Name) { Extra = "e" }).Select(r => r.Label).ToList());
    }
}
