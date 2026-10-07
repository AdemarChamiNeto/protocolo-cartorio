namespace Cartorio.WinForms.Forms;

partial class FormDetalhe
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        painelTopo = new Panel();
        lblTitulo = new Label();
        lblStatus = new Label();
        painelAcoes = new FlowLayoutPanel();
        btnAnalise = new Button();
        btnExigencias = new Button();
        btnReapresentar = new Button();
        btnRegistrar = new Button();
        btnCancelarProtocolo = new Button();
        btnImprimir = new Button();
        divisor = new SplitContainer();
        lstCampos = new ListView();
        colCampo = new ColumnHeader();
        colValor = new ColumnHeader();
        abas = new TabControl();
        abaExigencias = new TabPage();
        lstExigencias = new ListBox();
        abaHistorico = new TabPage();
        lstHistorico = new ListBox();
        painelTopo.SuspendLayout();
        painelAcoes.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)divisor).BeginInit();
        divisor.Panel1.SuspendLayout();
        divisor.Panel2.SuspendLayout();
        divisor.SuspendLayout();
        abas.SuspendLayout();
        abaExigencias.SuspendLayout();
        abaHistorico.SuspendLayout();
        SuspendLayout();
        //
        // painelTopo
        //
        painelTopo.Controls.Add(lblStatus);
        painelTopo.Controls.Add(lblTitulo);
        painelTopo.Dock = DockStyle.Top;
        painelTopo.Height = 64;
        painelTopo.Name = "painelTopo";
        painelTopo.Padding = new Padding(12, 8, 12, 0);
        lblTitulo.AutoSize = true;
        lblTitulo.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        lblTitulo.Location = new Point(12, 6);
        lblTitulo.Name = "lblTitulo";
        lblTitulo.Text = "Protocolo";
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblStatus.Location = new Point(14, 38);
        lblStatus.Name = "lblStatus";
        lblStatus.Text = "Status";
        //
        // painelAcoes
        //
        painelAcoes.Controls.Add(btnAnalise);
        painelAcoes.Controls.Add(btnExigencias);
        painelAcoes.Controls.Add(btnReapresentar);
        painelAcoes.Controls.Add(btnRegistrar);
        painelAcoes.Controls.Add(btnCancelarProtocolo);
        painelAcoes.Controls.Add(btnImprimir);
        painelAcoes.Dock = DockStyle.Top;
        painelAcoes.Height = 46;
        painelAcoes.Name = "painelAcoes";
        painelAcoes.Padding = new Padding(9, 4, 9, 4);
        btnAnalise.AutoSize = true;
        btnAnalise.Name = "btnAnalise";
        btnAnalise.Text = "Iniciar &análise";
        btnExigencias.AutoSize = true;
        btnExigencias.Name = "btnExigencias";
        btnExigencias.Text = "Emitir &exigências";
        btnReapresentar.AutoSize = true;
        btnReapresentar.Name = "btnReapresentar";
        btnReapresentar.Text = "Rea&presentado";
        btnRegistrar.AutoSize = true;
        btnRegistrar.Name = "btnRegistrar";
        btnRegistrar.Text = "&Registrar";
        btnCancelarProtocolo.AutoSize = true;
        btnCancelarProtocolo.Name = "btnCancelarProtocolo";
        btnCancelarProtocolo.Text = "&Cancelar protocolo";
        btnImprimir.AutoSize = true;
        btnImprimir.Name = "btnImprimir";
        btnImprimir.Text = "&Imprimir comprovante";
        //
        // divisor
        //
        divisor.Dock = DockStyle.Fill;
        divisor.Name = "divisor";
        divisor.Orientation = Orientation.Horizontal;
        divisor.Panel1.Controls.Add(lstCampos);
        divisor.Panel1.Padding = new Padding(12, 4, 12, 4);
        divisor.Panel2.Controls.Add(abas);
        divisor.Panel2.Padding = new Padding(12, 4, 12, 12);
        divisor.Size = new Size(760, 490);
        divisor.SplitterDistance = 250;
        //
        // lstCampos
        //
        lstCampos.Columns.AddRange(new ColumnHeader[] { colCampo, colValor });
        lstCampos.Dock = DockStyle.Fill;
        lstCampos.FullRowSelect = true;
        lstCampos.HeaderStyle = ColumnHeaderStyle.None;
        lstCampos.Name = "lstCampos";
        lstCampos.View = View.Details;
        colCampo.Width = 180;
        colValor.Width = 520;
        //
        // abas
        //
        abas.Controls.Add(abaExigencias);
        abas.Controls.Add(abaHistorico);
        abas.Dock = DockStyle.Fill;
        abas.Name = "abas";
        abaExigencias.Controls.Add(lstExigencias);
        abaExigencias.Name = "abaExigencias";
        abaExigencias.Text = "Exigências";
        abaExigencias.Padding = new Padding(4);
        lstExigencias.Dock = DockStyle.Fill;
        lstExigencias.HorizontalScrollbar = true;
        lstExigencias.Name = "lstExigencias";
        abaHistorico.Controls.Add(lstHistorico);
        abaHistorico.Name = "abaHistorico";
        abaHistorico.Text = "Histórico";
        abaHistorico.Padding = new Padding(4);
        lstHistorico.Dock = DockStyle.Fill;
        lstHistorico.HorizontalScrollbar = true;
        lstHistorico.Name = "lstHistorico";
        //
        // FormDetalhe
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(760, 600);
        Controls.Add(divisor);
        Controls.Add(painelAcoes);
        Controls.Add(painelTopo);
        KeyPreview = true;
        MinimumSize = new Size(700, 500);
        Name = "FormDetalhe";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Protocolo";
        painelTopo.ResumeLayout(false);
        painelTopo.PerformLayout();
        painelAcoes.ResumeLayout(false);
        painelAcoes.PerformLayout();
        divisor.Panel1.ResumeLayout(false);
        divisor.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)divisor).EndInit();
        divisor.ResumeLayout(false);
        abas.ResumeLayout(false);
        abaExigencias.ResumeLayout(false);
        abaHistorico.ResumeLayout(false);
        ResumeLayout(false);
    }

    private Panel painelTopo;
    private Label lblTitulo;
    private Label lblStatus;
    private FlowLayoutPanel painelAcoes;
    private Button btnAnalise;
    private Button btnExigencias;
    private Button btnReapresentar;
    private Button btnRegistrar;
    private Button btnCancelarProtocolo;
    private Button btnImprimir;
    private SplitContainer divisor;
    private ListView lstCampos;
    private ColumnHeader colCampo;
    private ColumnHeader colValor;
    private TabControl abas;
    private TabPage abaExigencias;
    private ListBox lstExigencias;
    private TabPage abaHistorico;
    private ListBox lstHistorico;
}
