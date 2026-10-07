namespace Cartorio.Core.Dominio;

/// <summary>Natureza do título apresentado (lista resumida para a demonstração).</summary>
public enum NaturezaTitulo
{
    CompraEVenda = 1,
    Doacao = 2,
    FormalDePartilha = 3,
    AlienacaoFiduciaria = 4,
    CedulaDeCredito = 5,
    CartaDeArrematacao = 6,
    Averbacao = 7,
    Outros = 99,
}

public static class NaturezaTituloExtensions
{
    public static string Descricao(this NaturezaTitulo n) => n switch
    {
        NaturezaTitulo.CompraEVenda => "Escritura de compra e venda",
        NaturezaTitulo.Doacao => "Escritura de doação",
        NaturezaTitulo.FormalDePartilha => "Formal de partilha",
        NaturezaTitulo.AlienacaoFiduciaria => "Contrato com alienação fiduciária",
        NaturezaTitulo.CedulaDeCredito => "Cédula de crédito",
        NaturezaTitulo.CartaDeArrematacao => "Carta de arrematação",
        NaturezaTitulo.Averbacao => "Requerimento de averbação",
        NaturezaTitulo.Outros => "Outros",
        _ => n.ToString(),
    };

    /// <summary>Averbação vira AV-n; os demais atos viram R-n na matrícula.</summary>
    public static string PrefixoAto(this NaturezaTitulo n) => n == NaturezaTitulo.Averbacao ? "AV" : "R";
}
