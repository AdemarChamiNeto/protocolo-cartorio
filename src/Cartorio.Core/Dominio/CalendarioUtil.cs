namespace Cartorio.Core.Dominio;

/// <summary>
/// Dias úteis: segunda a sexta, menos feriados nacionais (calculados por ano) e os feriados locais cadastrados.
/// Os prazos do registro de imóveis são contados em dias úteis.
/// </summary>
public sealed class CalendarioUtil
{
    private readonly HashSet<DateOnly> _locais;
    private readonly Dictionary<int, HashSet<DateOnly>> _nacionaisPorAno = new();

    public CalendarioUtil(IEnumerable<DateOnly>? feriadosLocais = null)
    {
        _locais = new HashSet<DateOnly>(feriadosLocais ?? []);
    }

    public bool EhDiaUtil(DateOnly dia) =>
        dia.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
        && !_locais.Contains(dia)
        && !Nacionais(dia.Year).Contains(dia);

    public string? NomeFeriado(DateOnly dia) =>
        FeriadosNacionais.DoAno(dia.Year).FirstOrDefault(f => f.Data == dia)?.Nome
        ?? (_locais.Contains(dia) ? "Feriado local" : null);

    /// <summary>
    /// Conta <paramref name="dias"/> dias úteis a partir do dia seguinte ao início
    /// (o dia do protocolo não conta). Ex.: segunda + 1 dia útil = terça.
    /// </summary>
    public DateOnly AdicionarDiasUteis(DateOnly inicio, int dias)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dias);
        var d = inicio;
        while (dias > 0)
        {
            d = d.AddDays(1);
            if (EhDiaUtil(d)) dias--;
        }
        return d;
    }

    /// <summary>Dias úteis em (de, ate] — negativo se <paramref name="ate"/> vier antes.</summary>
    public int DiasUteisEntre(DateOnly de, DateOnly ate)
    {
        if (ate < de) return -DiasUteisEntre(ate, de);
        var n = 0;
        for (var d = de.AddDays(1); d <= ate; d = d.AddDays(1))
            if (EhDiaUtil(d)) n++;
        return n;
    }

    private HashSet<DateOnly> Nacionais(int ano)
    {
        if (!_nacionaisPorAno.TryGetValue(ano, out var set))
            _nacionaisPorAno[ano] = set = FeriadosNacionais.DoAno(ano).Select(f => f.Data).ToHashSet();
        return set;
    }
}
