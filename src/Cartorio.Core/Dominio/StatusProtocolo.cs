namespace Cartorio.Core.Dominio;

public enum StatusProtocolo
{
    Prenotado = 1,
    EmAnalise = 2,
    ComExigencia = 3,
    Registrado = 4,
    Cancelado = 5,
}

public static class StatusProtocoloExtensions
{
    public static string Descricao(this StatusProtocolo s) => s switch
    {
        StatusProtocolo.Prenotado => "Prenotado",
        StatusProtocolo.EmAnalise => "Em análise",
        StatusProtocolo.ComExigencia => "Com exigência",
        StatusProtocolo.Registrado => "Registrado",
        StatusProtocolo.Cancelado => "Cancelado",
        _ => s.ToString(),
    };

    /// <summary>Ainda corre prazo (não foi registrado nem cancelado).</summary>
    public static bool EmAndamento(this StatusProtocolo s) => s is not (StatusProtocolo.Registrado or StatusProtocolo.Cancelado);
}
