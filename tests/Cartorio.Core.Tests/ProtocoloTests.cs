using Cartorio.Core.Dominio;

namespace Cartorio.Core.Tests;

public class ProtocoloTests
{
    private static readonly DateTime Quarta = new(2026, 10, 7, 9, 30, 0);
    private static readonly CalendarioUtil Cal = new();
    private static readonly RegrasPrazo Regras = new();

    internal static Protocolo Novo(NaturezaTitulo natureza = NaturezaTitulo.CompraEVenda, string? matricula = "12345") =>
        Protocolo.Prenotar(new Apresentante("  Maria Silva ", "529.982.247-25", ""), natureza, matricula, null, Quarta, "ana", Cal, Regras);

    [Fact]
    public void PrenotacaoCalculaPrazosEmDiasUteis()
    {
        var p = Novo();
        Assert.Equal(StatusProtocolo.Prenotado, p.Status);
        Assert.Equal(new DateOnly(2026, 10, 22), p.PrazoRegistro);         // 10 dias úteis (12/10 é feriado)
        Assert.Equal(new DateOnly(2026, 11, 6), p.VencimentoPrenotacao);   // 20 dias úteis (2/11 também)
        Assert.Equal("Maria Silva", p.Apresentante.Nome);
        Assert.Equal("52998224725", p.Apresentante.Documento);
        Assert.Null(p.Apresentante.Telefone);
        Assert.Equal("12.345", p.Matricula);
        Assert.Single(p.Eventos);
    }

    [Fact]
    public void ValidacaoPorCampo()
    {
        var erros = Protocolo.ValidarNovo(new Apresentante("Jo", "123", null), "12a");
        Assert.Equal(new[] { "Documento", "Matricula", "Nome" }, erros.Keys.Order().ToArray());
        Assert.Throws<DominioException>(() => Protocolo.Prenotar(new Apresentante("Jo", "123", null), NaturezaTitulo.Outros, null, null, Quarta, "ana", Cal, Regras));
    }

    [Fact]
    public void FluxoComExigenciaEReapresentacao()
    {
        var p = Novo();
        p.IniciarAnalise(Quarta.AddDays(1), "ana");
        p.EmitirExigencias(["Apresentar certidão de ônus reais.", "  ", "Comprovar pagamento do ITBI."], Quarta.AddDays(1), "ana");
        Assert.Equal(StatusProtocolo.ComExigencia, p.Status);
        Assert.Equal(2, p.Exigencias.Count);
        Assert.All(p.Exigencias, e => Assert.True(e.Pendente));

        p.Reapresentar(Quarta.AddDays(5), "ana");
        Assert.Equal(StatusProtocolo.EmAnalise, p.Status);
        Assert.All(p.Exigencias, e => Assert.False(e.Pendente));

        p.Registrar("r 3", null, Quarta.AddDays(6), "ana");
        Assert.Equal(StatusProtocolo.Registrado, p.Status);
        Assert.Equal("R-3/12.345", p.Ato);
        Assert.Equal(5, p.Eventos.Count);
        Assert.Equal(new AcoesPermitidas(false, false, false, false, false), p.Acoes(DateOnly.FromDateTime(Quarta)));
    }

    [Fact]
    public void TransicoesInvalidas()
    {
        var p = Novo();
        Assert.Throws<DominioException>(() => p.Registrar("R-1", null, Quarta, "ana"));       // sem análise
        Assert.Throws<DominioException>(() => p.EmitirExigencias(["Falta documento X."], Quarta, "ana"));
        Assert.Throws<DominioException>(() => p.Reapresentar(Quarta, "ana"));
        p.IniciarAnalise(Quarta, "ana");
        Assert.Throws<DominioException>(() => p.IniciarAnalise(Quarta, "ana"));
        Assert.Throws<DominioException>(() => p.EmitirExigencias([], Quarta, "ana"));
        Assert.Throws<DominioException>(() => p.EmitirExigencias(["curta"], Quarta, "ana"));
        p.Cancelar("Desistência", Quarta, "ana");
        Assert.Throws<DominioException>(() => p.Cancelar("De novo!", Quarta, "ana"));
    }

    [Fact]
    public void ReapresentarDepoisDaVigenciaNaoPode()
    {
        var p = Novo();
        p.IniciarAnalise(Quarta, "ana");
        p.EmitirExigencias(["Apresentar certidão atualizada."], Quarta, "ana");
        var depois = p.VencimentoPrenotacao.AddDays(1).ToDateTime(new TimeOnly(10, 0));
        Assert.True(p.PrenotacaoVencida(DateOnly.FromDateTime(depois)));
        Assert.False(p.Acoes(DateOnly.FromDateTime(depois)).Reapresentar);
        var ex = Assert.Throws<DominioException>(() => p.Reapresentar(depois, "ana"));
        Assert.Contains("06/11/2026", ex.Message);
    }

    [Theory]
    [InlineData("R-3", "R-3/12.345")]
    [InlineData("r3", "R-3/12.345")]
    [InlineData("R-03/12345", "R-3/12.345")]
    [InlineData("R-3 / 12.345", "R-3/12.345")]
    [InlineData("AV-2", "AV-2/12.345")]
    [InlineData("R-3/999", null)] // matrícula diferente
    [InlineData("R-0", null)]
    [InlineData("X-1", null)]
    [InlineData("", null)]
    public void NormalizarAto(string entrada, string? esperado) => Assert.Equal(esperado, Protocolo.NormalizarAto(entrada, "12.345"));

    [Fact]
    public void AverbacaoGeraAtoAV()
    {
        var p = Novo(NaturezaTitulo.Averbacao);
        p.IniciarAnalise(Quarta, "ana");
        Assert.Throws<DominioException>(() => p.Registrar("R-2", null, Quarta, "ana"));
        p.Registrar("AV-2", null, Quarta, "ana");
        Assert.Equal("AV-2/12.345", p.Ato);
    }

    [Fact]
    public void RegistrarExigeMatricula()
    {
        var p = Novo(matricula: null);
        p.IniciarAnalise(Quarta, "ana");
        Assert.Throws<DominioException>(() => p.Registrar("R-1", null, Quarta, "ana"));
        p.Registrar("R-1", "7.777", Quarta, "ana");
        Assert.Equal("7.777", p.Matricula);
    }

    [Fact]
    public void SituacaoDoPrazo()
    {
        var p = Novo(); // registrar até 22/10
        Assert.Equal(SituacaoPrazo.NoPrazo, p.Situacao(new DateOnly(2026, 10, 7), Cal, Regras));
        Assert.Equal(SituacaoPrazo.Vencendo, p.Situacao(new DateOnly(2026, 10, 20), Cal, Regras)); // faltam 2 dias úteis
        Assert.Equal(SituacaoPrazo.Vencido, p.Situacao(new DateOnly(2026, 10, 23), Cal, Regras));

        p.IniciarAnalise(Quarta, "ana");
        p.EmitirExigencias(["Apresentar certidão atualizada."], Quarta, "ana");
        // com exigência vale a vigência da prenotação (06/11), não o prazo de registro
        Assert.Equal(SituacaoPrazo.NoPrazo, p.Situacao(new DateOnly(2026, 10, 23), Cal, Regras));
        p.Cancelar("Desistência", Quarta, "ana");
        Assert.Equal(SituacaoPrazo.Concluido, p.Situacao(new DateOnly(2026, 10, 23), Cal, Regras));
    }

    [Theory]
    [InlineData("12.345", "12.345")]
    [InlineData("12345", "12.345")]
    [InlineData("0777", "777")]
    [InlineData("1234567", "1.234.567")]
    [InlineData("12a", null)]
    [InlineData("000", null)]
    public void NormalizarMatricula(string entrada, string? esperado) => Assert.Equal(esperado, Protocolo.NormalizarMatricula(entrada));
}
