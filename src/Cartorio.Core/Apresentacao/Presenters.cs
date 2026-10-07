using Cartorio.Core.Dominio;
using Cartorio.Core.Relatorios;
using Cartorio.Core.Servicos;

namespace Cartorio.Core.Apresentacao;

// MVP (Model-View-Presenter): os formulários só repassam eventos e desenham o que o presenter manda.
// Toda decisão fica aqui, e os testes usam views falsas, sem abrir janela.

// ============================ Lista ============================

public sealed record LinhaProtocolo(
    int Id, int Numero, string Data, string Apresentante, string Natureza, string Matricula,
    string Status, string Prazo, SituacaoPrazo Situacao);

public interface IListaProtocolosView
{
    string TextoBusca { get; }
    StatusProtocolo? StatusSelecionado { get; }
    bool SoEmAndamento { get; }
    DateOnly? De { get; }
    DateOnly? Ate { get; }

    void MostrarProtocolos(IReadOnlyList<LinhaProtocolo> linhas, string rodape, bool temAnterior, bool temProxima);
    void MostrarResumo(string resumo);
    void MostrarAviso(string mensagem);
    void MostrarErro(string mensagem);
}

public sealed class ListaProtocolosPresenter(IListaProtocolosView view, ProtocoloService servico, int tamanhoPagina = 50)
{
    private int _pagina = 1;

    /// <summary>Abertura da tela: roda a rotina de vencimento e carrega a primeira página.</summary>
    public void Iniciar()
    {
        var cancelados = servico.CancelarPrenotacoesVencidas();
        if (cancelados.Count > 0)
            view.MostrarAviso($"Prenotação vencida, protocolo(s) cancelado(s) automaticamente: {string.Join(", ", cancelados)}.");
        Buscar();
    }

    /// <summary>Filtro mudou: volta para a página 1.</summary>
    public void Buscar()
    {
        _pagina = 1;
        Carregar();
    }

    public void ProximaPagina() { _pagina++; Carregar(); }

    public void PaginaAnterior() { if (_pagina > 1) { _pagina--; Carregar(); } }

    public void Recarregar() => Carregar();

    /// <summary>Todos os resultados do filtro (não só a página) em CSV.</summary>
    public byte[] ExportarCsv()
    {
        var todos = servico.Buscar(Filtro() with { Pagina = 1, TamanhoPagina = int.MaxValue }).Itens;
        return ExportadorCsv.GerarBytes(todos);
    }

    private FiltroProtocolos Filtro()
    {
        if (view.De is { } de && view.Ate is { } ate && ate < de)
            throw new DominioException("A data final é anterior à inicial.");
        return new FiltroProtocolos(view.TextoBusca, view.StatusSelecionado, view.De, view.Ate, view.SoEmAndamento, _pagina, tamanhoPagina);
    }

    private void Carregar()
    {
        try
        {
            var pagina = servico.Buscar(Filtro());
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(pagina.Total / (double)tamanhoPagina));
            if (_pagina > totalPaginas) { _pagina = totalPaginas; pagina = servico.Buscar(Filtro()); }

            var hoje = servico.Hoje;
            var linhas = pagina.Itens.Select(p => Linha(p, hoje)).ToList();
            view.MostrarProtocolos(linhas, $"{pagina.Total} protocolo(s) — página {_pagina} de {totalPaginas}", _pagina > 1, _pagina < totalPaginas);
            AtualizarResumo();
        }
        catch (DominioException e)
        {
            view.MostrarErro(e.Message);
        }
    }

    private void AtualizarResumo()
    {
        var r = servico.Resumo();
        int N(StatusProtocolo s) => r.TryGetValue(s, out var n) ? n : 0;
        view.MostrarResumo(
            $"Prenotados: {N(StatusProtocolo.Prenotado)}  |  Em análise: {N(StatusProtocolo.EmAnalise)}  |  " +
            $"Com exigência: {N(StatusProtocolo.ComExigencia)}  |  Registrados: {N(StatusProtocolo.Registrado)}  |  " +
            $"Cancelados: {N(StatusProtocolo.Cancelado)}");
    }

    private LinhaProtocolo Linha(Protocolo p, DateOnly hoje)
    {
        var situacao = p.Situacao(hoje, servico.Calendario, servico.Regras);
        var prazo = situacao switch
        {
            SituacaoPrazo.Concluido => p.ConcluidoEm?.ToString("dd/MM/yyyy") ?? "",
            _ => p.Status == StatusProtocolo.ComExigencia
                ? $"Vigência até {p.VencimentoPrenotacao:dd/MM}"
                : $"Registrar até {p.PrazoRegistro:dd/MM}",
        };
        return new LinhaProtocolo(
            p.Id, p.Numero, p.PrenotadoEm.ToString("dd/MM/yyyy HH:mm"), p.Apresentante.Nome, p.Natureza.Descricao(),
            p.Matricula ?? "—", p.Status.Descricao(), prazo, situacao);
    }
}

// ============================ Novo protocolo ============================

public interface INovoProtocoloView
{
    string Nome { get; }
    string Documento { get; set; }
    string Telefone { get; }
    NaturezaTitulo? Natureza { get; }
    string Matricula { get; }
    string Observacoes { get; }

    void LimparErros();
    void MostrarErros(IReadOnlyDictionary<string, string> erros);
    void Concluir(Protocolo criado);
    void MostrarErro(string mensagem);
}

public sealed class NovoProtocoloPresenter(INovoProtocoloView view, ProtocoloService servico)
{
    public static IReadOnlyList<(NaturezaTitulo Valor, string Texto)> Naturezas { get; } =
        Enum.GetValues<NaturezaTitulo>().Select(n => (n, n.Descricao())).ToList();

    /// <summary>Ao sair do campo: formata o CPF/CNPJ se estiver válido.</summary>
    public void FormatarDocumento() => view.Documento = DocumentoFiscal.Formatar(view.Documento);

    public void Salvar()
    {
        view.LimparErros();
        var apresentante = new Apresentante(view.Nome, view.Documento, view.Telefone);
        var erros = Protocolo.ValidarNovo(apresentante, view.Matricula);
        if (view.Natureza is null) erros["Natureza"] = "Escolha a natureza do título.";
        if (erros.Count > 0)
        {
            view.MostrarErros(erros);
            return;
        }
        try
        {
            view.Concluir(servico.Prenotar(apresentante, view.Natureza!.Value, view.Matricula, view.Observacoes));
        }
        catch (DominioException e)
        {
            view.MostrarErro(e.Message);
        }
    }
}

// ============================ Detalhe ============================

public sealed record DetalheProtocolo(
    string Titulo, string Status, string Situacao, SituacaoPrazo SituacaoPrazo, IReadOnlyList<(string Campo, string Valor)> Campos,
    IReadOnlyList<string> Exigencias, IReadOnlyList<string> Historico, AcoesPermitidas Acoes);

public interface IDetalheProtocoloView
{
    void Exibir(DetalheProtocolo detalhe);
    /// <summary>Pede um texto ao usuário; null se cancelou.</summary>
    string? PedirTexto(string titulo, string instrucao, bool multilinha, string valorInicial = "");
    bool Confirmar(string mensagem);
    void MostrarErro(string mensagem);
    void ImprimirRecibo(IReadOnlyList<LinhaRecibo> linhas);
}

public sealed class DetalheProtocoloPresenter(IDetalheProtocoloView view, ProtocoloService servico, int protocoloId, string nomeCartorio)
{
    /// <summary>Disparado quando o protocolo muda (a lista principal se atualiza).</summary>
    public event Action? Alterado;

    public void Carregar()
    {
        try { view.Exibir(Montar(servico.Obter(protocoloId))); }
        catch (DominioException e) { view.MostrarErro(e.Message); }
    }

    public void IniciarAnalise() => Executar(() => servico.IniciarAnalise(protocoloId));

    public void EmitirExigencias()
    {
        var texto = view.PedirTexto("Nota devolutiva", "Escreva uma exigência por linha:", multilinha: true);
        if (texto is null) return;
        Executar(() => servico.EmitirExigencias(protocoloId, texto.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)));
    }

    public void Reapresentar()
    {
        if (!view.Confirmar("Confirmar que todas as exigências foram cumpridas e o título volta para análise?")) return;
        Executar(() => servico.Reapresentar(protocoloId));
    }

    public void Registrar()
    {
        Protocolo p;
        try { p = servico.Obter(protocoloId); }
        catch (DominioException e) { view.MostrarErro(e.Message); return; }
        var prefixo = p.Natureza.PrefixoAto();
        var matricula = p.Matricula;
        if (matricula is null)
        {
            matricula = view.PedirTexto("Registrar", "Matrícula do imóvel:", multilinha: false);
            if (matricula is null) return;
        }
        var ato = view.PedirTexto("Registrar", $"Número do ato na matrícula {matricula} (ex.: {prefixo}-3):", multilinha: false, $"{prefixo}-");
        if (ato is null) return;
        Executar(() => servico.Registrar(protocoloId, ato, matricula));
    }

    public void Cancelar()
    {
        var motivo = view.PedirTexto("Cancelar protocolo", "Motivo do cancelamento:", multilinha: false);
        if (motivo is null) return;
        Executar(() => servico.Cancelar(protocoloId, motivo));
    }

    public void Imprimir()
    {
        try { view.ImprimirRecibo(ReciboProtocolo.Montar(servico.Obter(protocoloId), nomeCartorio)); }
        catch (DominioException e) { view.MostrarErro(e.Message); }
    }

    private void Executar(Func<Protocolo> acao)
    {
        try
        {
            view.Exibir(Montar(acao()));
            Alterado?.Invoke();
        }
        catch (DominioException e)
        {
            view.MostrarErro(e.Message);
        }
    }

    private DetalheProtocolo Montar(Protocolo p)
    {
        var hoje = servico.Hoje;
        var situacao = p.Situacao(hoje, servico.Calendario, servico.Regras);
        var cal = servico.Calendario;
        string Restam(DateOnly limite)
        {
            var n = cal.DiasUteisEntre(hoje, limite);
            return n >= 0 ? $"{n} dia(s) útil(eis) restante(s)" : $"vencido há {-n} dia(s) útil(eis)";
        }

        var campos = new List<(string, string)>
        {
            ("Prenotado em", p.PrenotadoEm.ToString("dd/MM/yyyy HH:mm")),
            ("Apresentante", p.Apresentante.Nome),
            ("CPF/CNPJ", DocumentoFiscal.Formatar(p.Apresentante.Documento)),
            ("Telefone", p.Apresentante.Telefone ?? "—"),
            ("Natureza", p.Natureza.Descricao()),
            ("Matrícula", p.Matricula ?? "—"),
            ("Prazo de registro", $"{p.PrazoRegistro:dd/MM/yyyy}" + (p.Status.EmAndamento() ? $" ({Restam(p.PrazoRegistro)})" : "")),
            ("Vigência da prenotação", $"{p.VencimentoPrenotacao:dd/MM/yyyy}" + (p.Status == StatusProtocolo.ComExigencia ? $" ({Restam(p.VencimentoPrenotacao)})" : "")),
        };
        if (p.Observacoes is { } obs) campos.Add(("Observações", obs));
        if (p.Ato is { } ato) campos.Add(("Ato", ato));
        if (p.MotivoCancelamento is { } motivo) campos.Add(("Motivo do cancelamento", motivo));

        return new DetalheProtocolo(
            Titulo: $"Protocolo nº {p.Numero}",
            Status: p.Status.Descricao(),
            Situacao: situacao switch
            {
                SituacaoPrazo.Vencido => "Prazo vencido",
                SituacaoPrazo.Vencendo => "Prazo vencendo",
                SituacaoPrazo.NoPrazo => "No prazo",
                _ => "Concluído",
            },
            SituacaoPrazo: situacao,
            Campos: campos,
            Exigencias: p.Exigencias.Select(e =>
                $"{(e.Pendente ? "[pendente]" : $"[cumprida em {e.CumpridaEm:dd/MM}]")} {e.Texto} (emitida em {e.EmitidaEm:dd/MM/yyyy})").ToList(),
            Historico: p.Eventos.Select(e => $"{e.Quando:dd/MM/yyyy HH:mm} — {e.Descricao} ({e.Usuario})").ToList(),
            Acoes: p.Acoes(hoje));
    }
}
