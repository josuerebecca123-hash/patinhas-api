using System.ComponentModel.DataAnnotations;

namespace Patinhas.Api.Dtos;

public class RegistroDto
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    // Máximo 72: o BCrypt só considera os 72 primeiros bytes da senha.
    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    [MaxLength(72, ErrorMessage = "A senha deve ter no máximo 72 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Senha { get; set; } = string.Empty;
}

public record UsuarioDto(int Id, string Email, string Papel);

public record TokenDto(string Token, DateTimeOffset ExpiraEm, string Email, string Papel);
