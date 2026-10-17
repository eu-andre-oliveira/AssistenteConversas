using AssistenteConversas.Models;
using Microsoft.EntityFrameworkCore;

namespace AssistenteConversas.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadInteracao> Interacoes => Set<LeadInteracao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasKey(lead => lead.Id);
            entity.Property(lead => lead.Nome).HasMaxLength(200).IsRequired();
            entity.Property(lead => lead.Temperatura).HasMaxLength(30).IsRequired();
            entity.Property(lead => lead.Status).HasMaxLength(50).IsRequired();
            entity.Property(lead => lead.ConversaOriginal).IsRequired();
            entity.HasIndex(lead => lead.Nome);
            entity.HasIndex(lead => lead.Empresa);
            entity.HasIndex(lead => lead.Temperatura);
            entity.HasIndex(lead => lead.Status);
            entity.HasIndex(lead => lead.ProximoFollowUp);
        });

        modelBuilder.Entity<LeadInteracao>(entity =>
        {
            entity.HasKey(interacao => interacao.Id);
            entity.Property(interacao => interacao.Texto).HasMaxLength(4000).IsRequired();
            entity.HasIndex(interacao => interacao.LeadId);
            entity.HasIndex(interacao => interacao.DataRegistro);
        });
    }
}
