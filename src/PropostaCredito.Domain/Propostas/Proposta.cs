namespace PropostaCredito.Domain.Propostas;

public sealed class Proposta
{
    public const int NomeClienteTamanhoMaximo = 150;
    public const int LimitePrecisao = 18;
    public const int LimiteCasasDecimais = 2;

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public string NomeCliente { get; private set; } = string.Empty;
    public int Score { get; private set; }
    public StatusProposta Status { get; private set; }
    public int QuantidadeCartoes { get; private set; }
    public decimal LimitePorCartao { get; private set; }
    public DateTime CriadaEm { get; private set; }

    public decimal LimiteTotal => LimitePorCartao * QuantidadeCartoes;

    private Proposta() { } // usado pelo EF Core

    /// <summary>Gera a proposta aplicando a política de crédito ao score calculado.</summary>
    public static Proposta Gerar(Guid clienteId, string nomeCliente, int score, DateTime criadaEm)
    {
        var decisao = PoliticaCredito.Avaliar(score);

        return new Proposta
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            NomeCliente = nomeCliente.Trim(),
            Score = score,
            Status = decisao.Status,
            QuantidadeCartoes = decisao.QuantidadeCartoes,
            // Multiplicar por 1.00m fixa a escala em 2 casas, igual ao banco
            LimitePorCartao = decisao.LimitePorCartao * 1.00m,
            // O Postgres guarda microssegundos: truncar evita divergência entre o que é devolvido e o que é lido depois
            CriadaEm = TruncarParaMicrossegundos(criadaEm)
        };
    }

    private static DateTime TruncarParaMicrossegundos(DateTime data) =>
        new(data.Ticks - data.Ticks % (TimeSpan.TicksPerMillisecond / 1000), DateTimeKind.Utc);
}
