namespace PropostaCredito.Domain.Score;

/// <summary>
/// Score = pontos de renda + pontos de idade (máximo 1000).
/// <para>
/// O desafio define só as faixas de score, não como calculá-lo; a regra real de um banco é
/// proprietária. Esta é uma simplificação inspirada no crédito consignado, em que renda e
/// estabilidade (faixa etária economicamente ativa) são os principais fatores.
/// </para>
/// </summary>
public sealed class RendaIdadeScoreCalculator : IScoreCalculator
{
    public const int ScoreMaximo = 1000;

    public int Calcular(DadosScore dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var score = PontosPorRenda(dados.RendaMensal) + PontosPorIdade(Idade(dados));
        return Math.Clamp(score, 0, ScoreMaximo);
    }

    private static int PontosPorRenda(decimal renda) => renda switch
    {
        < 1_500m => 50,
        < 3_000m => 200,
        < 5_000m => 350,
        < 10_000m => 500,
        _ => 700
    };

    private static int PontosPorIdade(int idade) => idade switch
    {
        < 18 => 0, // menor de idade não deveria chegar aqui (o cadastro valida), mas não pontua
        <= 24 => 50,
        <= 60 => 300,
        _ => 150
    };

    private static int Idade(DadosScore dados)
    {
        var idade = dados.DataReferencia.Year - dados.DataNascimento.Year;

        // Ainda não fez aniversário no ano de referência
        if (dados.DataNascimento > dados.DataReferencia.AddYears(-idade))
            idade--;

        return idade;
    }
}
