using Microsoft.EntityFrameworkCore;
using Patinhas.Api.Dados;
using Patinhas.Api.Dominio;
using Patinhas.Api.Dtos;

namespace Patinhas.Api.Servicos;

public class CatalogoServicos : ICatalogoServicos
{
    private const int TamanhoMaximo = 50;
    private const int PaginaMaxima = 100_000;

    private readonly PatinhasDbContext _db;
    private readonly ILogger<CatalogoServicos> _logger;

    public CatalogoServicos(PatinhasDbContext db, ILogger<CatalogoServicos> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Resultado<PaginaDto<ServicoDto>>> ListarAsync(
        string? busca, int pagina, int tamanho, bool incluirInativos)
    {
        // Parâmetros fora do limite são ajustados em vez de dar erro: a listagem é pública.
        pagina = Math.Clamp(pagina, 1, PaginaMaxima);
        tamanho = Math.Clamp(tamanho, 1, TamanhoMaximo);

        IQueryable<Servico> consulta = _db.Servicos.AsNoTracking();

        // O público só vê serviços ativos; o admin (com token) vê todos.
        if (!incluirInativos)
            consulta = consulta.Where(s => s.Ativo);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            // ToLower().Contains() roda no PostgreSQL e no SQLite dos testes (ILike só no Postgres).
            var termo = busca.Trim().ToLowerInvariant();
            consulta = consulta.Where(s => s.Nome.ToLower().Contains(termo)
                                        || s.Descricao.ToLower().Contains(termo));
        }

        var total = await consulta.CountAsync();

        var itens = await consulta
            .OrderBy(s => s.Nome).ThenBy(s => s.Id)   // ordem estável: a paginação não repete nem pula itens
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(s => new ServicoDto(s.Id, s.Nome, s.Descricao, s.Preco, s.DuracaoMinutos, s.Ativo))
            .ToListAsync();

        var totalPaginas = (int)Math.Ceiling(total / (double)tamanho);

        // Termo sem resultado não é erro: 200 com lista vazia e total 0.
        return Resultado<PaginaDto<ServicoDto>>.Ok(new PaginaDto<ServicoDto>(itens, pagina, tamanho, total, totalPaginas));
    }

    public async Task<Resultado<ServicoDto>> ObterAsync(int id, bool incluirInativos)
    {
        var servico = await _db.Servicos.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

        // Serviço inativo é "não encontrado" para o público.
        if (servico is null || (!servico.Ativo && !incluirInativos))
            return Resultado<ServicoDto>.Falha(TipoErro.NaoEncontrado, "Serviço não encontrado.");

        return Resultado<ServicoDto>.Ok(ServicoDto.De(servico));
    }

    public async Task<Resultado<ServicoDto>> CriarAsync(ServicoCriarDto dto)
    {
        Servico servico;
        try
        {
            servico = new Servico(dto.Nome, dto.Descricao ?? string.Empty, dto.Preco, dto.DuracaoMinutos);
        }
        catch (ArgumentException ex)
        {
            return Resultado<ServicoDto>.Falha(TipoErro.Validacao, ex.Message);
        }

        _db.Servicos.Add(servico);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Serviço {ServicoId} ({Nome}) criado", servico.Id, servico.Nome);
        return Resultado<ServicoDto>.Ok(ServicoDto.De(servico));
    }

    public async Task<Resultado<ServicoDto>> AtualizarAsync(int id, ServicoAtualizarDto dto)
    {
        var servico = await _db.Servicos.FirstOrDefaultAsync(s => s.Id == id);
        if (servico is null)
            return Resultado<ServicoDto>.Falha(TipoErro.NaoEncontrado, "Serviço não encontrado.");

        try
        {
            servico.Atualizar(dto.Nome, dto.Descricao ?? string.Empty, dto.Preco, dto.DuracaoMinutos);
        }
        catch (ArgumentException ex)
        {
            return Resultado<ServicoDto>.Falha(TipoErro.Validacao, ex.Message);
        }

        servico.DefinirAtivo(dto.Ativo);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Serviço {ServicoId} atualizado", servico.Id);
        return Resultado<ServicoDto>.Ok(ServicoDto.De(servico));
    }

    public async Task<Resultado> ExcluirAsync(int id)
    {
        var servico = await _db.Servicos.FirstOrDefaultAsync(s => s.Id == id);
        if (servico is null)
            return Resultado.Falha(TipoErro.NaoEncontrado, "Serviço não encontrado.");

        const string mensagemConflito =
            "Este serviço já tem agendamentos e não pode ser excluído. Desative-o em vez de excluir.";

        if (await _db.Agendamentos.AnyAsync(a => a.ServicoId == id))
        {
            _logger.LogWarning("Exclusão do serviço {ServicoId} recusada: há agendamentos ligados", id);
            return Resultado.Falha(TipoErro.Conflito, mensagemConflito);
        }

        _db.Servicos.Remove(servico);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A chave estrangeira (Restrict) é a segunda barreira, caso um agendamento surja no meio.
            return Resultado.Falha(TipoErro.Conflito, mensagemConflito);
        }

        _logger.LogInformation("Serviço {ServicoId} excluído", id);
        return Resultado.Ok();
    }
}
