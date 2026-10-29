namespace PropostaCredito.Domain.Score;

/// <summary>
/// Calcula o score de crédito (0 a 1000). É o ponto de extensão do serviço: trocar a regra
/// (ex.: consultar um bureau externo) é criar outra implementação, sem mexer no resto.
/// </summary>
public interface IScoreCalculator
{
    int Calcular(DadosScore dados);
}

/// <param name="RendaMensal">Renda mensal informada no cadastro.</param>
/// <param name="DataNascimento">Usada para calcular a idade na data de referência.</param>
/// <param name="DataReferencia">"Hoje" do cálculo, injetado para os testes não dependerem do relógio.</param>
public sealed record DadosScore(decimal RendaMensal, DateOnly DataNascimento, DateOnly DataReferencia);
