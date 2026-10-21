using Microsoft.EntityFrameworkCore;
using Patinhas.Api.Dominio;

namespace Patinhas.Api.Dados;

public static class Seed
{
    // Senha das contas de teste (fictícias). A do admin vem de Seed__AdminSenha.
    private const string SenhaClientesTeste = "Patinhas@123";

    public static async Task ExecutarAsync(PatinhasDbContext db, IConfiguration config, ILogger logger)
    {
        var senhaAdmin = config["Seed:AdminSenha"];
        if (string.IsNullOrWhiteSpace(senhaAdmin))
            throw new InvalidOperationException(
                "A senha do admin do seed não foi configurada. Defina a variável de ambiente Seed__AdminSenha.");

        await GarantirUsuarioAsync(db, logger, "admin@patinhas.com.br", senhaAdmin, Papeis.Admin);
        await GarantirUsuarioAsync(db, logger, "marina.souza@exemplo.com", SenhaClientesTeste, Papeis.Cliente);
        await GarantirUsuarioAsync(db, logger, "carlos.lima@exemplo.com", SenhaClientesTeste, Papeis.Cliente);

        if (!await db.Servicos.AnyAsync())
        {
            db.Servicos.AddRange(
                new Servico("Banho porte pequeno", "Banho com shampoo neutro, secagem e perfume. Para cães de até 8 kg.", 50m, 45),
                new Servico("Banho porte médio", "Banho com shampoo neutro, secagem e perfume. Para cães de 9 a 20 kg.", 70m, 60),
                new Servico("Banho porte grande", "Banho com shampoo neutro, secagem e perfume. Para cães acima de 20 kg.", 95m, 75),
                new Servico("Tosa higiênica", "Aparação das regiões íntimas, barriga e patas para manter a higiene.", 45m, 30),
                new Servico("Tosa completa", "Tosa na máquina ou tesoura, conforme a raça, com acabamento e laço.", 110m, 90),
                new Servico("Hidratação de pelos", "Máscara de hidratação profunda para pelos ressecados ou opacos.", 40m, 30),
                new Servico("Corte de unhas", "Corte e lixamento das unhas com segurança e sem estresse.", 25m, 15),
                new Servico("Consulta veterinária", "Avaliação clínica geral com médico-veterinário, com orientação de tratamento.", 150m, 30),
                new Servico("Vacinação", "Aplicação de vacina com carteirinha atualizada (vacina cobrada à parte).", 90m, 20),
                new Servico("Hotelzinho (diária)", "Hospedagem por 24 horas com passeios, alimentação e acompanhamento.", 85m, 1440));

            logger.LogInformation("Seed: {Quantidade} serviços de exemplo criados", 10);
        }

        await db.SaveChangesAsync();

        await GarantirAgendamentosAsync(db, logger);
    }

    private static async Task GarantirAgendamentosAsync(PatinhasDbContext db, ILogger logger)
    {
        if (await db.Agendamentos.AnyAsync())
            return;

        var marina = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "marina.souza@exemplo.com");
        var carlos = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "carlos.lima@exemplo.com");
        var servicos = await db.Servicos.OrderBy(s => s.Id).Take(2).ToListAsync();

        // Se alguém apagou os dados de exemplo, não há o que semear (e a API não pode cair por isso).
        if (marina is null || carlos is null || servicos.Count < 2)
            return;

        // Um agendamento por cliente, daqui a uma semana (horários em UTC).
        var base7Dias = DateTime.UtcNow.Date.AddDays(7);
        db.Agendamentos.Add(new Agendamento(servicos[0].Id, marina.Id, "Thor", base7Dias.AddHours(13)));
        db.Agendamentos.Add(new Agendamento(servicos[1].Id, carlos.Id, "Mel", base7Dias.AddHours(14)));
        await db.SaveChangesAsync();

        logger.LogInformation("Seed: {Quantidade} agendamentos de exemplo criados", 2);
    }

    private static async Task GarantirUsuarioAsync(
        PatinhasDbContext db, ILogger logger, string email, string senha, string papel)
    {
        var emailNormalizado = email.ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Email == emailNormalizado))
            return;

        db.Usuarios.Add(new Usuario(emailNormalizado, BCrypt.Net.BCrypt.HashPassword(senha), papel));
        logger.LogInformation("Seed: usuário {Email} criado com papel {Papel}", emailNormalizado, papel);
    }
}
