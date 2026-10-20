namespace Patinhas.Api.Servicos;

public enum TipoErro
{
    Validacao,
    NaoEncontrado,
    Proibido,
    Conflito,
    NaoAutenticado
}

// Resultado de uma operação de serviço, sem throw para erro de negócio.
// O controller traduz: Validacao -> 400, NaoAutenticado -> 401, Proibido -> 403, NaoEncontrado -> 404, Conflito -> 409.
public class Resultado
{
    public bool Sucesso { get; }
    public TipoErro? Erro { get; }
    public string? Mensagem { get; }

    protected Resultado(bool sucesso, TipoErro? erro, string? mensagem)
    {
        Sucesso = sucesso;
        Erro = erro;
        Mensagem = mensagem;
    }

    public static Resultado Ok() => new(true, null, null);

    public static Resultado Falha(TipoErro erro, string mensagem) => new(false, erro, mensagem);
}

public class Resultado<T> : Resultado
{
    public T? Valor { get; }

    private Resultado(bool sucesso, T? valor, TipoErro? erro, string? mensagem)
        : base(sucesso, erro, mensagem)
    {
        Valor = valor;
    }

    public static Resultado<T> Ok(T valor) => new(true, valor, null, null);

    public static new Resultado<T> Falha(TipoErro erro, string mensagem) => new(false, default, erro, mensagem);
}
