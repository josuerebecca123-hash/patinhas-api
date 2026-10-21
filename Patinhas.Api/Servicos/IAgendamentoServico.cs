using Patinhas.Api.Dtos;

namespace Patinhas.Api.Servicos;

public interface IAgendamentoServico
{
    Task<Resultado<AgendamentoDto>> CriarAsync(AgendamentoCriarDto dto, Solicitante solicitante);
    Task<Resultado<PaginaDto<AgendamentoDto>>> ListarAsync(int pagina, int tamanho, Solicitante solicitante);
    Task<Resultado<AgendamentoDto>> ObterAsync(int id, Solicitante solicitante);
    Task<Resultado<AgendamentoDto>> CancelarAsync(int id, Solicitante solicitante);
}
