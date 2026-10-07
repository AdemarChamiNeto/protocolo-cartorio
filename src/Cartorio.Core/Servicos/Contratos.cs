using Cartorio.Core.Dominio;

namespace Cartorio.Core.Servicos;

public interface IRelogio
{
    DateTime Agora { get; }
}

public sealed class RelogioDoSistema : IRelogio
{
    public DateTime Agora => DateTime.Now;
}

public sealed record FiltroProtocolos(
    string? Texto = null,
    StatusProtocolo? Status = null,
    DateOnly? De = null,
    DateOnly? Ate = null,
    bool SoEmAndamento = false,
    int Pagina = 1,
    int TamanhoPagina = 50);

public sealed record PaginaProtocolos(IReadOnlyList<Protocolo> Itens, int Total);

public interface IProtocoloRepository
{
    /// <summary>Grava um novo protocolo, atribuindo Id e o próximo número do Livro 1 na mesma transação.</summary>
    void Inserir(Protocolo protocolo);

    /// <summary>Grava status, campos, exigências novas/cumpridas e eventos novos.</summary>
    void Atualizar(Protocolo protocolo);

    Protocolo? ObterPorId(int id);

    PaginaProtocolos Buscar(FiltroProtocolos filtro);

    /// <summary>Protocolos com exigência e vigência anterior a <paramref name="hoje"/>.</summary>
    IReadOnlyList<Protocolo> ComPrenotacaoVencida(DateOnly hoje);

    IReadOnlyDictionary<StatusProtocolo, int> ContarPorStatus();

    IReadOnlyList<DateOnly> FeriadosLocais();
}
