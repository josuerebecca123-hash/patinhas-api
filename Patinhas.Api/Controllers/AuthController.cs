using Microsoft.AspNetCore.Mvc;
using Patinhas.Api.Dtos;
using Patinhas.Api.Servicos;

namespace Patinhas.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthServico _auth;

    public AuthController(IAuthServico auth) => _auth = auth;

    /// <summary>Cria uma conta de cliente.</summary>
    [HttpPost("registro")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(RegistroDto dto)
    {
        var resultado = await _auth.RegistrarAsync(dto);
        return resultado.Sucesso
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : this.ParaProblem(resultado);
    }

    /// <summary>Entra com e-mail e senha e devolve o token JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var resultado = await _auth.LoginAsync(dto);
        return resultado.Sucesso ? Ok(resultado.Valor) : this.ParaProblem(resultado);
    }
}
