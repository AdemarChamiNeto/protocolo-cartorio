namespace Cartorio.Core.Dominio;

public sealed record Feriado(DateOnly Data, string Nome);

/// <summary>
/// Feriados nacionais (Lei 662/1949, Lei 6.802/1980 e Lei 14.759/2023), mais a Sexta-feira Santa, calculada pela Páscoa.
/// Carnaval e Corpus Christi são ponto facultativo nacional; onde forem feriado, entram como feriado local.
/// </summary>
public static class FeriadosNacionais
{
    public static IReadOnlyList<Feriado> DoAno(int ano)
    {
        var pascoa = Pascoa(ano);
        var lista = new List<Feriado>
        {
            new(new DateOnly(ano, 1, 1), "Confraternização Universal"),
            new(pascoa.AddDays(-2), "Sexta-feira Santa"),
            new(new DateOnly(ano, 4, 21), "Tiradentes"),
            new(new DateOnly(ano, 5, 1), "Dia do Trabalho"),
            new(new DateOnly(ano, 9, 7), "Independência do Brasil"),
            new(new DateOnly(ano, 10, 12), "Nossa Senhora Aparecida"),
            new(new DateOnly(ano, 11, 2), "Finados"),
            new(new DateOnly(ano, 11, 15), "Proclamação da República"),
            new(new DateOnly(ano, 12, 25), "Natal"),
        };
        if (ano >= 2024) lista.Add(new(new DateOnly(ano, 11, 20), "Dia Nacional de Zumbi e da Consciência Negra"));
        return lista.OrderBy(f => f.Data).ToList();
    }

    /// <summary>Domingo de Páscoa (algoritmo de Meeus/Jones/Butcher, calendário gregoriano).</summary>
    public static DateOnly Pascoa(int ano)
    {
        int a = ano % 19, b = ano / 100, c = ano % 100, d = b / 4, e = b % 4;
        int f = (b + 8) / 25, g = (b - f + 1) / 3, h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int mes = (h + l - 7 * m + 114) / 31, dia = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(ano, mes, dia);
    }
}
