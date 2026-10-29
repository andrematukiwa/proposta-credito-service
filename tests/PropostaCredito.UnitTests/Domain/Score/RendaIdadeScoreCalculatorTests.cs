using System.Globalization;
using PropostaCredito.Domain.Score;

namespace PropostaCredito.UnitTests.Domain.Score;

public class RendaIdadeScoreCalculatorTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 29);

    private readonly RendaIdadeScoreCalculator _calculator = new();

    [Theory]
    [InlineData(1_000, "2006-01-01", 100)]  // renda baixa (50) + 20 anos (50)
    [InlineData(2_000, "1990-01-01", 500)]  // 1.500–2.999 (200) + 36 anos (300)
    [InlineData(5_000, "1990-01-01", 800)]  // 5.000–9.999 (500) + 36 anos (300)
    [InlineData(10_000, "1990-01-01", 1000)] // a partir de 10.000 (700) + 36 anos (300)
    public void Calcular_DeveSomarPontosDeRendaEIdade(double renda, string dataNascimento, int scoreEsperado)
    {
        // Arrange
        var dados = new DadosScore((decimal)renda, DateOnly.Parse(dataNascimento, CultureInfo.InvariantCulture), Hoje);

        // Act
        var score = _calculator.Calcular(dados);

        // Assert
        Assert.Equal(scoreEsperado, score);
    }
}
