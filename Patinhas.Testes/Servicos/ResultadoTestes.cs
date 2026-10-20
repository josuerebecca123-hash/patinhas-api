using Patinhas.Api.Servicos;
using Xunit;

namespace Patinhas.Testes.Servicos;

public class ResultadoTestes
{
    [Fact]
    public void Ok_TrazOValorESemErro()
    {
        var resultado = Resultado<int>.Ok(42);

        Assert.True(resultado.Sucesso);
        Assert.Equal(42, resultado.Valor);
        Assert.Null(resultado.Erro);
    }

    [Fact]
    public void Falha_TrazOTipoEAMensagem()
    {
        var resultado = Resultado<int>.Falha(TipoErro.Conflito, "Horário ocupado.");

        Assert.False(resultado.Sucesso);
        Assert.Equal(TipoErro.Conflito, resultado.Erro);
        Assert.Equal("Horário ocupado.", resultado.Mensagem);
        Assert.Equal(default, resultado.Valor);
    }
}
