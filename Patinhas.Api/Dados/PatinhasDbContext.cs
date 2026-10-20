using Microsoft.EntityFrameworkCore;
using Patinhas.Api.Dominio;

namespace Patinhas.Api.Dados;

public class PatinhasDbContext : DbContext
{
    public PatinhasDbContext(DbContextOptions<PatinhasDbContext> opcoes) : base(opcoes) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.SenhaHash).IsRequired().HasMaxLength(100);
            e.Property(u => u.Papel).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<Servico>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Nome).IsRequired().HasMaxLength(100);
            e.Property(s => s.Descricao).HasMaxLength(500);
            e.Property(s => s.Preco).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Agendamento>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.NomeDoPet).IsRequired().HasMaxLength(100);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(a => new { a.ServicoId, a.DataHora });

            e.HasOne(a => a.Servico)
                .WithMany()
                .HasForeignKey(a => a.ServicoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
