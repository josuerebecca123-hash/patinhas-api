using Patinhas.Api.Dominio;
using Xunit;

namespace Patinhas.Testes.Dominio;

public class AgendamentoTestes
{
    private static readonly DateTime Base = new(2026, 11, 10, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Criar_ComDadosValidos_FicaMarcado()
    {
        var agendamento = new Agendamento(1, 2, "  Thor ", Base);

        Assert.Equal("Thor", agendamento.NomeDoPet);
        Assert.Equal(StatusAgendamento.Marcado, agendamento.Status);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Criar_ComDataQueNaoEhUtc_Lanca(DateTimeKind tipo)
    {
        var data = DateTime.SpecifyKind(new DateTime(2026, 11, 10, 14, 0, 0), tipo);

        Assert.Throws<ArgumentException>(() => new Agendamento(1, 2, "Thor", data));
    }

    [Fact]
    public void Criar_SemNomeDoPet_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Agendamento(1, 2, "", Base));
    }

    [Fact]
    public void Cancelar_MudaOStatusParaCancelado()
    {
        var agendamento = new Agendamento(1, 2, "Thor", Base);

        agendamento.Cancelar();

        Assert.Equal(StatusAgendamento.Cancelado, agendamento.Status);
    }

    // A (14:00, 60 min) contra B, que começa 'deslocamento' minutos depois de A.
    // Esta é a regra do 409 em forma pura.
    [Theory]
    [InlineData(30, 60, true)]    // 14:30 começa antes de A terminar: conflito
    [InlineData(0, 60, true)]     // mesmo horário: conflito
    [InlineData(-30, 60, true)]   // 13:30 termina depois de A começar: conflito
    [InlineData(10, 10, true)]    // B dentro de A: conflito
    [InlineData(60, 60, false)]   // B começa quando A termina: encostar não é sobrepor
    [InlineData(-60, 60, false)]  // B termina quando A começa: sem conflito
    [InlineData(120, 30, false)]  // bem depois: sem conflito
    public void HaSobreposicao_DetectaConflitoDeHorario(int deslocamento, int duracaoB, bool esperado)
    {
        var resultado = Agendamento.HaSobreposicao(Base, 60, Base.AddMinutes(deslocamento), duracaoB);

        Assert.Equal(esperado, resultado);
    }
}
