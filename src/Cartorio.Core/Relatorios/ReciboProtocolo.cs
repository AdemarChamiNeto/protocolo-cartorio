using Cartorio.Core.Dominio;

namespace Cartorio.Core.Relatorios;

public sealed record LinhaRecibo(string Texto, bool Destaque = false, bool Separador = false);

/// <summary>
/// Conteúdo do comprovante de protocolo entregue ao apresentante. Fica separado do desenho (PrintDocument)
/// para ser testado e reaproveitado (impressão, PDF, tela).
/// </summary>
public static class ReciboProtocolo
{
    public static IReadOnlyList<LinhaRecibo> Montar(Protocolo p, string nomeCartorio)
    {
        var linhas = new List<LinhaRecibo>
        {
            new(nomeCartorio.ToUpperInvariant(), Destaque: true),
            new("COMPROVANTE DE PROTOCOLO — LIVRO 1"),
            new("", Separador: true),
            new($"Protocolo nº {p.Numero:N0}".Replace(",", "."), Destaque: true),
            new($"Data e hora: {p.PrenotadoEm:dd/MM/yyyy HH:mm}"),
            new($"Título: {p.Natureza.Descricao()}"),
            new($"Matrícula: {p.Matricula ?? "a definir na qualificação"}"),
            new("", Separador: true),
            new($"Apresentante: {p.Apresentante.Nome}"),
            new($"{(DocumentoFiscal.Identificar(p.Apresentante.Documento) == TipoDocumento.Cnpj ? "CNPJ" : "CPF")}: {DocumentoFiscal.Formatar(p.Apresentante.Documento)}"),
        };
        if (p.Apresentante.Telefone is { } tel) linhas.Add(new($"Telefone: {tel}"));
        linhas.Add(new("", Separador: true));
        linhas.Add(new($"Prazo para registro: {p.PrazoRegistro:dd/MM/yyyy}"));
        linhas.Add(new($"Vigência da prenotação: até {p.VencimentoPrenotacao:dd/MM/yyyy}"));
        linhas.Add(new("Havendo exigências, cumpra-as dentro da vigência para manter a prioridade do protocolo."));
        return linhas;
    }
}
