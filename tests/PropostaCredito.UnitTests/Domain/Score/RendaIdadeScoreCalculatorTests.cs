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

    // Idade fixa em 36 anos (300 pontos): o score varia só pela renda.
    // Renda como string porque [InlineData] não aceita decimal.
    [Theory]
    [InlineData("1499.99", 350)]  // 50 + 300
    [InlineData("1500.00", 500)]  // 200 + 300
    [InlineData("2999.99", 500)]
    [InlineData("3000.00", 650)]  // 350 + 300
    [InlineData("4999.99", 650)]
    [InlineData("5000.00", 800)]  // 500 + 300
    [InlineData("9999.99", 800)]
    [InlineData("10000.00", 1000)] // 700 + 300
    public void Calcular_DeveMudarDeFaixa_QuandoRendaCruzarAFronteira(string renda, int scoreEsperado)
    {
        // Arrange
        var dados = new DadosScore(decimal.Parse(renda, CultureInfo.InvariantCulture), new DateOnly(1990, 1, 1), Hoje);

        // Act
        var score = _calculator.Calcular(dados);

        // Assert
        Assert.Equal(scoreEsperado, score);
    }

    // Renda fixa em R$ 1.000,00 (50 pontos): o score varia só pela idade.
    // Cada par é "faz X anos amanhã" e "faz X anos hoje", que é onde o cálculo de aniversário erra.
    [Theory]
    [InlineData("2008-10-30", 50)]   // 17 anos: 50 + 0
    [InlineData("2008-10-29", 100)]  // 18 hoje: 50 + 50
    [InlineData("2001-10-30", 100)]  // 24 anos
    [InlineData("2001-10-29", 350)]  // 25 hoje: 50 + 300
    [InlineData("1965-10-30", 350)]  // 60 anos
    [InlineData("1965-10-29", 200)]  // 61 hoje: 50 + 150
    public void Calcular_DeveMudarDeFaixa_QuandoIdadeCruzarAFronteira(string dataNascimento, int scoreEsperado)
    {
        // Arrange
        var dados = new DadosScore(1_000m, DateOnly.Parse(dataNascimento, CultureInfo.InvariantCulture), Hoje);

        // Act
        var score = _calculator.Calcular(dados);

        // Assert
        Assert.Equal(scoreEsperado, score);
    }
}
