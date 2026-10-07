using Cartorio.Core.Apresentacao;
using Cartorio.Core.Dominio;
using Cartorio.Core.Infra;
using Cartorio.Core.Relatorios;
using Cartorio.Core.Servicos;

namespace Cartorio.Core.Tests;

public class ProtocoloServiceTests
{
    internal static (ProtocoloService, RelogioManual, RepositorioEmMemoria) Criar()
    {
        var repo = new RepositorioEmMemoria();
        var relogio = new RelogioManual(new DateTime(2026, 10, 7, 9, 0, 0));
        return (new ProtocoloService(repo, relogio, "ana"), relogio, repo);
    }

    internal static Apresentante Maria => new("Maria Silva", "52998224725", null);

    [Fact]
    public void NumeracaoSequencial()
    {
        var (s, _, _) = Criar();
        var numeros = Enumerable.Range(0, 3).Select(_ => s.Prenotar(Maria, NaturezaTitulo.Doacao, null, null).Numero).ToArray();
        Assert.Equal(new[] { 1, 2, 3 }, numeros);
    }

    [Fact]
    public void RotinaCancelaPrenotacoesVencidas()
    {
        var (s, relogio, _) = Criar();
        var vencido = s.Prenotar(Maria, NaturezaTitulo.Doacao, null, null);
        s.IniciarAnalise(vencido.Id);
        s.EmitirExigencias(vencido.Id, ["Apresentar certidão atualizada."]);
        var emDia = s.Prenotar(Maria, NaturezaTitulo.Doacao, null, null);

        relogio.Agora = new DateTime(2026, 11, 9, 8, 0, 0); // depois de 06/11
        Assert.Equal(new[] { vencido.Numero }, s.CancelarPrenotacoesVencidas().ToArray());
        Assert.Equal(StatusProtocolo.Cancelado, s.Obter(vencido.Id).Status);
        Assert.Equal("sistema", s.Obter(vencido.Id).Eventos[^1].Usuario);
        Assert.Equal(StatusProtocolo.Prenotado, s.Obter(emDia.Id).Status);
        Assert.Empty(s.CancelarPrenotacoesVencidas());
    }

    [Fact]
    public void FeriadoLocalDoRepositorioEntraNoPrazo()
    {
        var repo = new RepositorioEmMemoria([new DateOnly(2026, 10, 8)]);
        var s = new ProtocoloService(repo, new RelogioManual(new DateTime(2026, 10, 7, 9, 0, 0)), "ana");
        Assert.Equal(new DateOnly(2026, 10, 23), s.Prenotar(Maria, NaturezaTitulo.Doacao, null, null).PrazoRegistro);
    }

    [Fact]
    public void BuscaPorNumeroNomeDocumentoEMatricula()
    {
        var (s, _, _) = Criar();
        s.Prenotar(Maria, NaturezaTitulo.Doacao, "12.345", null);
        s.Prenotar(new("José Santos", "11.222.333/0001-81", null), NaturezaTitulo.Doacao, "999", null);
        int Total(string t) => s.Buscar(new FiltroProtocolos(Texto: t)).Total;
        Assert.Equal(1, Total("2"));
        Assert.Equal(1, Total("maria"));
        Assert.Equal(1, Total("529.982"));
        Assert.Equal(1, Total("12345"));
        Assert.Equal(1, Total("11.222.333/0001-81"));
        Assert.Equal(2, s.Buscar(new FiltroProtocolos()).Total);
    }

    [Fact]
    public void DadosDeDemonstracaoSaoCoerentes()
    {
        var repo = new RepositorioEmMemoria();
        var agora = new DateTime(2026, 10, 7, 15, 0, 0);
        DadosDemonstracao.Popular(repo, agora);
        var todos = repo.Buscar(new FiltroProtocolos()).Itens.OrderBy(p => p.Numero).ToList();
        Assert.Equal(7, todos.Count);
        Assert.True(todos.Zip(todos.Skip(1)).All(par => par.First.PrenotadoEm < par.Second.PrenotadoEm), "numeração cronológica");
        Assert.True(todos[0].PrenotacaoVencida(DateOnly.FromDateTime(agora)));
        Assert.Equal(new[] { 1, 1, 2, 2, 1 }, repo.ContarPorStatus().OrderBy(k => k.Key).Select(k => k.Value).ToArray());
    }
}

public class RelatoriosTests
{
    [Fact]
    public void CsvComBomSeparadorEProtecaoContraFormula()
    {
        var (s, _, _) = ProtocoloServiceTests.Criar();
        s.Prenotar(new("=HYPERLINK(\"x\"); Silva", "52998224725", null), NaturezaTitulo.Doacao, "1", null);
        var bytes = ExportadorCsv.GerarBytes(s.Buscar(new FiltroProtocolos()).Itens);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var linhas = System.Text.Encoding.UTF8.GetString(bytes[3..]).Split(Environment.NewLine);
        Assert.StartsWith("Número;Data;Apresentante", linhas[0]);
        Assert.Contains("\"'=HYPERLINK(\"\"x\"\"); Silva\"", linhas[1]);
        Assert.Contains("529.982.247-25", linhas[1]);
    }

    [Fact]
    public void ReciboTemNumeroPrazosEDocumentoFormatado()
    {
        var (s, _, _) = ProtocoloServiceTests.Criar();
        var p = s.Prenotar(new("Empresa X", "11222333000181", "(42) 3333-0000"), NaturezaTitulo.CompraEVenda, null, null);
        var texto = string.Join("\n", ReciboProtocolo.Montar(p, "Registro de Imóveis Exemplo").Select(l => l.Texto));
        Assert.Contains("REGISTRO DE IMÓVEIS EXEMPLO", texto);
        Assert.Contains("Protocolo nº 1", texto);
        Assert.Contains("CNPJ: 11.222.333/0001-81", texto);
        Assert.Contains("Prazo para registro: 22/10/2026", texto);
        Assert.Contains("a definir na qualificação", texto);
    }
}

// ---------------- presenters com views falsas ----------------

internal sealed class ListaFalsa : IListaProtocolosView
{
    public string TextoBusca { get; set; } = "";
    public StatusProtocolo? StatusSelecionado { get; set; }
    public bool SoEmAndamento { get; set; }
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    public IReadOnlyList<LinhaProtocolo> Linhas = [];
    public string Rodape = "", Resumo = "";
    public bool TemAnterior, TemProxima;
    public List<string> Avisos = [], Erros = [];

    public void MostrarProtocolos(IReadOnlyList<LinhaProtocolo> linhas, string rodape, bool temAnterior, bool temProxima)
    { Linhas = linhas; Rodape = rodape; TemAnterior = temAnterior; TemProxima = temProxima; }
    public void MostrarResumo(string resumo) => Resumo = resumo;
    public void MostrarAviso(string mensagem) => Avisos.Add(mensagem);
    public void MostrarErro(string mensagem) => Erros.Add(mensagem);
}

public class ListaPresenterTests
{
    [Fact]
    public void IniciarAvisaCancelamentosEPagina()
    {
        var repo = new RepositorioEmMemoria();
        DadosDemonstracao.Popular(repo, new DateTime(2026, 10, 7, 15, 0, 0));
        var s = new ProtocoloService(repo, new RelogioManual(new DateTime(2026, 10, 7, 15, 0, 0)), "ana");
        var v = new ListaFalsa();
        var presenter = new ListaProtocolosPresenter(v, s, tamanhoPagina: 3);

        presenter.Iniciar();
        Assert.Contains("1", Assert.Single(v.Avisos));
        Assert.Equal(3, v.Linhas.Count);
        Assert.Equal(7, v.Linhas[0].Numero); // mais recente primeiro
        Assert.Equal("7 protocolo(s) — página 1 de 3", v.Rodape);
        Assert.False(v.TemAnterior);
        Assert.True(v.TemProxima);
        Assert.Contains("Cancelados: 2", v.Resumo);

        presenter.ProximaPagina();
        presenter.ProximaPagina();
        Assert.Single(v.Linhas);
        Assert.False(v.TemProxima);

        v.SoEmAndamento = true;
        presenter.Buscar();
        Assert.Equal("3 protocolo(s) — página 1 de 1", v.Rodape);
        Assert.Contains(v.Linhas, l => l.Situacao == SituacaoPrazo.Vencendo);
    }

    [Fact]
    public void DataFinalAntesDaInicialMostraErro()
    {
        var (s, _, _) = ProtocoloServiceTests.Criar();
        var v = new ListaFalsa { De = new DateOnly(2026, 10, 7), Ate = new DateOnly(2026, 10, 1) };
        new ListaProtocolosPresenter(v, s).Buscar();
        Assert.Single(v.Erros);
    }
}

internal sealed class NovoFalso : INovoProtocoloView
{
    public string Nome { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Telefone { get; set; } = "";
    public NaturezaTitulo? Natureza { get; set; }
    public string Matricula { get; set; } = "";
    public string Observacoes { get; set; } = "";
    public IReadOnlyDictionary<string, string> Erros = new Dictionary<string, string>();
    public Protocolo? Criado;

    public void LimparErros() => Erros = new Dictionary<string, string>();
    public void MostrarErros(IReadOnlyDictionary<string, string> erros) => Erros = erros;
    public void Concluir(Protocolo criado) => Criado = criado;
    public void MostrarErro(string mensagem) => throw new InvalidOperationException(mensagem);
}

public class NovoPresenterTests
{
    [Fact]
    public void MostraErrosPorCampoENaoGrava()
    {
        var (s, _, repo) = ProtocoloServiceTests.Criar();
        var v = new NovoFalso { Nome = "Jo", Documento = "123" };
        new NovoProtocoloPresenter(v, s).Salvar();
        Assert.Equal(new[] { "Documento", "Natureza", "Nome" }, v.Erros.Keys.Order().ToArray());
        Assert.Null(v.Criado);
        Assert.Equal(0, repo.Buscar(new FiltroProtocolos()).Total);
    }

    [Fact]
    public void FormataDocumentoESalva()
    {
        var (s, _, _) = ProtocoloServiceTests.Criar();
        var v = new NovoFalso { Nome = "Maria Silva", Documento = "52998224725", Natureza = NaturezaTitulo.Doacao, Matricula = "12345" };
        var presenter = new NovoProtocoloPresenter(v, s);
        presenter.FormatarDocumento();
        Assert.Equal("529.982.247-25", v.Documento);
        presenter.Salvar();
        Assert.Equal(1, v.Criado!.Numero);
        Assert.Empty(v.Erros);
    }
}

internal sealed class DetalheFalso : IDetalheProtocoloView
{
    public DetalheProtocolo? Atual;
    public Queue<string?> Respostas = new();
    public bool Confirma = true;
    public List<string> Erros = [];
    public IReadOnlyList<LinhaRecibo>? Recibo;

    public void Exibir(DetalheProtocolo detalhe) => Atual = detalhe;
    public string? PedirTexto(string titulo, string instrucao, bool multilinha, string valorInicial = "") => Respostas.Dequeue();
    public bool Confirmar(string mensagem) => Confirma;
    public void MostrarErro(string mensagem) => Erros.Add(mensagem);
    public void ImprimirRecibo(IReadOnlyList<LinhaRecibo> linhas) => Recibo = linhas;
}

public class DetalhePresenterTests
{
    private static (DetalheProtocoloPresenter, DetalheFalso, ProtocoloService, int) Criar(string? matricula = "12.345")
    {
        var (s, _, _) = ProtocoloServiceTests.Criar();
        var p = s.Prenotar(ProtocoloServiceTests.Maria, NaturezaTitulo.CompraEVenda, matricula, null);
        var v = new DetalheFalso();
        var presenter = new DetalheProtocoloPresenter(v, s, p.Id, "Cartório Exemplo");
        presenter.Carregar();
        return (presenter, v, s, p.Id);
    }

    [Fact]
    public void FluxoPelaTela()
    {
        var (presenter, v, s, id) = Criar();
        var alteracoes = 0;
        presenter.Alterado += () => alteracoes++;
        Assert.True(v.Atual!.Acoes.IniciarAnalise);
        Assert.False(v.Atual.Acoes.Registrar);

        presenter.IniciarAnalise();
        v.Respostas.Enqueue("Apresentar certidão de ônus reais.\r\n\r\nComprovar pagamento do ITBI.");
        presenter.EmitirExigencias();
        Assert.Equal(2, v.Atual.Exigencias.Count);
        Assert.StartsWith("[pendente]", v.Atual.Exigencias[0]);
        Assert.Contains("restante", v.Atual.Campos.Single(c => c.Campo == "Vigência da prenotação").Valor);

        presenter.Reapresentar();
        v.Respostas.Enqueue("R-9");
        presenter.Registrar();
        Assert.Equal("Registrado", v.Atual.Status);
        Assert.Equal("R-9/12.345", v.Atual.Campos.Single(c => c.Campo == "Ato").Valor);
        Assert.Equal(4, alteracoes);
        Assert.Empty(v.Erros);
        Assert.Equal(StatusProtocolo.Registrado, s.Obter(id).Status);
    }

    [Fact]
    public void CancelarNaCaixaDeDialogoNaoMudaNada()
    {
        var (presenter, v, s, id) = Criar();
        v.Respostas.Enqueue(null);
        presenter.Cancelar();
        Assert.Equal(StatusProtocolo.Prenotado, s.Obter(id).Status);

        v.Confirma = false;
        presenter.IniciarAnalise();
        v.Respostas.Enqueue("Falta a certidão de casamento atualizada.");
        presenter.EmitirExigencias();
        presenter.Reapresentar(); // usuário respondeu "não"
        Assert.Equal(StatusProtocolo.ComExigencia, s.Obter(id).Status);
    }

    [Fact]
    public void ErroDeRegraApareceNaTela()
    {
        var (presenter, v, _, _) = Criar(matricula: null);
        presenter.IniciarAnalise();
        v.Respostas.Enqueue("5.555");
        v.Respostas.Enqueue("AV-1"); // compra e venda gera R, não AV
        presenter.Registrar();
        Assert.Contains("R-n", Assert.Single(v.Erros));
        Assert.Equal("Em análise", v.Atual!.Status);
    }

    [Fact]
    public void Imprimir()
    {
        var (presenter, v, _, _) = Criar();
        presenter.Imprimir();
        Assert.Contains(v.Recibo!, l => l.Texto == "Protocolo nº 1" && l.Destaque);
    }
}
