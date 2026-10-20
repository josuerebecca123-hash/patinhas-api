using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Patinhas.Api.Dominio;
using Patinhas.Api.Dtos;
using Patinhas.Api.Servicos;
using Xunit;

namespace Patinhas.Testes.Servicos;

public class CatalogoServicosTestes : IDisposable
{
    private readonly BancoDeTestes _banco = new();
    private readonly CatalogoServicos _catalogo;

    public CatalogoServicosTestes()
    {
        _catalogo = new CatalogoServicos(_banco.Db, NullLogger<CatalogoServicos>.Instance);
    }

    public void Dispose() => _banco.Dispose();

    private async Task<Servico> SalvarAsync(string nome, string descricao = "Descrição do serviço.", bool ativo = true)
    {
        var servico = new Servico(nome, descricao, 50m, 30);
        servico.DefinirAtivo(ativo);
        _banco.Db.Servicos.Add(servico);
        await _banco.Db.SaveChangesAsync();
        return servico;
    }

    private static ServicoCriarDto NovoServico(string nome = "Banho de teste", decimal preco = 60m, int duracao = 45) =>
        new() { Nome = nome, Descricao = "Banho completo.", Preco = preco, DuracaoMinutos = duracao };

    [Fact]
    public async Task Criar_ComDadosValidos_PersisteEFicaAtivo()
    {
        var resultado = await _catalogo.CriarAsync(NovoServico());

        Assert.True(resultado.Sucesso);
        Assert.True(resultado.Valor!.Id > 0);
        Assert.True(resultado.Valor.Ativo);
        Assert.Equal(1, await _banco.Db.Servicos.CountAsync());
    }

    [Fact]
    public async Task Criar_ComPrecoInvalido_DevolveValidacao()
    {
        var resultado = await _catalogo.CriarAsync(NovoServico(preco: 0));

        Assert.False(resultado.Sucesso);
        Assert.Equal(TipoErro.Validacao, resultado.Erro);
    }

    [Fact]
    public async Task Listar_Paginado_DevolveTotalETotalPaginas()
    {
        for (var i = 1; i <= 12; i++)
            await SalvarAsync($"Serviço {i:00}");

        var primeira = await _catalogo.ListarAsync(null, 1, 5, false);
        var ultima = await _catalogo.ListarAsync(null, 3, 5, false);

        Assert.Equal(5, primeira.Valor!.Itens.Count);
        Assert.Equal(12, primeira.Valor.Total);
        Assert.Equal(3, primeira.Valor.TotalPaginas);
        Assert.Equal("Serviço 01", primeira.Valor.Itens[0].Nome);
        Assert.Equal(2, ultima.Valor!.Itens.Count);
    }

    [Fact]
    public async Task Listar_ComBusca_FiltraPorNomeOuDescricaoSemDiferenciarMaiusculas()
    {
        await SalvarAsync("Banho porte médio", "Banho completo com secagem.");
        await SalvarAsync("Tosa higiênica", "Aparação das regiões íntimas.");

        var porNome = await _catalogo.ListarAsync("BANHO", 1, 10, false);
        var porDescricao = await _catalogo.ListarAsync("aparação", 1, 10, false);

        Assert.Equal(1, porNome.Valor!.Total);
        Assert.Equal("Banho porte médio", porNome.Valor.Itens[0].Nome);
        Assert.Equal(1, porDescricao.Valor!.Total);
        Assert.Equal("Tosa higiênica", porDescricao.Valor.Itens[0].Nome);
    }

    [Fact]
    public async Task Listar_TermoSemResultado_DevolveSucessoComListaVaziaETotalZero()
    {
        await SalvarAsync("Banho porte médio");

        var resultado = await _catalogo.ListarAsync("zzzz", 1, 10, false);

        Assert.True(resultado.Sucesso);
        Assert.Empty(resultado.Valor!.Itens);
        Assert.Equal(0, resultado.Valor.Total);
        Assert.Equal(0, resultado.Valor.TotalPaginas);
    }

    [Fact]
    public async Task Listar_Publico_EscondeInativosEAdminVe()
    {
        await SalvarAsync("Ativo");
        await SalvarAsync("Inativo", ativo: false);

        var publico = await _catalogo.ListarAsync(null, 1, 10, incluirInativos: false);
        var admin = await _catalogo.ListarAsync(null, 1, 10, incluirInativos: true);

        Assert.Equal(1, publico.Valor!.Total);
        Assert.Equal(2, admin.Valor!.Total);
    }

    [Fact]
    public async Task Listar_ComParametrosForaDoLimite_AjustaPaginaETamanho()
    {
        await SalvarAsync("Banho");

        var resultado = await _catalogo.ListarAsync(null, -5, 1000, false);

        Assert.Equal(1, resultado.Valor!.Pagina);
        Assert.Equal(50, resultado.Valor.Tamanho);
    }

    [Fact]
    public async Task Obter_Existente_DevolveODto()
    {
        var salvo = await SalvarAsync("Banho");

        var resultado = await _catalogo.ObterAsync(salvo.Id, false);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Banho", resultado.Valor!.Nome);
    }

    [Fact]
    public async Task Obter_Inexistente_DevolveNaoEncontrado()
    {
        var resultado = await _catalogo.ObterAsync(9999, false);

        Assert.False(resultado.Sucesso);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro);
    }

    [Fact]
    public async Task Obter_Inativo_SoApareceParaAdmin()
    {
        var inativo = await SalvarAsync("Inativo", ativo: false);

        var publico = await _catalogo.ObterAsync(inativo.Id, incluirInativos: false);
        var admin = await _catalogo.ObterAsync(inativo.Id, incluirInativos: true);

        Assert.Equal(TipoErro.NaoEncontrado, publico.Erro);
        Assert.True(admin.Sucesso);
    }

    [Fact]
    public async Task Atualizar_Existente_MudaOsCampos()
    {
        var salvo = await SalvarAsync("Banho");
        var dto = new ServicoAtualizarDto
        {
            Nome = "Banho premium", Descricao = "Mais completo.", Preco = 120m, DuracaoMinutos = 90, Ativo = false
        };

        var resultado = await _catalogo.AtualizarAsync(salvo.Id, dto);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Banho premium", resultado.Valor!.Nome);
        Assert.Equal(120m, resultado.Valor.Preco);
        Assert.False(resultado.Valor.Ativo);
    }

    [Fact]
    public async Task Atualizar_Inexistente_DevolveNaoEncontrado()
    {
        var resultado = await _catalogo.AtualizarAsync(9999, new ServicoAtualizarDto
        {
            Nome = "X", Descricao = "Y", Preco = 10m, DuracaoMinutos = 30
        });

        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro);
    }

    [Fact]
    public async Task Excluir_SemAgendamentos_Remove()
    {
        var salvo = await SalvarAsync("Banho");

        var resultado = await _catalogo.ExcluirAsync(salvo.Id);

        Assert.True(resultado.Sucesso);
        Assert.Equal(0, await _banco.Db.Servicos.CountAsync());
    }

    [Fact]
    public async Task Excluir_ComAgendamentos_DevolveConflito()
    {
        var servico = await SalvarAsync("Banho");
        var usuario = new Usuario("ana@exemplo.com", "hash", Papeis.Cliente);
        _banco.Db.Usuarios.Add(usuario);
        await _banco.Db.SaveChangesAsync();
        _banco.Db.Agendamentos.Add(new Agendamento(
            servico.Id, usuario.Id, "Thor", new DateTime(2026, 11, 10, 14, 0, 0, DateTimeKind.Utc)));
        await _banco.Db.SaveChangesAsync();

        var resultado = await _catalogo.ExcluirAsync(servico.Id);

        Assert.False(resultado.Sucesso);
        Assert.Equal(TipoErro.Conflito, resultado.Erro);
        Assert.Equal(1, await _banco.Db.Servicos.CountAsync());
    }

    [Fact]
    public async Task Excluir_Inexistente_DevolveNaoEncontrado()
    {
        var resultado = await _catalogo.ExcluirAsync(9999);

        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro);
    }

    private static bool PassaNaValidacao(object dto) =>
        Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), true);

    [Theory]
    [InlineData("", 60, 45)]      // nome vazio
    [InlineData("Banho", 0, 45)]  // preço zero
    [InlineData("Banho", -5, 45)] // preço negativo
    [InlineData("Banho", 60, 2)]  // duração curta demais
    public void ServicoCriarDto_ComDadosInvalidos_NaoPassaNaValidacao(string nome, int preco, int duracao)
    {
        Assert.False(PassaNaValidacao(NovoServico(nome, preco, duracao)));
    }

    [Fact]
    public void ServicoCriarDto_ComDadosValidos_PassaNaValidacao()
    {
        Assert.True(PassaNaValidacao(NovoServico()));
    }
}
