using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Patinhas.Api.Dados;

namespace Patinhas.Testes;

// SQLite em memória: um banco novo para cada teste.
public sealed class BancoDeTestes : IDisposable
{
    private readonly SqliteConnection _conexao;

    public PatinhasDbContext Db { get; }

    public BancoDeTestes()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();

        var opcoes = new DbContextOptionsBuilder<PatinhasDbContext>()
            .UseSqlite(_conexao)
            .Options;

        Db = new PatinhasDbContext(opcoes);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _conexao.Dispose();
    }
}
