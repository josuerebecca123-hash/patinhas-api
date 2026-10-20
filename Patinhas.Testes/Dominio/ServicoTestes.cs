using Patinhas.Api.Dominio;
using Xunit;

namespace Patinhas.Testes.Dominio;

public class ServicoTestes
{
    [Fact]
    public void Criar_ComDadosValidos_FicaAtivo()
    {
        var servico = new Servico("Banho porte médio", "Banho completo.", 70m, 60);

        Assert.Equal("Banho porte médio", servico.Nome);
        Assert.Equal(70m, servico.Preco);
        Assert.Equal(60, servico.DuracaoMinutos);
        Assert.True(servico.Ativo);
    }

    [Fact]
    public void Criar_ComNomeVazio_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Servico("  ", "x", 50m, 30));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Criar_ComPrecoNaoPositivo_Lanca(int preco)
    {
        Assert.Throws<ArgumentException>(() => new Servico("Banho", "x", preco, 30));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1441)]
    public void Criar_ComDuracaoForaDoLimite_Lanca(int duracao)
    {
        Assert.Throws<ArgumentException>(() => new Servico("Banho", "x", 50m, duracao));
    }
}
