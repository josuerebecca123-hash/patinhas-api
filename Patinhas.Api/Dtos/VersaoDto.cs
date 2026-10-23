namespace Patinhas.Api.Dtos;

// Resposta do GET /api/versao: prova qual commit do GitHub está no ar.
public record VersaoDto(string Commit, string Ambiente);
