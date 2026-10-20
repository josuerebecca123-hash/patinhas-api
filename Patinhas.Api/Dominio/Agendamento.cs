namespace Patinhas.Api.Dominio;

public class Agendamento
{
    public int Id { get; private set; }
    public int ServicoId { get; private set; }
    public Servico? Servico { get; private set; }
    public int UsuarioId { get; private set; }
    public Usuario? Usuario { get; private set; }
    public string NomeDoPet { get; private set; }
    public DateTime DataHora { get; private set; }
    public StatusAgendamento Status { get; private set; }

    // Construtor vazio só para o EF Core.
    private Agendamento()
    {
        NomeDoPet = null!;
    }

    public Agendamento(int servicoId, int usuarioId, string nomeDoPet, DateTime dataHora)
    {
        if (servicoId <= 0)
            throw new ArgumentException("Serviço inválido.", nameof(servicoId));
        if (usuarioId <= 0)
            throw new ArgumentException("Usuário inválido.", nameof(usuarioId));
        if (string.IsNullOrWhiteSpace(nomeDoPet))
            throw new ArgumentException("O nome do pet é obrigatório.", nameof(nomeDoPet));
        if (dataHora.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A data e hora devem estar em UTC.", nameof(dataHora));

        ServicoId = servicoId;
        UsuarioId = usuarioId;
        NomeDoPet = nomeDoPet.Trim();
        DataHora = dataHora;
        Status = StatusAgendamento.Marcado;
    }

    public void Cancelar() => Status = StatusAgendamento.Cancelado;

    // Regra do 409 em forma pura: encostar (fim == início) não é sobrepor.
    public static bool HaSobreposicao(DateTime inicioA, int duracaoMinutosA, DateTime inicioB, int duracaoMinutosB)
    {
        var fimA = inicioA.AddMinutes(duracaoMinutosA);
        var fimB = inicioB.AddMinutes(duracaoMinutosB);
        return inicioA < fimB && inicioB < fimA;
    }
}
