namespace PropostaCredito.Domain.Propostas;

/// <summary>
/// Tabela de concessão do desafio: transforma o score em decisão de crédito.
/// <list type="table">
///   <item>0 – 100: negada, 0 cartões</item>
///   <item>101 – 500: aprovada, 1 cartão de R$ 1.000,00</item>
///   <item>501 – 1000: aprovada, 2 cartões de R$ 5.000,00 cada</item>
/// </list>
/// Na última faixa o desafio fala em "até 2" cartões; a interpretação adotada é emitir
/// o máximo da faixa (2), já que nenhum critério adicional foi definido.
/// </summary>
public static class PoliticaCredito
{
    public const int ScoreMaximoNegado = 100;
    public const int ScoreMaximoFaixaIntermediaria = 500;

    public const decimal LimiteFaixaIntermediaria = 1_000m;
    public const decimal LimiteFaixaAlta = 5_000m;

    public static DecisaoCredito Avaliar(int score) => score switch
    {
        <= ScoreMaximoNegado => new(StatusProposta.Negada, QuantidadeCartoes: 0, LimitePorCartao: 0m),
        <= ScoreMaximoFaixaIntermediaria => new(StatusProposta.Aprovada, QuantidadeCartoes: 1, LimiteFaixaIntermediaria),
        _ => new(StatusProposta.Aprovada, QuantidadeCartoes: 2, LimiteFaixaAlta)
    };
}

public sealed record DecisaoCredito(StatusProposta Status, int QuantidadeCartoes, decimal LimitePorCartao);
