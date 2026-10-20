using Microsoft.AspNetCore.Mvc;
using Patinhas.Api.Servicos;

namespace Patinhas.Api.Controllers;

public static class ResultadoExtensoes
{
    // Traduz o erro de negócio do serviço em ProblemDetails com o status certo.
    public static IActionResult ParaProblem(this ControllerBase controller, Resultado resultado)
    {
        var (status, titulo) = resultado.Erro switch
        {
            TipoErro.Validacao => (StatusCodes.Status400BadRequest, "Dados inválidos"),
            TipoErro.NaoAutenticado => (StatusCodes.Status401Unauthorized, "Não autenticado"),
            TipoErro.Proibido => (StatusCodes.Status403Forbidden, "Acesso negado"),
            TipoErro.NaoEncontrado => (StatusCodes.Status404NotFound, "Não encontrado"),
            TipoErro.Conflito => (StatusCodes.Status409Conflict, "Conflito"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        return controller.Problem(detail: resultado.Mensagem, statusCode: status, title: titulo);
    }
}
