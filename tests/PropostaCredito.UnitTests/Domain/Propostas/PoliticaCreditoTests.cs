using PropostaCredito.Domain.Propostas;

namespace PropostaCredito.UnitTests.Domain.Propostas;

public class PoliticaCreditoTests
{
    [Theory]
    [InlineData(0, StatusProposta.Negada, 0, 0)]
    [InlineData(100, StatusProposta.Negada, 0, 0)]
    [InlineData(101, StatusProposta.Aprovada, 1, 1000)]
    [InlineData(500, StatusProposta.Aprovada, 1, 1000)]
    [InlineData(501, StatusProposta.Aprovada, 2, 5000)]
    [InlineData(1000, StatusProposta.Aprovada, 2, 5000)]
    public void Avaliar_DeveAplicarAFaixaCorreta_QuandoScoreEstiverNaFronteira(
        int score, StatusProposta statusEsperado, int cartoesEsperados, int limiteEsperado)
    {
        // Act
        var decisao = PoliticaCredito.Avaliar(score);

        // Assert
        Assert.Equal(statusEsperado, decisao.Status);
        Assert.Equal(cartoesEsperados, decisao.QuantidadeCartoes);
        Assert.Equal(limiteEsperado, decisao.LimitePorCartao);
    }
}
