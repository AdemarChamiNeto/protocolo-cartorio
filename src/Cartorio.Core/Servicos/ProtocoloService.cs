using Cartorio.Core.Dominio;

namespace Cartorio.Core.Servicos;

/// <summary>Casos de uso: carrega o protocolo, aplica a regra do domínio e grava. É o que os presenters chamam.</summary>
public sealed class ProtocoloService
{
    private readonly IProtocoloRepository _repo;
    private readonly IRelogio _relogio;
    private readonly string _usuario;

    public ProtocoloService(IProtocoloRepository repo, IRelogio relogio, string usuario, RegrasPrazo? regras = null)
    {
        _repo = repo;
        _relogio = relogio;
        _usuario = usuario;
        Regras = regras ?? new RegrasPrazo();
        Calendario = new CalendarioUtil(repo.FeriadosLocais());
    }

    public RegrasPrazo Regras { get; }
    public CalendarioUtil Calendario { get; }
    public DateOnly Hoje => DateOnly.FromDateTime(_relogio.Agora);

    public Protocolo Prenotar(Apresentante apresentante, NaturezaTitulo natureza, string? matricula, string? observacoes)
    {
        var p = Protocolo.Prenotar(apresentante, natureza, matricula, observacoes, _relogio.Agora, _usuario, Calendario, Regras);
        _repo.Inserir(p);
        return p;
    }

    public Protocolo Obter(int id) => _repo.ObterPorId(id) ?? throw new DominioException($"Protocolo {id} não encontrado.");

    public PaginaProtocolos Buscar(FiltroProtocolos filtro) => _repo.Buscar(filtro);

    public IReadOnlyDictionary<StatusProtocolo, int> Resumo() => _repo.ContarPorStatus();

    public Protocolo IniciarAnalise(int id) => Alterar(id, p => p.IniciarAnalise(_relogio.Agora, _usuario));

    public Protocolo EmitirExigencias(int id, IEnumerable<string> textos) => Alterar(id, p => p.EmitirExigencias(textos, _relogio.Agora, _usuario));

    public Protocolo Reapresentar(int id) => Alterar(id, p => p.Reapresentar(_relogio.Agora, _usuario));

    public Protocolo Registrar(int id, string ato, string? matricula) => Alterar(id, p => p.Registrar(ato, matricula, _relogio.Agora, _usuario));

    public Protocolo Cancelar(int id, string motivo) => Alterar(id, p => p.Cancelar(motivo, _relogio.Agora, _usuario));

    /// <summary>Rotina diária: cancela as prenotações que perderam o efeito. Devolve os números cancelados.</summary>
    public IReadOnlyList<int> CancelarPrenotacoesVencidas()
    {
        var numeros = new List<int>();
        foreach (var p in _repo.ComPrenotacaoVencida(Hoje))
        {
            p.Cancelar($"Prenotação vencida em {p.VencimentoPrenotacao:dd/MM/yyyy} sem cumprimento das exigências", _relogio.Agora, "sistema");
            _repo.Atualizar(p);
            numeros.Add(p.Numero);
        }
        return numeros;
    }

    private Protocolo Alterar(int id, Action<Protocolo> acao)
    {
        var p = Obter(id);
        acao(p);
        _repo.Atualizar(p);
        return p;
    }
}
