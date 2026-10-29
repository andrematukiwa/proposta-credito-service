using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropostaCredito.Domain.Propostas;

namespace PropostaCredito.Infrastructure.Propostas;

internal sealed class PropostaConfiguration : IEntityTypeConfiguration<Proposta>
{
    public void Configure(EntityTypeBuilder<Proposta> builder)
    {
        builder.ToTable("propostas");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ClienteId).IsRequired();
        builder.Property(p => p.NomeCliente).HasMaxLength(Proposta.NomeClienteTamanhoMaximo).IsRequired();
        builder.Property(p => p.Score).IsRequired();

        // Gravado como texto ("Aprovada"/"Negada"): legível em consultas e imune a reordenação do enum
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(p => p.QuantidadeCartoes).IsRequired();
        builder.Property(p => p.LimitePorCartao)
            .HasPrecision(Proposta.LimitePrecisao, Proposta.LimiteCasasDecimais)
            .IsRequired();
        builder.Property(p => p.CriadaEm).IsRequired();

        builder.Ignore(p => p.LimiteTotal);

        // Uma proposta por cliente: garante a idempotência mesmo com processamento concorrente
        builder.HasIndex(p => p.ClienteId).IsUnique();
    }
}
