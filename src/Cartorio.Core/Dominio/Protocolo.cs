using System.Text.RegularExpressions;

namespace Cartorio.Core.Dominio;

public sealed record Apresentante(string Nome, string Documento, string? Telefone);

public sealed class Exigencia
{
    public int Id { get; set; }
    public required string Texto { get; init; }
    public DateTime EmitidaEm { get; init; }
    public DateTime? CumpridaEm { get; set; }
    public bool Pendente => CumpridaEm is null;
}

public sealed record EventoProtocolo(DateTime Quando, string Usuario, string Descricao)
{
    /// <summary>0 enquanto não foi gravado.</summary>
    public int Id { get; set; }
}

/// <summary>Prazos em dias úteis. Os padrões seguem a Lei 6.015/73 (arts. 188 e 205) e podem ser ajustados.</summary>
public sealed record RegrasPrazo(int DiasParaRegistro = 10, int DiasVigenciaPrenotacao = 20, int DiasAlerta = 2);

/// <summary>
/// Um título apresentado ao cartório e lançado no Livro 1 (Protocolo).
/// Todas as mudanças de estado passam por métodos desta classe, que validam a regra e registram o histórico.
/// </summary>
public sealed partial class Protocolo
{
    private readonly List<Exigencia> _exigencias = [];
    private readonly List<EventoProtocolo> _eventos = [];

    public int Id { get; set; }
    /// <summary>Número de ordem no Livro 1: sequencial e sem buracos, gerado pelo repositório.</summary>
    public int Numero { get; set; }
    /// <summary>Token de concorrência otimista (rowversion no SQL Server): impede que duas pessoas sobrescrevam a mesma alteração.</summary>
    public byte[]? Versao { get; set; }
    public DateTime PrenotadoEm { get; private set; }
    public Apresentante Apresentante { get; private set; } = null!;
    public NaturezaTitulo Natureza { get; private set; }
    public string? Matricula { get; private set; }
    public string? Observacoes { get; private set; }
    public StatusProtocolo Status { get; private set; }
    public DateOnly PrazoRegistro { get; private set; }
    public DateOnly VencimentoPrenotacao { get; private set; }
    public string? Ato { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTime? ConcluidoEm { get; private set; }

    public IReadOnlyList<Exigencia> Exigencias => _exigencias;
    public IReadOnlyList<EventoProtocolo> Eventos => _eventos;

    private Protocolo() { }

    public static Protocolo Prenotar(
        Apresentante apresentante, NaturezaTitulo natureza, string? matricula, string? observacoes,
        DateTime agora, string usuario, CalendarioUtil calendario, RegrasPrazo regras)
    {
        var erros = ValidarNovo(apresentante, matricula);
        if (erros.Count > 0) throw new DominioException(string.Join(Environment.NewLine, erros.Values));

        var hoje = DateOnly.FromDateTime(agora);
        var p = new Protocolo
        {
            PrenotadoEm = agora,
            Apresentante = apresentante with
            {
                Nome = apresentante.Nome.Trim(),
                Documento = DocumentoFiscal.Limpar(apresentante.Documento),
                Telefone = string.IsNullOrWhiteSpace(apresentante.Telefone) ? null : apresentante.Telefone.Trim(),
            },
            Natureza = natureza,
            Matricula = NormalizarMatricula(matricula),
            Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim(),
            Status = StatusProtocolo.Prenotado,
            PrazoRegistro = calendario.AdicionarDiasUteis(hoje, regras.DiasParaRegistro),
            VencimentoPrenotacao = calendario.AdicionarDiasUteis(hoje, regras.DiasVigenciaPrenotacao),
        };
        p._eventos.Add(new(agora, usuario, $"Prenotado — {natureza.Descricao()}"));
        return p;
    }

    /// <summary>Validação dos campos da prenotação, por campo (o formulário mostra cada erro ao lado do campo).</summary>
    public static Dictionary<string, string> ValidarNovo(Apresentante a, string? matricula)
    {
        var erros = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(a.Nome) || a.Nome.Trim().Length < 3)
            erros[nameof(Apresentante.Nome)] = "Informe o nome do apresentante.";
        if (DocumentoFiscal.Identificar(a.Documento) is null)
            erros[nameof(Apresentante.Documento)] = "CPF ou CNPJ inválido.";
        if (!string.IsNullOrWhiteSpace(matricula) && NormalizarMatricula(matricula) is null)
            erros[nameof(Matricula)] = "Matrícula deve ter só números (ex.: 12345 ou 12.345).";
        return erros;
    }

    // ---------------- transições ----------------

    public void IniciarAnalise(DateTime agora, string usuario)
    {
        Exigir(Status == StatusProtocolo.Prenotado, "Só um título prenotado pode entrar em análise.");
        Status = StatusProtocolo.EmAnalise;
        _eventos.Add(new(agora, usuario, "Qualificação iniciada"));
    }

    /// <summary>Nota devolutiva: o título volta ao apresentante com as exigências a cumprir.</summary>
    public void EmitirExigencias(IEnumerable<string> textos, DateTime agora, string usuario)
    {
        Exigir(Status == StatusProtocolo.EmAnalise, "Exigências são emitidas durante a análise.");
        var lista = textos.Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        Exigir(lista.Count > 0, "Informe ao menos uma exigência.");
        Exigir(lista.All(t => t.Length >= 10), "Descreva cada exigência (mín. 10 caracteres).");

        foreach (var t in lista) _exigencias.Add(new Exigencia { Texto = t, EmitidaEm = agora });
        Status = StatusProtocolo.ComExigencia;
        _eventos.Add(new(agora, usuario, $"Nota devolutiva emitida com {lista.Count} exigência(s)"));
    }

    /// <summary>O apresentante trouxe o que faltava dentro da vigência da prenotação.</summary>
    public void Reapresentar(DateTime agora, string usuario)
    {
        Exigir(Status == StatusProtocolo.ComExigencia, "Só um título com exigência pode ser reapresentado.");
        Exigir(DateOnly.FromDateTime(agora) <= VencimentoPrenotacao,
            $"A prenotação venceu em {VencimentoPrenotacao:dd/MM/yyyy}; é preciso protocolar de novo.");
        foreach (var e in _exigencias.Where(e => e.Pendente)) e.CumpridaEm = agora;
        Status = StatusProtocolo.EmAnalise;
        _eventos.Add(new(agora, usuario, "Reapresentado com as exigências cumpridas"));
    }

    /// <summary>Registro (R-n) ou averbação (AV-n) na matrícula. Ex.: "R-3/12.345".</summary>
    public void Registrar(string ato, string? matricula, DateTime agora, string usuario)
    {
        Exigir(Status == StatusProtocolo.EmAnalise, "O registro é feito ao fim da análise, sem exigências pendentes.");
        var mat = NormalizarMatricula(matricula ?? Matricula);
        Exigir(mat is not null, "Informe a matrícula do imóvel.");
        var atoNorm = NormalizarAto(ato, mat!);
        Exigir(atoNorm is not null, $"Ato inválido. Use {Natureza.PrefixoAto()}-número, ex.: {Natureza.PrefixoAto()}-3/{mat}.");
        Exigir(atoNorm!.StartsWith(Natureza.PrefixoAto() + "-"),
            Natureza == NaturezaTitulo.Averbacao ? "Averbação gera ato AV-n." : "Este título gera ato de registro (R-n).");

        Matricula = mat;
        Ato = atoNorm;
        Status = StatusProtocolo.Registrado;
        ConcluidoEm = agora;
        _eventos.Add(new(agora, usuario, $"Registrado: {atoNorm}"));
    }

    /// <summary>Cancelamento por desistência do apresentante ou por vencimento da prenotação.</summary>
    public void Cancelar(string motivo, DateTime agora, string usuario)
    {
        Exigir(Status.EmAndamento(), "Protocolo já encerrado.");
        Exigir(!string.IsNullOrWhiteSpace(motivo) && motivo.Trim().Length >= 5, "Informe o motivo do cancelamento.");
        MotivoCancelamento = motivo.Trim();
        Status = StatusProtocolo.Cancelado;
        ConcluidoEm = agora;
        _eventos.Add(new(agora, usuario, $"Cancelado: {MotivoCancelamento}"));
    }

    /// <summary>Com exigência não cumprida e vigência expirada, a prenotação perde o efeito.</summary>
    public bool PrenotacaoVencida(DateOnly hoje) => Status == StatusProtocolo.ComExigencia && hoje > VencimentoPrenotacao;

    public SituacaoPrazo Situacao(DateOnly hoje, CalendarioUtil calendario, RegrasPrazo regras)
    {
        if (!Status.EmAndamento()) return SituacaoPrazo.Concluido;
        // com exigência, o que importa é a vigência da prenotação; nos demais, o prazo para registrar
        var limite = Status == StatusProtocolo.ComExigencia ? VencimentoPrenotacao : PrazoRegistro;
        var restantes = calendario.DiasUteisEntre(hoje, limite);
        if (restantes < 0) return SituacaoPrazo.Vencido;
        return restantes <= regras.DiasAlerta ? SituacaoPrazo.Vencendo : SituacaoPrazo.NoPrazo;
    }

    /// <summary>Ações possíveis agora (o presenter usa para habilitar os botões).</summary>
    public AcoesPermitidas Acoes(DateOnly hoje) => new(
        IniciarAnalise: Status == StatusProtocolo.Prenotado,
        EmitirExigencias: Status == StatusProtocolo.EmAnalise,
        Reapresentar: Status == StatusProtocolo.ComExigencia && hoje <= VencimentoPrenotacao,
        Registrar: Status == StatusProtocolo.EmAnalise,
        Cancelar: Status.EmAndamento());

    // ---------------- reidratação (usada pelos repositórios) ----------------

    public static Protocolo Reconstituir(
        int id, int numero, DateTime prenotadoEm, Apresentante apresentante, NaturezaTitulo natureza, string? matricula,
        string? observacoes, StatusProtocolo status, DateOnly prazoRegistro, DateOnly vencimentoPrenotacao, string? ato,
        string? motivoCancelamento, DateTime? concluidoEm, IEnumerable<Exigencia> exigencias, IEnumerable<EventoProtocolo> eventos)
    {
        var p = new Protocolo
        {
            Id = id, Numero = numero, PrenotadoEm = prenotadoEm, Apresentante = apresentante, Natureza = natureza,
            Matricula = matricula, Observacoes = observacoes, Status = status, PrazoRegistro = prazoRegistro,
            VencimentoPrenotacao = vencimentoPrenotacao, Ato = ato, MotivoCancelamento = motivoCancelamento, ConcluidoEm = concluidoEm,
        };
        p._exigencias.AddRange(exigencias);
        p._eventos.AddRange(eventos);
        return p;
    }

    // ---------------- auxiliares ----------------

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao) throw new DominioException(mensagem);
    }

    /// <summary>"12.345" ou "12345" → "12.345". Devolve null se não for número.</summary>
    public static string? NormalizarMatricula(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var digitos = texto.Replace(".", "").Trim();
        if (!SoDigitos().IsMatch(digitos) || digitos.TrimStart('0').Length == 0) return null;
        return int.Parse(digitos).ToString("#,0", PontoMilhar);
    }

    /// <summary>Aceita "r-3", "R-3/12345", "AV 2" e devolve "R-3/12.345".</summary>
    public static string? NormalizarAto(string? texto, string matricula)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var m = AtoRegex().Match(texto.Trim().ToUpperInvariant());
        if (!m.Success || int.Parse(m.Groups["n"].Value) == 0) return null;
        if (m.Groups["mat"].Success && NormalizarMatricula(m.Groups["mat"].Value) != matricula) return null;
        return $"{m.Groups["tipo"].Value}-{int.Parse(m.Groups["n"].Value)}/{matricula}";
    }

    // separador de milhar fixo em "." (não depende da cultura instalada na máquina)
    private static readonly System.Globalization.NumberFormatInfo PontoMilhar = new() { NumberGroupSeparator = "." };

    [GeneratedRegex(@"^\d{1,9}$")]
    private static partial Regex SoDigitos();

    [GeneratedRegex(@"^(?<tipo>R|AV)[\s-]?(?<n>\d{1,4})(?:\s*/\s*(?<mat>[\d.]+))?$")]
    private static partial Regex AtoRegex();
}

public enum SituacaoPrazo { NoPrazo, Vencendo, Vencido, Concluido }

public sealed record AcoesPermitidas(bool IniciarAnalise, bool EmitirExigencias, bool Reapresentar, bool Registrar, bool Cancelar);
