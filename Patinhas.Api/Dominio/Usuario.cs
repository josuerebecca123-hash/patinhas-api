namespace Patinhas.Api.Dominio;

public class Usuario
{
    public int Id { get; private set; }
    public string Email { get; private set; }
    public string SenhaHash { get; private set; }
    public string Papel { get; private set; }

    // Construtor vazio só para o EF Core.
    private Usuario()
    {
        Email = null!;
        SenhaHash = null!;
        Papel = null!;
    }

    public Usuario(string email, string senhaHash, string papel)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("E-mail inválido.", nameof(email));
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("O hash da senha é obrigatório.", nameof(senhaHash));
        if (papel != Papeis.Admin && papel != Papeis.Cliente)
            throw new ArgumentException("Papel inválido.", nameof(papel));

        Email = email.Trim().ToLowerInvariant();
        SenhaHash = senhaHash;
        Papel = papel;
    }
}
