namespace Cartorio.WinForms.Forms;

/// <summary>Caixa de diálogo genérica para pedir um texto (motivo, exigências, número do ato).</summary>
public partial class FormEntradaTexto : Form
{
    public FormEntradaTexto(string titulo, string instrucao, bool multilinha, string valorInicial)
    {
        InitializeComponent();
        Text = titulo;
        lblInstrucao.Text = instrucao;
        txtValor.Multiline = multilinha;
        txtValor.AcceptsReturn = multilinha;
        txtValor.ScrollBars = multilinha ? ScrollBars.Vertical : ScrollBars.None;
        txtValor.Height = multilinha ? 160 : txtValor.PreferredHeight;
        if (!multilinha) Height -= 160 - txtValor.PreferredHeight;
        txtValor.Text = valorInicial;
        txtValor.SelectionStart = valorInicial.Length;
    }

    public string Valor => txtValor.Text;

    public static string? Pedir(IWin32Window dono, string titulo, string instrucao, bool multilinha, string valorInicial = "")
    {
        using var f = new FormEntradaTexto(titulo, instrucao, multilinha, valorInicial);
        return f.ShowDialog(dono) == DialogResult.OK ? f.Valor : null;
    }
}
