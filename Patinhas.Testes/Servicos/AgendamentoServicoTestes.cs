using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Patinhas.Api.Dominio;
using Patinhas.Api.Dtos;
using Patinhas.Api.Servicos;
using Xunit;

namespace Patinhas.Testes.Servicos;

public class AgendamentoServicoTestes : IDisposable
{
    private readonly BancoDeTestes _banco = new();
    private readonly AgendamentoServico _servico;

    private readonly Usuario _admin;
    private readonly Usuario _ana;
    private readonly Usuario _bruno;
    private readonly Servico _banho;   // 60 minutos
    private readonly Servico _tosa;    // 90 minutos

    private readonly Solicitante _comoAdmin;
    private readonly Solicitante _comoAna;
    private readonly Solicitante _comoBruno;

    public AgendamentoServicoTestes()
    {
        // "Agora" fixo: 1º de novembro de 2026, 12:00 UTC.
        var relogio = new RelogioFixo(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
        _servico = new AgendamentoServico(_banco.Db, relogio, NullLogger<AgendamentoServico>.Instance);

        _admin = new Usuario("admin@exemplo.com", "hash", Papeis.Admin);
        _ana = new Usuario("ana@exemplo.com", "hash", Papeis.Cliente);
        _bruno = new Usuario("bruno@exemplo.com", "hash", Papeis.Cliente);
        _banho = new Servico("Banho", "Banho completo.", 70m, 60);
        _tosa = new Servico("Tosa", "Tosa completa.", 110m, 90);

        _banco.Db.Usuarios.AddRange(_admin, _ana, _bruno);
        _banco.Db.Servicos.AddRange(_banho, _tosa);
        _banco.Db.SaveChanges();

        _comoAdmin = new Solicitante(_admin.Id, true);
        _comoAna = new Solicitante(_ana.Id, false);
        _comoBruno = new Solicitante(_bruno.Id, false);
    }

    public void Dispose() => _banco.Dispose();

    private static DateTime Em(int dia, int hora, int minuto = 0) =>
        new(2026, 11, dia, hora, minuto, 0, DateTimeKind.Utc);

    private Task<Resultado<AgendamentoDto>> CriarAsync(Solicitante quem, Servico servico, DateTime inicioUtc, string pet = "Thor") =>
        _servico.CriarAsync(
            new AgendamentoCriarDto { ServicoId = servico.Id, NomeDoPet = pet, DataHora = new DateTimeOffset(inicioUtc) },
            quem);

    // ---------- A regra do 409 ----------

    [Fact]
    public async Task Criar_HorarioLivre_CriaMarcadoComODonoDoSolicitante()
    {
        var resultado = await CriarAsync(_comoAna, _banho, Em(10, 14));

        Assert.True(resultado.Sucesso);
        Assert.Equal(_ana.Id, resultado.Valor!.UsuarioId);
        Assert.Equal("Marcado", resultado.Valor.Status);
        Assert.Equal(new DateTimeOffset(Em(10, 15)), resultado.Valor.DataHoraFim);
    }

    [Fact]
    public async Task Criar_MesmoHorarioDoMesmoServico_DevolveConflito()
    {
        await CriarAsync(_comoAna, _banho, Em(10, 14));

        var conflito = await CriarAsync(_comoBruno, _banho, Em(10, 14));

        Assert.False(conflito.Sucesso);
        Assert.Equal(TipoErro.Conflito, conflito.Erro);
        Assert.Equal(1, await _banco.Db.Agendamentos.CountAsync());
    }

    [Fact]
    public async Task Criar_HorarioQueSobrepoeOFimDoOutro_DevolveConflito()
    {
        await CriarAsync(_comoAna, _banho, Em(10, 14));          // 14:00 a 15:00

        var conflito = await CriarAsync(_comoBruno, _banho, Em(10, 14, 30));

        Assert.Equal(TipoErro.Conflito, conflito.Erro);
    }

    [Fact]
    public async Task Criar_HorarioQueTerminaDepoisDoInicioDoOutro_DevolveConflito()
    {
        await CriarAsync(_comoAna, _banho, Em(10, 14));          // 14:00 a 15:00

        var conflito = await CriarAsync(_comoBruno, _banho, Em(10, 13, 30));

        Assert.Equal(TipoErro.Conflito, conflito.Erro);
    }

    [Fact]
    public async Task Criar_HorarioQueApenasEncosta_NaoConflita()
    {
        await CriarAsync(_comoAna, _banho, Em(10, 14));          // 14:00 a 15:00

        var seguinte = await CriarAsync(_comoBruno, _banho, Em(10, 15));
        var anterior = await CriarAsync(_comoBruno, _banho, Em(10, 13));

        Assert.True(seguinte.Sucesso);
        Assert.True(anterior.Sucesso);
    }

    [Fact]
    public async Task Criar_NoMesmoHorarioMasDeOutroServico_NaoConflita()
    {
        await CriarAsync(_comoAna, _banho, Em(10, 14));

        var outroServico = await CriarAsync(_comoBruno, _tosa, Em(10, 14));

        Assert.True(outroServico.Sucesso);
    }

    [Fact]
    public async Task Criar_AposCancelamento_LiberaOHorario()
    {
        var primeiro = await CriarAsync(_comoAna, _banho, Em(10, 14));
        await _servico.CancelarAsync(primeiro.Valor!.Id, _comoAna);

        var novo = await CriarAsync(_comoBruno, _banho, Em(10, 14));

        Assert.True(novo.Sucesso);
    }

    [Fact]
    public async Task Criar_ComFusoDiferente_ConverteParaUtcEConflita()
    {
        await CriarAsync(_comoAna, _banho, Em(10, 14));          // 14:00 UTC

        // 11:00 em UTC-3 é exatamente 14:00 UTC.
        var emBrasilia = new DateTimeOffset(2026, 11, 10, 11, 0, 0, TimeSpan.FromHours(-3));
        var conflito = await _servico.CriarAsync(
            new AgendamentoCriarDto { ServicoId = _banho.Id, NomeDoPet = "Mel", DataHora = emBrasilia },
            _comoBruno);

        Assert.Equal(TipoErro.Conflito, conflito.Erro);
    }

    // ---------- Validações de negócio ----------

    [Fact]
    public async Task Criar_NoPassado_DevolveValidacao()
    {
        var resultado = await CriarAsync(_comoAna, _banho, Em(1, 10));   // antes do "agora" (1º, 12:00)

        Assert.Equal(TipoErro.Validacao, resultado.Erro);
    }

    [Fact]
    public async Task Criar_ServicoInexistente_DevolveValidacao()
    {
        var resultado = await _servico.CriarAsync(
            new AgendamentoCriarDto { ServicoId = 9999, NomeDoPet = "Thor", DataHora = new DateTimeOffset(Em(10, 14)) },
            _comoAna);

        Assert.Equal(TipoErro.Validacao, resultado.Erro);
    }

    [Fact]
    public async Task Criar_ServicoInativo_DevolveValidacao()
    {
        _banho.DefinirAtivo(false);
        await _banco.Db.SaveChangesAsync();

        var resultado = await CriarAsync(_comoAna, _banho, Em(10, 14));

        Assert.Equal(TipoErro.Validacao, resultado.Erro);
    }

    // ---------- Quem vê o quê ----------

    [Fact]
    public async Task Listar_Cliente_VeSomenteOsSeus()
    {
        await CriarAsync(_comoAna, _banho, Em(10, 9));
        await CriarAsync(_comoAna, _banho, Em(10, 11));
        await CriarAsync(_comoBruno, _banho, Em(10, 14));

        var daAna = await _servico.ListarAsync(1, 10, _comoAna);

        Assert.Equal(2, daAna.Valor!.Total);
        Assert.All(daAna.Valor.Itens, item => Assert.Equal(_ana.Id, item.UsuarioId));
    }

    [Fact]
    public async Task Listar_Admin_VeTodos()
    {
        await CriarAsync(_comoAna, _banho, Em(10, 9));
        await CriarAsync(_comoBruno, _banho, Em(10, 14));

        var todos = await _servico.ListarAsync(1, 10, _comoAdmin);

        Assert.Equal(2, todos.Valor!.Total);
    }

    // ---------- Autorização: o teste de IDOR ----------

    [Fact]
    public async Task Obter_AgendamentoDeOutroCliente_DevolveProibido()
    {
        var daAna = await CriarAsync(_comoAna, _banho, Em(10, 14));

        var tentativa = await _servico.ObterAsync(daAna.Valor!.Id, _comoBruno);

        Assert.False(tentativa.Sucesso);
        Assert.Equal(TipoErro.Proibido, tentativa.Erro);
    }

    [Fact]
    public async Task Obter_ComoDonoOuAdmin_DevolveOAgendamento()
    {
        var daAna = await CriarAsync(_comoAna, _banho, Em(10, 14));

        var comoDono = await _servico.ObterAsync(daAna.Valor!.Id, _comoAna);
        var comoAdmin = await _servico.ObterAsync(daAna.Valor.Id, _comoAdmin);

        Assert.True(comoDono.Sucesso);
        Assert.True(comoAdmin.Sucesso);
    }

    [Fact]
    public async Task Obter_Inexistente_DevolveNaoEncontrado()
    {
        var resultado = await _servico.ObterAsync(9999, _comoAna);

        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro);
    }

    [Fact]
    public async Task Cancelar_AgendamentoDeOutroCliente_DevolveProibidoEMantemMarcado()
    {
        var daAna = await CriarAsync(_comoAna, _banho, Em(10, 14));

        var tentativa = await _servico.CancelarAsync(daAna.Valor!.Id, _comoBruno);

        Assert.Equal(TipoErro.Proibido, tentativa.Erro);
        var noBanco = await _banco.Db.Agendamentos.SingleAsync();
        Assert.Equal(StatusAgendamento.Marcado, noBanco.Status);
    }

    [Fact]
    public async Task Cancelar_ComoDono_MudaOStatus()
    {
        var daAna = await CriarAsync(_comoAna, _banho, Em(10, 14));

        var resultado = await _servico.CancelarAsync(daAna.Valor!.Id, _comoAna);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Cancelado", resultado.Valor!.Status);
    }

    [Fact]
    public async Task Cancelar_ComoAdmin_FuncionaMesmoSendoDeOutroUsuario()
    {
        var daAna = await CriarAsync(_comoAna, _banho, Em(10, 14));

        var resultado = await _servico.CancelarAsync(daAna.Valor!.Id, _comoAdmin);

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public async Task Cancelar_JaCancelado_DevolveConflito()
    {
        var daAna = await CriarAsync(_comoAna, _banho, Em(10, 14));
        await _servico.CancelarAsync(daAna.Valor!.Id, _comoAna);

        var denovo = await _servico.CancelarAsync(daAna.Valor.Id, _comoAna);

        Assert.Equal(TipoErro.Conflito, denovo.Erro);
    }

    [Fact]
    public async Task Cancelar_Inexistente_DevolveNaoEncontrado()
    {
        var resultado = await _servico.CancelarAsync(9999, _comoAna);

        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro);
    }

    // ---------- Validação do DTO (400 automático) ----------

    private static bool PassaNaValidacao(object dto) =>
        Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), true);

    [Theory]
    [InlineData(0, "Thor")]    // sem serviço
    [InlineData(1, "")]        // sem nome do pet
    [InlineData(1, "   ")]     // nome só com espaços
    public void AgendamentoCriarDto_ComDadosInvalidos_NaoPassaNaValidacao(int servicoId, string pet)
    {
        var dto = new AgendamentoCriarDto { ServicoId = servicoId, NomeDoPet = pet, DataHora = new DateTimeOffset(Em(10, 14)) };

        Assert.False(PassaNaValidacao(dto));
    }

    [Fact]
    public void AgendamentoCriarDto_ComDadosValidos_PassaNaValidacao()
    {
        var dto = new AgendamentoCriarDto { ServicoId = 1, NomeDoPet = "Thor", DataHora = new DateTimeOffset(Em(10, 14)) };

        Assert.True(PassaNaValidacao(dto));
    }
}
