using System.Globalization;
using PropostaCredito.Domain.Propostas;

namespace PropostaCredito.UnitTests.Domain.Propostas;

public class PropostaTests
{
    private static readonly DateTime Agora = new(2026, 10, 29, 15, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(100, 0)]
    [InlineData(500, 1000)]
    [InlineData(800, 10000)]
    public void Gerar_DeveCalcularLimiteTotalPelaQuantidadeDeCartoes(int score, int limiteTotalEsperado)
    {
        // Act
        var proposta = Proposta.Gerar(Guid.NewGuid(), "Maria Silva", score, Agora);

        // Assert
        Assert.Equal(limiteTotalEsperado, proposta.LimiteTotal);
    }

    [Fact]
    public void Gerar_DeveGuardarDadosDoClienteEScore()
    {
        // Arrange
        var clienteId = Guid.NewGuid();

        // Act
        var proposta = Proposta.Gerar(clienteId, "  Maria Silva  ", score: 500, Agora);

        // Assert
        Assert.NotEqual(Guid.Empty, proposta.Id);
        Assert.Equal(clienteId, proposta.ClienteId);
        Assert.Equal("Maria Silva", proposta.NomeCliente);
        Assert.Equal(500, proposta.Score);
        Assert.Equal(StatusProposta.Aprovada, proposta.Status);
    }

    [Fact]
    public void Gerar_DeveGravarLimiteComDuasCasasDecimais()
    {
        // Act
        var proposta = Proposta.Gerar(Guid.NewGuid(), "Maria Silva", score: 500, Agora);

        // Assert: decimal compara só o valor (1000m == 1000.00m), por isso a escala é conferida pela string
        Assert.Equal("1000.00", proposta.LimitePorCartao.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Gerar_DeveTruncarDataDeCriacaoEmMicrossegundos()
    {
        // Arrange: 5 ticks = 500 nanossegundos, abaixo da precisão do Postgres
        var criadaEm = Agora.AddTicks(5);

        // Act
        var proposta = Proposta.Gerar(Guid.NewGuid(), "Maria Silva", score: 500, criadaEm);

        // Assert
        Assert.Equal(Agora, proposta.CriadaEm);
    }
}
