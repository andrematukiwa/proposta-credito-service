using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PropostaCredito.Application.Common.Abstractions;
using PropostaCredito.Application.Common.Exceptions;
using PropostaCredito.Application.Contracts;
using PropostaCredito.Application.Propostas.Repositories;
using PropostaCredito.Application.Propostas.Services;
using PropostaCredito.Domain.Propostas;
using PropostaCredito.Domain.Score;

namespace PropostaCredito.UnitTests.Application.Propostas.Services;

public class PropostaServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 29, 15, 0, 0, TimeSpan.Zero);

    private readonly IPropostaRepository _repository = Substitute.For<IPropostaRepository>();
    private readonly IScoreCalculator _scoreCalculator = Substitute.For<IScoreCalculator>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PropostaService _service;

    // xUnit cria uma instância nova da classe por teste: os mocks nunca vazam entre testes.
    // O score é mockado: a regra de cálculo já tem testes próprios (RendaIdadeScoreCalculatorTests),
    // aqui interessa o que o serviço faz com o resultado.
    public PropostaServiceTests()
    {
        _service = new PropostaService(
            _repository,
            _scoreCalculator,
            _eventPublisher,
            _unitOfWork,
            new FakeTimeProvider(Agora),
            NullLogger<PropostaService>.Instance);
    }

    private static ClienteCadastrado ClienteValido() => new(
        ClienteId: Guid.NewGuid(),
        Nome: "Maria Silva",
        DataNascimento: new DateOnly(1990, 5, 10),
        RendaMensal: 5_000m,
        CriadoEm: Agora.UtcDateTime);

    // Mensagens que nenhuma retentativa corrige: cada uma altera um único campo de um evento válido
    public static TheoryData<string, ClienteCadastrado> MensagensInvalidas => new()
    {
        { "sem ClienteId", ClienteValido() with { ClienteId = Guid.Empty } },
        { "sem nome", ClienteValido() with { Nome = "  " } },
        { "nome acima de 150 caracteres", ClienteValido() with { Nome = new string('a', 151) } },
        { "renda zerada", ClienteValido() with { RendaMensal = 0m } },
        { "renda negativa", ClienteValido() with { RendaMensal = -1m } },
        { "sem data de nascimento", ClienteValido() with { DataNascimento = default } },
    };

    [Theory]
    [InlineData(100, "Negada", 0, 0)]
    [InlineData(500, "Aprovada", 1, 1000)]
    [InlineData(800, "Aprovada", 2, 5000)]
    public async Task GerarAsync_DevePublicarPropostaGeradaComADecisaoDaFaixa(
        int score, string statusEsperado, int cartoesEsperados, int limiteEsperado)
    {
        // Arrange
        var cliente = ClienteValido();
        _scoreCalculator.Calcular(Arg.Any<DadosScore>()).Returns(score);

        // Act
        await _service.GerarAsync(cliente);

        // Assert: negadas também são publicadas; quem decide se há cartão é o consumidor
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<PropostaGerada>(e =>
                e.ClienteId == cliente.ClienteId
                && e.NomeCliente == cliente.Nome
                && e.Score == score
                && e.Status == statusEsperado
                && e.QuantidadeCartoes == cartoesEsperados
                && e.LimitePorCartao == limiteEsperado),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GerarAsync_DeveSalvarPropostaEEventoNaMesmaTransacao()
    {
        // Arrange
        var cliente = ClienteValido();
        _scoreCalculator.Calcular(Arg.Any<DadosScore>()).Returns(800);
        Proposta? propostaAdicionada = null;
        _repository.Adicionar(Arg.Do<Proposta>(p => propostaAdicionada = p));

        // Act
        await _service.GerarAsync(cliente);

        // Assert: o evento carrega o Id da proposta gravada, e só um SaveChanges confirma os dois (Outbox)
        Assert.NotNull(propostaAdicionada);
        Assert.Equal(cliente.ClienteId, propostaAdicionada.ClienteId);
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<PropostaGerada>(e => e.PropostaId == propostaAdicionada.Id),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GerarAsync_DeveCalcularScoreComRendaENascimentoDoClienteNaDataDeHoje()
    {
        // Arrange
        var cliente = ClienteValido();

        // Act
        await _service.GerarAsync(cliente);

        // Assert: a data de referência vem do TimeProvider, não do relógio da máquina
        _scoreCalculator.Received(1).Calcular(
            new DadosScore(cliente.RendaMensal, cliente.DataNascimento, new DateOnly(2026, 10, 29)));
    }

    [Fact]
    public async Task GerarAsync_NaoDeveGerarSegundaProposta_QuandoClienteJaTiverProposta()
    {
        // Arrange: o mesmo evento republicado depois de já ter sido processado
        var cliente = ClienteValido();
        _repository.ExistePorClienteIdAsync(cliente.ClienteId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _service.GerarAsync(cliente);

        // Assert
        _scoreCalculator.DidNotReceive().Calcular(Arg.Any<DadosScore>());
        _repository.DidNotReceive().Adicionar(Arg.Any<Proposta>());
        await _eventPublisher.DidNotReceive().PublishAsync(Arg.Any<PropostaGerada>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(MensagensInvalidas))]
    public async Task GerarAsync_DeveLancarMensagemInvalida_QuandoEventoTiverDadosInvalidos(string cenario, ClienteCadastrado cliente)
    {
        // Arrange: "cenario" só nomeia o caso no relatório de testes
        _ = cenario;

        // Act
        var acao = () => _service.GerarAsync(cliente);

        // Assert: a exceção faz a mensagem ir direto para a DLQ, sem retry, sem tocar no banco e sem publicar
        await Assert.ThrowsAsync<MensagemInvalidaException>(acao);
        await _repository.DidNotReceive().ExistePorClienteIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _eventPublisher.DidNotReceive().PublishAsync(Arg.Any<PropostaGerada>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObterPorClienteIdAsync_DeveDevolverPropostaComLimiteTotal()
    {
        // Arrange: faixa alta, 2 cartões de R$ 5.000,00
        var clienteId = Guid.NewGuid();
        var proposta = Proposta.Gerar(clienteId, "Maria Silva", score: 800, Agora.UtcDateTime);
        _repository.ObterPorClienteIdAsync(clienteId, Arg.Any<CancellationToken>()).Returns(proposta);

        // Act
        var resposta = await _service.ObterPorClienteIdAsync(clienteId);

        // Assert
        Assert.Equal(proposta.Id, resposta.Id);
        Assert.Equal("Aprovada", resposta.Status);
        Assert.Equal(2, resposta.QuantidadeCartoes);
        Assert.Equal(10_000m, resposta.LimiteTotal);
    }

    [Fact]
    public async Task ObterPorClienteIdAsync_DeveLancarNotFound_QuandoPropostaNaoExistir()
    {
        // Arrange: sem configuração, o mock devolve null (proposta ainda não gerada)
        var clienteId = Guid.NewGuid();

        // Act
        var acao = () => _service.ObterPorClienteIdAsync(clienteId);

        // Assert
        var excecao = await Assert.ThrowsAsync<NotFoundException>(acao);
        Assert.Contains("em processamento", excecao.Message, StringComparison.Ordinal);
    }
}
