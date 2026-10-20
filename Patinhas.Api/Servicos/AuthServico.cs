using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Patinhas.Api.Dados;
using Patinhas.Api.Dominio;
using Patinhas.Api.Dtos;
using Patinhas.Api.Seguranca;

namespace Patinhas.Api.Servicos;

public class AuthServico : IAuthServico
{
    private static readonly TimeSpan ValidadeDoToken = TimeSpan.FromHours(2);

    private readonly PatinhasDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthServico> _logger;

    public AuthServico(PatinhasDbContext db, IConfiguration config, ILogger<AuthServico> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<Resultado<UsuarioDto>> RegistrarAsync(RegistroDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _db.Usuarios.AnyAsync(u => u.Email == email))
        {
            _logger.LogWarning("Registro recusado: o e-mail {Email} já está cadastrado", email);
            return Resultado<UsuarioDto>.Falha(TipoErro.Conflito, "Já existe uma conta com este e-mail.");
        }

        // O registro público cria SEMPRE "cliente". O admin só nasce no seed.
        var usuario = new Usuario(email, BCrypt.Net.BCrypt.HashPassword(dto.Senha), Papeis.Cliente);
        _db.Usuarios.Add(usuario);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Duas requisições com o mesmo e-mail ao mesmo tempo: o índice único do banco decide.
            _logger.LogWarning("Registro recusado por conflito no banco para o e-mail {Email}", email);
            return Resultado<UsuarioDto>.Falha(TipoErro.Conflito, "Já existe uma conta com este e-mail.");
        }

        _logger.LogInformation("Usuário {Email} registrado com o papel {Papel}", usuario.Email, usuario.Papel);
        return Resultado<UsuarioDto>.Ok(new UsuarioDto(usuario.Id, usuario.Email, usuario.Papel));
    }

    public async Task<Resultado<TokenDto>> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        // A mesma mensagem para e-mail inexistente e senha errada: não revela qual dos dois falhou.
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
        {
            _logger.LogWarning("Login recusado para o e-mail {Email}", email);
            return Resultado<TokenDto>.Falha(TipoErro.NaoAutenticado, "E-mail ou senha inválidos.");
        }

        _logger.LogInformation("Login realizado por {Email}", usuario.Email);
        return Resultado<TokenDto>.Ok(GerarToken(usuario));
    }

    private TokenDto GerarToken(Usuario usuario)
    {
        var segredo = _config["Jwt:Segredo"]
            ?? throw new InvalidOperationException("O segredo do JWT (Jwt:Segredo) não foi configurado.");

        var expira = DateTime.UtcNow.Add(ValidadeDoToken);

        var descritor = new SecurityTokenDescriptor
        {
            // A identidade vai nos claims. É de lá que os controllers vão ler quem está logado.
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Papel)
            }),
            Issuer = JwtConstantes.Emissor,
            Audience = JwtConstantes.Audiencia,
            Expires = expira,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(segredo)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descritor);
        return new TokenDto(token, new DateTimeOffset(expira), usuario.Email, usuario.Papel);
    }
}
