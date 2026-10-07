namespace Cartorio.WinForms.Forms;

partial class FormPrincipal
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        barra = new ToolStrip();
        btnNovo = new ToolStripButton();
        btnAbrir = new ToolStripButton();
        separador = new ToolStripSeparator();
        btnExportar = new ToolStripButton();
        btnAtualizar = new ToolStripButton();
        painelFiltros = new FlowLayoutPanel();
        lblBusca = new Label();
        txtBusca = new TextBox();
        lblStatusFiltro = new Label();
        cboStatus = new ComboBox();
        chkAndamento = new CheckBox();
        lblDe = new Label();
        dtpDe = new DateTimePicker();
        lblAte = new Label();
        dtpAte = new DateTimePicker();
        grade = new DataGridView();
        painelPaginacao = new FlowLayoutPanel();
        btnProxima = new Button();
        lblPagina = new Label();
        btnAnterior = new Button();
        rodape = new StatusStrip();
        lblResumo = new ToolStripStatusLabel();
        barra.SuspendLayout();
        painelFiltros.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)grade).BeginInit();
        painelPaginacao.SuspendLayout();
        rodape.SuspendLayout();
        SuspendLayout();
        //
        // barra
        //
        barra.GripStyle = ToolStripGripStyle.Hidden;
        barra.ImageScalingSize = new Size(20, 20);
        barra.Items.AddRange(new ToolStripItem[] { btnNovo, btnAbrir, separador, btnExportar, btnAtualizar });
        barra.Name = "barra";
        barra.Padding = new Padding(6, 2, 6, 2);
        btnNovo.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnNovo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnNovo.Name = "btnNovo";
        btnNovo.Text = "Nova prenotação (F2)";
        btnAbrir.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnAbrir.Name = "btnAbrir";
        btnAbrir.Text = "Abrir (Enter)";
        btnExportar.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnExportar.Name = "btnExportar";
        btnExportar.Text = "Exportar CSV";
        btnAtualizar.DisplayStyle = ToolStripItemDisplayStyle.Text;
        btnAtualizar.Name = "btnAtualizar";
        btnAtualizar.Text = "Atualizar (F5)";
        //
        // painelFiltros
        //
        painelFiltros.AutoSize = true;
        painelFiltros.Controls.Add(lblBusca);
        painelFiltros.Controls.Add(txtBusca);
        painelFiltros.Controls.Add(lblStatusFiltro);
        painelFiltros.Controls.Add(cboStatus);
        painelFiltros.Controls.Add(chkAndamento);
        painelFiltros.Controls.Add(lblDe);
        painelFiltros.Controls.Add(dtpDe);
        painelFiltros.Controls.Add(lblAte);
        painelFiltros.Controls.Add(dtpAte);
        painelFiltros.Dock = DockStyle.Top;
        painelFiltros.Name = "painelFiltros";
        painelFiltros.Padding = new Padding(8, 6, 8, 6);
        lblBusca.Anchor = AnchorStyles.Left;
        lblBusca.AutoSize = true;
        lblBusca.Text = "&Buscar:";
        txtBusca.Name = "txtBusca";
        txtBusca.PlaceholderText = "nº, apresentante, CPF/CNPJ ou matrícula (Ctrl+F)";
        txtBusca.Size = new Size(300, 23);
        txtBusca.TabIndex = 0;
        lblStatusFiltro.Anchor = AnchorStyles.Left;
        lblStatusFiltro.AutoSize = true;
        lblStatusFiltro.Margin = new Padding(12, 0, 3, 0);
        lblStatusFiltro.Text = "&Status:";
        cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cboStatus.Name = "cboStatus";
        cboStatus.Size = new Size(130, 23);
        cboStatus.TabIndex = 1;
        chkAndamento.Anchor = AnchorStyles.Left;
        chkAndamento.AutoSize = true;
        chkAndamento.Margin = new Padding(12, 3, 3, 3);
        chkAndamento.Name = "chkAndamento";
        chkAndamento.TabIndex = 2;
        chkAndamento.Text = "Só em &andamento";
        lblDe.Anchor = AnchorStyles.Left;
        lblDe.AutoSize = true;
        lblDe.Margin = new Padding(12, 0, 3, 0);
        lblDe.Text = "De:";
        dtpDe.Format = DateTimePickerFormat.Short;
        dtpDe.Name = "dtpDe";
        dtpDe.ShowCheckBox = true;
        dtpDe.Checked = false;
        dtpDe.Size = new Size(120, 23);
        dtpDe.TabIndex = 3;
        lblAte.Anchor = AnchorStyles.Left;
        lblAte.AutoSize = true;
        lblAte.Text = "Até:";
        dtpAte.Format = DateTimePickerFormat.Short;
        dtpAte.Name = "dtpAte";
        dtpAte.ShowCheckBox = true;
        dtpAte.Checked = false;
        dtpAte.Size = new Size(120, 23);
        dtpAte.TabIndex = 4;
        //
        // grade
        //
        grade.AllowUserToAddRows = false;
        grade.AllowUserToDeleteRows = false;
        grade.AllowUserToResizeRows = false;
        grade.BackgroundColor = SystemColors.Window;
        grade.BorderStyle = BorderStyle.None;
        grade.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grade.Dock = DockStyle.Fill;
        grade.MultiSelect = false;
        grade.Name = "grade";
        grade.ReadOnly = true;
        grade.RowHeadersVisible = false;
        grade.RowTemplate.Height = 26;
        grade.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grade.StandardTab = true;
        grade.TabIndex = 5;
        //
        // painelPaginacao
        //
        painelPaginacao.AutoSize = true;
        painelPaginacao.Controls.Add(btnProxima);
        painelPaginacao.Controls.Add(lblPagina);
        painelPaginacao.Controls.Add(btnAnterior);
        painelPaginacao.Dock = DockStyle.Bottom;
        painelPaginacao.FlowDirection = FlowDirection.RightToLeft;
        painelPaginacao.Name = "painelPaginacao";
        painelPaginacao.Padding = new Padding(8, 4, 8, 4);
        btnProxima.AutoSize = true;
        btnProxima.Name = "btnProxima";
        btnProxima.TabIndex = 7;
        btnProxima.Text = "Próxima ›";
        lblPagina.Anchor = AnchorStyles.Left;
        lblPagina.AutoSize = true;
        lblPagina.Margin = new Padding(12, 0, 12, 0);
        lblPagina.Name = "lblPagina";
        lblPagina.Text = "";
        btnAnterior.AutoSize = true;
        btnAnterior.Name = "btnAnterior";
        btnAnterior.TabIndex = 6;
        btnAnterior.Text = "‹ Anterior";
        //
        // rodape
        //
        rodape.Items.AddRange(new ToolStripItem[] { lblResumo });
        rodape.Name = "rodape";
        lblResumo.Name = "lblResumo";
        lblResumo.Text = "";
        //
        // FormPrincipal
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1180, 680);
        Controls.Add(grade);
        Controls.Add(painelPaginacao);
        Controls.Add(painelFiltros);
        Controls.Add(barra);
        Controls.Add(rodape);
        KeyPreview = true;
        MinimumSize = new Size(900, 500);
        Name = "FormPrincipal";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Protocolo";
        barra.ResumeLayout(false);
        barra.PerformLayout();
        painelFiltros.ResumeLayout(false);
        painelFiltros.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)grade).EndInit();
        painelPaginacao.ResumeLayout(false);
        painelPaginacao.PerformLayout();
        rodape.ResumeLayout(false);
        rodape.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private ToolStrip barra;
    private ToolStripButton btnNovo;
    private ToolStripButton btnAbrir;
    private ToolStripSeparator separador;
    private ToolStripButton btnExportar;
    private ToolStripButton btnAtualizar;
    private FlowLayoutPanel painelFiltros;
    private Label lblBusca;
    private TextBox txtBusca;
    private Label lblStatusFiltro;
    private ComboBox cboStatus;
    private CheckBox chkAndamento;
    private Label lblDe;
    private DateTimePicker dtpDe;
    private Label lblAte;
    private DateTimePicker dtpAte;
    private DataGridView grade;
    private FlowLayoutPanel painelPaginacao;
    private Button btnProxima;
    private Label lblPagina;
    private Button btnAnterior;
    private StatusStrip rodape;
    private ToolStripStatusLabel lblResumo;
}
