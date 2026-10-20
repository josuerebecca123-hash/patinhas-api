using Patinhas.Api.Dtos;

namespace Patinhas.Api.Servicos;

public interface ICatalogoServicos
{
    Task<Resultado<PaginaDto<ServicoDto>>> ListarAsync(string? busca, int pagina, int tamanho, bool incluirInativos);
    Task<Resultado<ServicoDto>> ObterAsync(int id, bool incluirInativos);
    Task<Resultado<ServicoDto>> CriarAsync(ServicoCriarDto dto);
    Task<Resultado<ServicoDto>> AtualizarAsync(int id, ServicoAtualizarDto dto);
    Task<Resultado> ExcluirAsync(int id);
}
