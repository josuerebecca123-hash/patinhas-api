using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Patinhas.Api.Dominio;
using Patinhas.Api.Dtos;
using Patinhas.Api.Seguranca;
using Patinhas.Api.Servicos;
using Xunit;

namespace Patinhas.Testes.Servicos;

public class AuthServicoTestes : IDisposable
{
    private const string Segredo = "segredo-de-teste-com-mais-de-trinta-e-dois-caracteres";

    private readonly BancoDeTestes _banco = new();
    private readonly AuthServico _servico;

    public AuthServicoTestes()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Segredo"] = Segredo })
            .Build();

        _servico = new AuthServico(_banco.Db, config, NullLogger<AuthServico>.Instance);
    }

    public void Dispose() => _banco.Dispose();

    private static RegistroDto NovoRegistro(string email = "ana@exemplo.com", string senha = "Senha@123") =>
        new() { Email = email, Senha = senha };

    [Fact]
    public async Task Registrar_EmailNovo_CriaClienteComSenhaEmHash()
    {
        var resultado = await _servico.RegistrarAsync(NovoRegistro());

        Assert.True(resultado.Sucesso);
        Assert.Equal(Papeis.Cliente, resultado.Valor!.Papel);

        var salvo = await _banco.Db.Usuarios.SingleAsync();
        Assert.NotEqual("Senha@123", salvo.SenhaHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Senha@123", salvo.SenhaHash));
    }

    [Fact]
    public async Task Registrar_EmailRepetido_DevolveConflito()
    {
        await _servico.RegistrarAsync(NovoRegistro("ana@exemplo.com"));

        var repetido = await _servico.RegistrarAsync(NovoRegistro("ANA@exemplo.com"));

        Assert.False(repetido.Sucesso);
        Assert.Equal(TipoErro.Conflito, repetido.Erro);
        Assert.Equal(1, await _banco.Db.Usuarios.CountAsync());
    }

    [Fact]
    public async Task Login_CredenciaisCorretas_DevolveToken()
    {
        await _servico.RegistrarAsync(NovoRegistro());

        var login = await _servico.LoginAsync(new LoginDto { Email = "ANA@exemplo.com", Senha = "Senha@123" });

        Assert.True(login.Sucesso);
        Assert.False(string.IsNullOrWhiteSpace(login.Valor!.Token));
        Assert.Equal(Papeis.Cliente, login.Valor.Papel);
    }

    [Fact]
    public async Task Login_SenhaErrada_DevolveNaoAutenticado()
    {
        await _servico.RegistrarAsync(NovoRegistro());

        var login = await _servico.LoginAsync(new LoginDto { Email = "ana@exemplo.com", Senha = "senha-errada" });

        Assert.False(login.Sucesso);
        Assert.Equal(TipoErro.NaoAutenticado, login.Erro);
    }

    [Fact]
    public async Task Login_EmailInexistente_DevolveNaoAutenticado()
    {
        var login = await _servico.LoginAsync(new LoginDto { Email = "ninguem@exemplo.com", Senha = "Senha@123" });

        Assert.False(login.Sucesso);
        Assert.Equal(TipoErro.NaoAutenticado, login.Erro);
    }

    // Valida o token do mesmo jeito que o JwtBearer do Program.cs e confere que a
    // identidade e o papel chegam pelos claims (é de lá que o código vai ler o usuário).
    [Fact]
    public async Task Login_TokenGerado_ValidaETrazIdentidadeEPapelNosClaims()
    {
        await _servico.RegistrarAsync(NovoRegistro());
        var idDoUsuario = (await _banco.Db.Usuarios.SingleAsync()).Id;
        var login = await _servico.LoginAsync(new LoginDto { Email = "ana@exemplo.com", Senha = "Senha@123" });

        var parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtConstantes.Emissor,
            ValidateAudience = true,
            ValidAudience = JwtConstantes.Audiencia,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Segredo)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        var handler = new JsonWebTokenHandler { MapInboundClaims = true };

        var validacao = await handler.ValidateTokenAsync(login.Valor!.Token, parametros);

        Assert.True(validacao.IsValid);
        var principal = new ClaimsPrincipal(validacao.ClaimsIdentity);
        Assert.Equal(idDoUsuario.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.True(principal.IsInRole(Papeis.Cliente));
    }

    private static bool PassaNaValidacao(object dto) =>
        Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), true);

    [Theory]
    [InlineData("", "Senha@123")]
    [InlineData("semarroba", "Senha@123")]
    [InlineData("ana@exemplo.com", "curta")]
    [InlineData("ana@exemplo.com", "")]
    public void Registro_ComDadosInvalidos_NaoPassaNaValidacao(string email, string senha)
    {
        Assert.False(PassaNaValidacao(NovoRegistro(email, senha)));
    }

    [Fact]
    public void Registro_ComDadosValidos_PassaNaValidacao()
    {
        Assert.True(PassaNaValidacao(NovoRegistro()));
    }
}
