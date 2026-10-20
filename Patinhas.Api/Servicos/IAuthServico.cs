using Patinhas.Api.Dtos;

namespace Patinhas.Api.Servicos;

public interface IAuthServico
{
    Task<Resultado<UsuarioDto>> RegistrarAsync(RegistroDto dto);
    Task<Resultado<TokenDto>> LoginAsync(LoginDto dto);
}
