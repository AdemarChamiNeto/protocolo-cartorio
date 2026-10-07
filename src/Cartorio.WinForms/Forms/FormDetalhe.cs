using Cartorio.Core.Apresentacao;
using Cartorio.Core.Dominio;
using Cartorio.Core.Relatorios;
using Cartorio.Core.Servicos;
using Cartorio.WinForms.Impressao;

namespace Cartorio.WinForms.Forms;

/// <summary>Detalhe e andamento de um protocolo. Os botões só ficam ativos quando a ação é permitida.</summary>
public partial class FormDetalhe : Form, IDetalheProtocoloView
{
    private readonly DetalheProtocoloPresenter _presenter;

    public FormDetalhe(ProtocoloService servico, int protocoloId, string nomeCartorio)
    {
        InitializeComponent();
        _presenter = new DetalheProtocoloPresenter(this, servico, protocoloId, nomeCartorio);
        _presenter.Alterado += () => Alterado = true;

        btnAnalise.Click += (_, _) => _presenter.IniciarAnalise();
        btnExigencias.Click += (_, _) => _presenter.EmitirExigencias();
        btnReapresentar.Click += (_, _) => _presenter.Reapresentar();
        btnRegistrar.Click += (_, _) => _presenter.Registrar();
        btnCancelarProtocolo.Click += (_, _) => _presenter.Cancelar();
        btnImprimir.Click += (_, _) => _presenter.Imprimir();
        Load += (_, _) => _presenter.Carregar();
    }

    /// <summary>A lista principal recarrega ao fechar se algo mudou.</summary>
    public bool Alterado { get; private set; }

    public void Exibir(DetalheProtocolo d)
    {
        Text = d.Titulo;
        lblTitulo.Text = d.Titulo;
        lblStatus.Text = $"{d.Status}  •  {d.Situacao}";
        lblStatus.ForeColor = d.SituacaoPrazo switch
        {
            SituacaoPrazo.Vencido => Color.Firebrick,
            SituacaoPrazo.Vencendo => Color.DarkOrange,
            SituacaoPrazo.NoPrazo => Color.SeaGreen,
            _ => SystemColors.GrayText,
        };

        lstCampos.BeginUpdate();
        lstCampos.Items.Clear();
        foreach (var (campo, valor) in d.Campos) lstCampos.Items.Add(new ListViewItem(new[] { campo, valor }));
        lstCampos.EndUpdate();

        lstExigencias.DataSource = d.Exigencias.Count > 0 ? d.Exigencias.ToList() : new List<string> { "Nenhuma exigência emitida." };
        lstHistorico.DataSource = d.Historico.ToList();

        btnAnalise.Enabled = d.Acoes.IniciarAnalise;
        btnExigencias.Enabled = d.Acoes.EmitirExigencias;
        btnReapresentar.Enabled = d.Acoes.Reapresentar;
        btnRegistrar.Enabled = d.Acoes.Registrar;
        btnCancelarProtocolo.Enabled = d.Acoes.Cancelar;
    }

    public string? PedirTexto(string titulo, string instrucao, bool multilinha, string valorInicial = "") =>
        FormEntradaTexto.Pedir(this, titulo, instrucao, multilinha, valorInicial);

    public bool Confirmar(string mensagem) =>
        MessageBox.Show(this, mensagem, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    public void MostrarErro(string mensagem) => MessageBox.Show(this, mensagem, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public void ImprimirRecibo(IReadOnlyList<LinhaRecibo> linhas) => ImpressorRecibo.Visualizar(this, linhas, Text);
}
