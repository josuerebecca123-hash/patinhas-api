using System.ComponentModel.DataAnnotations;
using Patinhas.Api.Dominio;

namespace Patinhas.Api.Dtos;

public class AgendamentoCriarDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Informe o serviço.")]
    public int ServicoId { get; set; }

    [Required(ErrorMessage = "O nome do pet é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O nome do pet deve ter no máximo 100 caracteres.")]
    public string NomeDoPet { get; set; } = string.Empty;

    // Recebe DateTimeOffset (com fuso); a entidade guarda sempre em UTC.
    public DateTimeOffset DataHora { get; set; }
}

public record AgendamentoDto(
    int Id,
    int ServicoId,
    string ServicoNome,
    string NomeDoPet,
    DateTimeOffset DataHora,
    DateTimeOffset DataHoraFim,
    string Status,
    int UsuarioId)
{
    public static AgendamentoDto De(Agendamento a, string servicoNome, int duracaoMinutos)
    {
        // Alguns bancos devolvem DateTime sem o Kind: marcamos como UTC para não depender do fuso do servidor.
        var inicio = new DateTimeOffset(DateTime.SpecifyKind(a.DataHora, DateTimeKind.Utc));
        return new AgendamentoDto(
            a.Id, a.ServicoId, servicoNome, a.NomeDoPet,
            inicio, inicio.AddMinutes(duracaoMinutos),
            a.Status.ToString(), a.UsuarioId);
    }
}
