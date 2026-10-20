using System.ComponentModel.DataAnnotations;
using Patinhas.Api.Dominio;

namespace Patinhas.Api.Dtos;

public class ServicoCriarDto
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Descricao { get; set; }

  [Range(0.01, 100000, ErrorMessage = "O preço deve ficar entre 0,01 e 100000.")]
    public decimal Preco { get; set; }

    [Range(5, 1440, ErrorMessage = "A duração deve ficar entre 5 e 1440 minutos.")]
    public int DuracaoMinutos { get; set; }
}

// No PUT, o admin também pode ativar ou desativar o serviço.
public class ServicoAtualizarDto : ServicoCriarDto
{
    public bool Ativo { get; set; } = true;
}

public record ServicoDto(int Id, string Nome, string Descricao, decimal Preco, int DuracaoMinutos, bool Ativo)
{
    public static ServicoDto De(Servico s) =>
        new(s.Id, s.Nome, s.Descricao, s.Preco, s.DuracaoMinutos, s.Ativo);
}

public record PaginaDto<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, int Total, int TotalPaginas);
