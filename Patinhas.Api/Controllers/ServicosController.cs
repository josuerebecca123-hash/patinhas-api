using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Patinhas.Api.Dominio;
using Patinhas.Api.Dtos;
using Patinhas.Api.Servicos;

namespace Patinhas.Api.Controllers;

[ApiController]
[Route("api/servicos")]
[Produces("application/json")]
public class ServicosController : ControllerBase
{
    private readonly ICatalogoServicos _catalogo;

    public ServicosController(ICatalogoServicos catalogo) => _catalogo = catalogo;

    /// <summary>Lista pública e paginada dos serviços, com busca por nome ou descrição.</summary>
    /// <remarks>Com token de admin, a lista inclui também os serviços inativos.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaDto<ServicoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 10)
    {
        var resultado = await _catalogo.ListarAsync(busca, pagina, tamanho, User.IsInRole(Papeis.Admin));
        return resultado.Sucesso ? Ok(resultado.Valor) : this.ParaProblem(resultado);
    }

    /// <summary>Detalhe público de um serviço.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ServicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(int id)
    {
        var resultado = await _catalogo.ObterAsync(id, User.IsInRole(Papeis.Admin));
        return resultado.Sucesso ? Ok(resultado.Valor) : this.ParaProblem(resultado);
    }

    /// <summary>Cria um serviço (somente admin).</summary>
    [HttpPost]
    [Authorize(Roles = Papeis.Admin)]
    [ProducesResponseType(typeof(ServicoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Criar(ServicoCriarDto dto)
    {
        var resultado = await _catalogo.CriarAsync(dto);
        return resultado.Sucesso
            ? CreatedAtAction(nameof(Obter), new { id = resultado.Valor!.Id }, resultado.Valor)
            : this.ParaProblem(resultado);
    }

    /// <summary>Atualiza um serviço (somente admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Papeis.Admin)]
    [ProducesResponseType(typeof(ServicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, ServicoAtualizarDto dto)
    {
        var resultado = await _catalogo.AtualizarAsync(id, dto);
        return resultado.Sucesso ? Ok(resultado.Valor) : this.ParaProblem(resultado);
    }

    /// <summary>Exclui um serviço sem agendamentos (somente admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Papeis.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(int id)
    {
        var resultado = await _catalogo.ExcluirAsync(id);
        return resultado.Sucesso ? NoContent() : this.ParaProblem(resultado);
    }
}
