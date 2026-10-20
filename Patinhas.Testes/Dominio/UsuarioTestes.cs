using Patinhas.Api.Dominio;
using Xunit;

namespace Patinhas.Testes.Dominio;

public class UsuarioTestes
{
    [Fact]
    public void Criar_ComDadosValidos_NormalizaEmailParaMinusculas()
    {
        var usuario = new Usuario("  Marina.Souza@Exemplo.com ", "hash-qualquer", Papeis.Cliente);

        Assert.Equal("marina.souza@exemplo.com", usuario.Email);
        Assert.Equal(Papeis.Cliente, usuario.Papel);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("semarroba.com")]
    public void Criar_ComEmailInvalido_Lanca(string email)
    {
        Assert.Throws<ArgumentException>(() => new Usuario(email, "hash", Papeis.Cliente));
    }

    [Fact]
    public void Criar_ComPapelInvalido_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Usuario("a@exemplo.com", "hash", "superusuario"));
    }

    [Fact]
    public void Criar_SemHashDeSenha_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Usuario("a@exemplo.com", "", Papeis.Cliente));
    }
}
