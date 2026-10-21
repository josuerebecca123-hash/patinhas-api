using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Patinhas.Api.Dados;
using Patinhas.Api.Dominio;
using Patinhas.Api.Dtos;

namespace Patinhas.Api.Servicos;

public class AgendamentoServico : IAgendamentoServico
{
    private const int TamanhoMaximo = 50;
    private const int PaginaMaxima = 100_000;

    private readonly PatinhasDbContext _db;
    private readonly TimeProvider _relogio;
    private readonly ILogger<AgendamentoServico> _logger;

    public AgendamentoServico(PatinhasDbContext db, TimeProvider relogio, ILogger<AgendamentoServico> logger)
    {
        _db = db;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task<Resultado<AgendamentoDto>> CriarAsync(AgendamentoCriarDto dto, Solicitante solicitante)
    {
        // DateTimeOffset -> UTC. É o que o Npgsql exige em "timestamp with time zone".
        var inicio = dto.DataHora.UtcDateTime;

        if (inicio <= _relogio.GetUtcNow().UtcDateTime)
            return Resultado<AgendamentoDto>.Falha(TipoErro.Validacao, "A data e hora do agendamento devem estar no futuro.");

        var servico = await _db.Servicos.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dto.ServicoId && s.Ativo);
        if (servico is null)
            return Resultado<AgendamentoDto>.Falha(TipoErro.Validacao, "Serviço inexistente ou inativo.");

        var duracao = servico.DuracaoMinutos;

        // A regra do 409 roda dentro de uma transação SERIALIZABLE: se dois pedidos tentarem o mesmo
        // horário ao mesmo tempo, o banco derruba um deles em vez de deixar os dois passarem.
        await using var transacao = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Como os dois agendamentos são do mesmo serviço (mesma duração), só podem se sobrepor
            // os que começam entre (início - duração) e (início + duração).
            var janelaInicio = inicio.AddMinutes(-duracao);
            var janelaFim = inicio.AddMinutes(duracao);

            var horariosProximos = await _db.Agendamentos.AsNoTracking()
                .Where(a => a.ServicoId == servico.Id
                         && a.Status == StatusAgendamento.Marcado   // cancelado não bloqueia horário
                         && a.DataHora > janelaInicio
                         && a.DataHora < janelaFim)
                .Select(a => a.DataHora)
                .ToListAsync();

            // A consulta só reduz os candidatos; quem decide é a regra do domínio.
            if (horariosProximos.Any(h => Agendamento.HaSobreposicao(h, duracao, inicio, duracao)))
            {
                _logger.LogWarning(
                    "Conflito de horário: o serviço {ServicoId} já está ocupado em {DataHora}", servico.Id, inicio);
                return Resultado<AgendamentoDto>.Falha(TipoErro.Conflito,
                    "Já existe um agendamento deste serviço que se sobrepõe a este horário. Escolha outro horário.");
            }

            // O dono vem do solicitante (claims), nunca do corpo da requisição.
            var agendamento = new Agendamento(servico.Id, solicitante.UsuarioId, dto.NomeDoPet, inicio);
            _db.Agendamentos.Add(agendamento);
            await _db.SaveChangesAsync();
            await transacao.CommitAsync();

            _logger.LogInformation(
                "Agendamento {AgendamentoId} criado para o serviço {ServicoId} pelo usuário {UsuarioId}",
                agendamento.Id, servico.Id, solicitante.UsuarioId);
            return Resultado<AgendamentoDto>.Ok(AgendamentoDto.De(agendamento, servico.Nome, duracao));
        }
        catch (ArgumentException ex)
        {
            return Resultado<AgendamentoDto>.Falha(TipoErro.Validacao, ex.Message);
        }
        catch (Exception ex) when (FalhaDeConcorrencia(ex))
        {
            _logger.LogWarning(ex, "Conflito de concorrência ao agendar o serviço {ServicoId}", servico.Id);
            return Resultado<AgendamentoDto>.Falha(TipoErro.Conflito,
                "Outro agendamento foi feito para este horário ao mesmo tempo. Tente novamente.");
        }
    }

    public async Task<Resultado<PaginaDto<AgendamentoDto>>> ListarAsync(int pagina, int tamanho, Solicitante solicitante)
    {
        pagina = Math.Clamp(pagina, 1, PaginaMaxima);
        tamanho = Math.Clamp(tamanho, 1, TamanhoMaximo);

        IQueryable<Agendamento> consulta = _db.Agendamentos.AsNoTracking().Include(a => a.Servico);

        // Cliente só enxerga os próprios agendamentos; admin enxerga todos.
        if (!solicitante.EhAdmin)
            consulta = consulta.Where(a => a.UsuarioId == solicitante.UsuarioId);

        var total = await consulta.CountAsync();

        var agendamentos = await consulta
            .OrderBy(a => a.DataHora).ThenBy(a => a.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync();

        var itens = agendamentos
            .Select(a => AgendamentoDto.De(a, a.Servico?.Nome ?? string.Empty, a.Servico?.DuracaoMinutos ?? 0))
            .ToList();

        var totalPaginas = (int)Math.Ceiling(total / (double)tamanho);
        return Resultado<PaginaDto<AgendamentoDto>>.Ok(new PaginaDto<AgendamentoDto>(itens, pagina, tamanho, total, totalPaginas));
    }

    public async Task<Resultado<AgendamentoDto>> ObterAsync(int id, Solicitante solicitante)
    {
        var agendamento = await _db.Agendamentos.AsNoTracking()
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamento is null)
            return Resultado<AgendamentoDto>.Falha(TipoErro.NaoEncontrado, "Agendamento não encontrado.");

        if (!PodeAcessar(agendamento, solicitante))
            return NegarAcesso(agendamento.Id, solicitante);

        return Resultado<AgendamentoDto>.Ok(ParaDto(agendamento));
    }

    public async Task<Resultado<AgendamentoDto>> CancelarAsync(int id, Solicitante solicitante)
    {
        var agendamento = await _db.Agendamentos
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamento is null)
            return Resultado<AgendamentoDto>.Falha(TipoErro.NaoEncontrado, "Agendamento não encontrado.");

        // Só o dono ou o admin. O id do dono vem do token; o id do agendamento vem da URL.
        if (!PodeAcessar(agendamento, solicitante))
            return NegarAcesso(agendamento.Id, solicitante);

        if (agendamento.Status == StatusAgendamento.Cancelado)
            return Resultado<AgendamentoDto>.Falha(TipoErro.Conflito, "Este agendamento já está cancelado.");

        agendamento.Cancelar();
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Agendamento {AgendamentoId} cancelado pelo usuário {UsuarioId}", agendamento.Id, solicitante.UsuarioId);
        return Resultado<AgendamentoDto>.Ok(ParaDto(agendamento));
    }

    private static bool PodeAcessar(Agendamento agendamento, Solicitante solicitante) =>
        solicitante.EhAdmin || agendamento.UsuarioId == solicitante.UsuarioId;

    private Resultado<AgendamentoDto> NegarAcesso(int agendamentoId, Solicitante solicitante)
    {
        _logger.LogWarning(
            "Acesso negado: o usuário {UsuarioId} tentou acessar o agendamento {AgendamentoId} de outra pessoa",
            solicitante.UsuarioId, agendamentoId);
        return Resultado<AgendamentoDto>.Falha(TipoErro.Proibido, "Você não tem permissão para acessar este agendamento.");
    }

    private static AgendamentoDto ParaDto(Agendamento a) =>
        AgendamentoDto.De(a, a.Servico?.Nome ?? string.Empty, a.Servico?.DuracaoMinutos ?? 0);

    // 40001 = serialization_failure do PostgreSQL (duas transações disputando o mesmo horário).
    private static bool FalhaDeConcorrencia(Exception ex) =>
        ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure }
        || ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure };
}
