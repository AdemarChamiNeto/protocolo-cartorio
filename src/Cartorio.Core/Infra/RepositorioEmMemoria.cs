using Cartorio.Core.Dominio;
using Cartorio.Core.Servicos;

namespace Cartorio.Core.Infra;

/// <summary>Repositório em memória: usado nos testes e no "modo demonstração" (sem SQL Server).</summary>
public sealed class RepositorioEmMemoria(IEnumerable<DateOnly>? feriadosLocais = null) : IProtocoloRepository
{
    private readonly List<Protocolo> _itens = [];
    private readonly List<DateOnly> _feriados = feriadosLocais?.ToList() ?? [];
    private int _ultimoNumero;
    private int _ultimoIdExigencia;

    public void Inserir(Protocolo protocolo)
    {
        protocolo.Numero = ++_ultimoNumero;
        protocolo.Id = protocolo.Numero;
        NumerarExigencias(protocolo);
        _itens.Add(protocolo);
    }

    public void Atualizar(Protocolo protocolo) => NumerarExigencias(protocolo);

    public Protocolo? ObterPorId(int id) => _itens.FirstOrDefault(p => p.Id == id);

    public PaginaProtocolos Buscar(FiltroProtocolos f)
    {
        IEnumerable<Protocolo> q = _itens;
        if (f.Status is { } s) q = q.Where(p => p.Status == s);
        if (f.SoEmAndamento) q = q.Where(p => p.Status.EmAndamento());
        if (f.De is { } de) q = q.Where(p => DateOnly.FromDateTime(p.PrenotadoEm) >= de);
        if (f.Ate is { } ate) q = q.Where(p => DateOnly.FromDateTime(p.PrenotadoEm) <= ate);
        if (!string.IsNullOrWhiteSpace(f.Texto))
        {
            var t = f.Texto.Trim();
            var doc = DocumentoFiscal.Limpar(t);
            q = q.Where(p =>
                p.Numero.ToString() == t
                || p.Apresentante.Nome.Contains(t, StringComparison.CurrentCultureIgnoreCase)
                || (doc.Length >= 3 && p.Apresentante.Documento.Contains(doc))
                || (p.Matricula is not null && p.Matricula.Replace(".", "") == t.Replace(".", "")));
        }
        var lista = q.OrderByDescending(p => p.Numero).ToList();
        return new PaginaProtocolos(lista.Skip((f.Pagina - 1) * f.TamanhoPagina).Take(f.TamanhoPagina).ToList(), lista.Count);
    }

    public IReadOnlyList<Protocolo> ComPrenotacaoVencida(DateOnly hoje) => _itens.Where(p => p.PrenotacaoVencida(hoje)).ToList();

    public IReadOnlyDictionary<StatusProtocolo, int> ContarPorStatus() =>
        _itens.GroupBy(p => p.Status).ToDictionary(g => g.Key, g => g.Count());

    public IReadOnlyList<DateOnly> FeriadosLocais() => _feriados;

    private void NumerarExigencias(Protocolo p)
    {
        foreach (var e in p.Exigencias.Where(e => e.Id == 0)) e.Id = ++_ultimoIdExigencia;
    }
}

/// <summary>Relógio controlado manualmente (testes e geração dos dados de demonstração).</summary>
public sealed class RelogioManual(DateTime inicio) : IRelogio
{
    public DateTime Agora { get; set; } = inicio;
    public void Avancar(TimeSpan t) => Agora = Agora.Add(t);
}
