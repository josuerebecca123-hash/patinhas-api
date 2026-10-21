namespace Patinhas.Testes;

// Relógio de teste: "agora" fica sempre no mesmo instante.
public sealed class RelogioFixo : TimeProvider
{
    private readonly DateTimeOffset _agora;

    public RelogioFixo(DateTimeOffset agora) => _agora = agora;

    public override DateTimeOffset GetUtcNow() => _agora;
}
