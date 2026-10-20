namespace Patinhas.Api.Dominio;

public class Servico
{
    public int Id { get; private set; }
    public string Nome { get; private set; }
    public string Descricao { get; private set; }
    public decimal Preco { get; private set; }
    public int DuracaoMinutos { get; private set; }
    public bool Ativo { get; private set; }

    // Construtor vazio só para o EF Core.
    private Servico()
    {
        Nome = null!;
        Descricao = null!;
    }

    public Servico(string nome, string descricao, decimal preco, int duracaoMinutos)
    {
        Nome = null!;
        Descricao = null!;
        Atualizar(nome, descricao, preco, duracaoMinutos);
        Ativo = true;
    }

    public void Atualizar(string nome, string descricao, decimal preco, int duracaoMinutos)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome é obrigatório.", nameof(nome));
        if (preco <= 0)
            throw new ArgumentException("O preço deve ser maior que zero.", nameof(preco));
        if (duracaoMinutos < 5 || duracaoMinutos > 1440)
            throw new ArgumentException("A duração deve ficar entre 5 e 1440 minutos.", nameof(duracaoMinutos));

        Nome = nome.Trim();
        Descricao = descricao?.Trim() ?? string.Empty;
        Preco = preco;
        DuracaoMinutos = duracaoMinutos;
    }

    public void DefinirAtivo(bool ativo) => Ativo = ativo;
}
