using System.Text;

namespace Cartorio.Core.Dominio;

public enum TipoDocumento { Cpf, Cnpj }

/// <summary>
/// CPF e CNPJ com validação dos dígitos verificadores (módulo 11).
/// O CNPJ aceita o formato alfanumérico que a Receita passa a emitir a partir de julho/2026:
/// as 12 primeiras posições podem ter letras, cujo valor no cálculo é (código ASCII − 48); os 2 DVs continuam numéricos.
/// </summary>
public static class DocumentoFiscal
{
    /// <summary>Tira pontuação e espaços e passa para maiúsculas.</summary>
    public static string Limpar(string? texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto ?? "")
            if (char.IsAsciiLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        return sb.ToString();
    }

    public static TipoDocumento? Identificar(string? texto)
    {
        var d = Limpar(texto);
        if (d.Length == 11 && CpfValido(d)) return TipoDocumento.Cpf;
        if (d.Length == 14 && CnpjValido(d)) return TipoDocumento.Cnpj;
        return null;
    }

    public static bool CpfValido(string? texto)
    {
        var d = Limpar(texto);
        if (d.Length != 11 || !d.All(char.IsAsciiDigit) || d.Distinct().Count() == 1) return false;
        return Dv(d[..9], 10) == d[9] - '0' && Dv(d[..10], 11) == d[10] - '0';

        static int Dv(string s, int pesoInicial)
        {
            var soma = 0;
            for (var i = 0; i < s.Length; i++) soma += (s[i] - '0') * (pesoInicial - i);
            var resto = soma * 10 % 11;
            return resto == 10 ? 0 : resto;
        }
    }

    public static bool CnpjValido(string? texto)
    {
        var d = Limpar(texto);
        if (d.Length != 14 || !d[..12].All(char.IsAsciiLetterOrDigit) || !d[12..].All(char.IsAsciiDigit)) return false;
        if (d.Distinct().Count() == 1) return false;
        return Dv(d[..12]) == d[12] - '0' && Dv(d[..13]) == d[13] - '0';

        static int Dv(string s)
        {
            // pesos 2..9 da direita para a esquerda, recomeçando em 2
            var soma = 0;
            for (int i = s.Length - 1, peso = 2; i >= 0; i--, peso = peso == 9 ? 2 : peso + 1)
                soma += (s[i] - '0') * peso;
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }

    /// <summary>000.000.000-00 ou 00.000.000/0000-00; devolve o texto original se não for válido.</summary>
    public static string Formatar(string? texto)
    {
        var d = Limpar(texto);
        return Identificar(d) switch
        {
            TipoDocumento.Cpf => $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}",
            TipoDocumento.Cnpj => $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}",
            _ => texto ?? "",
        };
    }
}
