using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Patinhas.Api.Dominio;
using Patinhas.Api.Dtos;
using Patinhas.Api.Servicos;

namespace Patinhas.Api.Controllers;

[ApiController]
[Route("api/agendamentos")]
[Produces("application/json")]
[Authorize]
public class AgendamentosController : ControllerBase
{
    private readonly IAgendamentoServico _agendamentos;

    public AgendamentosController(IAgendamentoServico agendamentos) => _agendamentos = agendamentos;

    // A identidade vem SEMPRE dos claims do token.
    private Solicitante? ObterSolicitante()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var usuarioId)
            ? new Solicitante(usuarioId, User.IsInRole(Papeis.Admin))
            : null;
    }

    private IActionResult TokenSemIdentidade() =>
        this.ParaProblem(Resultado.Falha(TipoErro.NaoAutenticado, "O token não traz uma identidade válida."));

    /// <summary>Lista os agendamentos: o cliente vê só os seus; o admin vê todos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaDto<AgendamentoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanho = 10)
    {
        var solicitante = ObterSolicitante();
        if (solicitante is null) return TokenSemIdentidade();

        var resultado = await _agendamentos.ListarAsync(pagina, tamanho, solicitante);
        return resultado.Sucesso ? Ok(resultado.Valor) : this.ParaProblem(resultado);
    }

    /// <summary>Detalhe de um agendamento (somente o dono ou o admin).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AgendamentoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(int id)
    {
        var solicitante = ObterSolicitante();
        if (solicitante is null) return TokenSemIdentidade();

        var resultado = await _agendamentos.ObterAsync(id, solicitante);
        return resultado.Sucesso ? Ok(resultado.Valor) : this.ParaProblem(resultado);
    }

    /// <summary>Cria um agendamento para o usuário logado.</summary>
    /// <remarks>Devolve 409 se o horário se sobrepõe a outro agendamento do mesmo serviço.</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(AgendamentoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(AgendamentoCriarDto dto)
    {
        var solicitante = ObterSolicitante();
        if (solicitante is null) return TokenSemIdentidade();

        var resultado = await _agendamentos.CriarAsync(dto, solicitante);
        return resultado.Sucesso
            ? CreatedAtAction(nameof(Obter), new { id = resultado.Valor!.Id }, resultado.Valor)
            : this.ParaProblem(resultado);
    }

    /// <summary>Cancela um agendamento (somente o dono ou o admin) e libera o horário.</summary>
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(typeof(AgendamentoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(int id)
    {
        var solicitante = ObterSolicitante();
        if (solicitante is null) return TokenSemIdentidade();

        var resultado = await _agendamentos.CancelarAsync(id, solicitante);
        return resultado.Sucesso ? Ok(resultado.Valor) : this.ParaProblem(resultado);
    }
}
