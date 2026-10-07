using System.Text;
using Cartorio.Core.Dominio;

namespace Cartorio.Core.Relatorios;

/// <summary>CSV com ";" e BOM UTF-8: abre direto no Excel em português com acentos corretos.</summary>
public static class ExportadorCsv
{
    public static string Gerar(IEnumerable<Protocolo> protocolos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Número;Data;Apresentante;Documento;Natureza;Matrícula;Status;Prazo registro;Vigência prenotação;Ato");
        foreach (var p in protocolos)
        {
            sb.AppendJoin(';',
                p.Numero,
                p.PrenotadoEm.ToString("dd/MM/yyyy HH:mm"),
                Campo(p.Apresentante.Nome),
                DocumentoFiscal.Formatar(p.Apresentante.Documento),
                Campo(p.Natureza.Descricao()),
                p.Matricula ?? "",
                p.Status.Descricao(),
                p.PrazoRegistro.ToString("dd/MM/yyyy"),
                p.VencimentoPrenotacao.ToString("dd/MM/yyyy"),
                p.Ato ?? "");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static byte[] GerarBytes(IEnumerable<Protocolo> protocolos) =>
        [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(Gerar(protocolos))];

    /// <summary>Aspas quando há separador, aspas ou quebra; e neutraliza fórmulas (=, +, -, @) contra CSV injection.</summary>
    internal static string Campo(string valor)
    {
        if (valor.Length > 0 && "=+-@".Contains(valor[0])) valor = "'" + valor;
        return valor.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{valor.Replace("\"", "\"\"")}\"" : valor;
    }
}
