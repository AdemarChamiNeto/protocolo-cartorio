using System.Drawing.Printing;
using Cartorio.Core.Relatorios;

namespace Cartorio.WinForms.Impressao;

/// <summary>Desenha o comprovante (montado no Core) numa página A4 e abre a pré-visualização.</summary>
public static class ImpressorRecibo
{
    public static void Visualizar(IWin32Window dono, IReadOnlyList<LinhaRecibo> linhas, string titulo)
    {
        using var doc = new PrintDocument { DocumentName = titulo };
        doc.PrintPage += (_, e) => Desenhar(e, linhas);
        using var preview = new PrintPreviewDialog
        {
            Document = doc,
            Width = 800,
            Height = 900,
            Text = titulo,
            UseAntiAlias = true,
        };
        preview.ShowDialog(dono);
    }

    private static void Desenhar(PrintPageEventArgs e, IReadOnlyList<LinhaRecibo> linhas)
    {
        var g = e.Graphics!;
        using var normal = new Font("Segoe UI", 11);
        using var destaque = new Font("Segoe UI", 13, FontStyle.Bold);
        using var caneta = new Pen(Color.Gray, 1);
        var area = e.MarginBounds;
        float y = area.Top;

        foreach (var linha in linhas)
        {
            if (linha.Separador)
            {
                y += 6;
                g.DrawLine(caneta, area.Left, y, area.Right, y);
                y += 10;
                continue;
            }
            var fonte = linha.Destaque ? destaque : normal;
            var tamanho = g.MeasureString(linha.Texto, fonte, area.Width);
            g.DrawString(linha.Texto, fonte, Brushes.Black, new RectangleF(area.Left, y, area.Width, tamanho.Height));
            y += tamanho.Height + 4;
        }

        y += 60;
        g.DrawLine(caneta, area.Left + area.Width / 4f, y, area.Right - area.Width / 4f, y);
        var assinatura = "Escrevente autorizado";
        var largura = g.MeasureString(assinatura, normal).Width;
        g.DrawString(assinatura, normal, Brushes.Black, area.Left + (area.Width - largura) / 2, y + 4);
        e.HasMorePages = false;
    }
}
