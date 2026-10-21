namespace Patinhas.Api.Servicos;

// Quem está fazendo o pedido. Vem SEMPRE dos claims do token, nunca do corpo nem da URL.
public record Solicitante(int UsuarioId, bool EhAdmin);
