namespace Cartorio.WinForms.Forms;

partial class FormNovoProtocolo
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        tabela = new TableLayoutPanel();
        lblNome = new Label();
        txtNome = new TextBox();
        lblDocumento = new Label();
        txtDocumento = new TextBox();
        lblTelefone = new Label();
        txtTelefone = new TextBox();
        lblNatureza = new Label();
        cboNatureza = new ComboBox();
        lblMatricula = new Label();
        txtMatricula = new TextBox();
        lblObservacoes = new Label();
        txtObservacoes = new TextBox();
        painelBotoes = new FlowLayoutPanel();
        btnCancelar = new Button();
        btnSalvar = new Button();
        erros = new ErrorProvider(components);
        tabela.SuspendLayout();
        painelBotoes.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)erros).BeginInit();
        SuspendLayout();
        //
        // tabela
        //
        tabela.ColumnCount = 2;
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tabela.Controls.Add(lblNome, 0, 0);
        tabela.Controls.Add(txtNome, 1, 0);
        tabela.Controls.Add(lblDocumento, 0, 1);
        tabela.Controls.Add(txtDocumento, 1, 1);
        tabela.Controls.Add(lblTelefone, 0, 2);
        tabela.Controls.Add(txtTelefone, 1, 2);
        tabela.Controls.Add(lblNatureza, 0, 3);
        tabela.Controls.Add(cboNatureza, 1, 3);
        tabela.Controls.Add(lblMatricula, 0, 4);
        tabela.Controls.Add(txtMatricula, 1, 4);
        tabela.Controls.Add(lblObservacoes, 0, 5);
        tabela.Controls.Add(txtObservacoes, 1, 5);
        tabela.Dock = DockStyle.Fill;
        tabela.Name = "tabela";
        tabela.Padding = new Padding(12, 12, 30, 0);
        tabela.RowCount = 6;
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        //
        // rótulos
        //
        lblNome.Anchor = AnchorStyles.Left;
        lblNome.AutoSize = true;
        lblNome.Text = "&Apresentante *";
        lblDocumento.Anchor = AnchorStyles.Left;
        lblDocumento.AutoSize = true;
        lblDocumento.Text = "&CPF / CNPJ *";
        lblTelefone.Anchor = AnchorStyles.Left;
        lblTelefone.AutoSize = true;
        lblTelefone.Text = "&Telefone";
        lblNatureza.Anchor = AnchorStyles.Left;
        lblNatureza.AutoSize = true;
        lblNatureza.Text = "&Natureza do título *";
        lblMatricula.Anchor = AnchorStyles.Left;
        lblMatricula.AutoSize = true;
        lblMatricula.Text = "&Matrícula";
        lblObservacoes.AutoSize = true;
        lblObservacoes.Margin = new Padding(3, 8, 3, 0);
        lblObservacoes.Text = "&Observações";
        //
        // campos
        //
        txtNome.Dock = DockStyle.Fill;
        txtNome.MaxLength = 150;
        txtNome.Name = "txtNome";
        txtNome.TabIndex = 0;
        txtDocumento.Dock = DockStyle.Fill;
        txtDocumento.MaxLength = 20;
        txtDocumento.Name = "txtDocumento";
        txtDocumento.PlaceholderText = "000.000.000-00 ou 00.000.000/0000-00 (aceita CNPJ alfanumérico)";
        txtDocumento.TabIndex = 1;
        txtTelefone.Dock = DockStyle.Fill;
        txtTelefone.MaxLength = 20;
        txtTelefone.Name = "txtTelefone";
        txtTelefone.TabIndex = 2;
        cboNatureza.Dock = DockStyle.Fill;
        cboNatureza.DropDownStyle = ComboBoxStyle.DropDownList;
        cboNatureza.Name = "cboNatureza";
        cboNatureza.TabIndex = 3;
        txtMatricula.Dock = DockStyle.Fill;
        txtMatricula.MaxLength = 15;
        txtMatricula.Name = "txtMatricula";
        txtMatricula.PlaceholderText = "Se já souber (ex.: 12.345)";
        txtMatricula.TabIndex = 4;
        txtObservacoes.Dock = DockStyle.Fill;
        txtObservacoes.MaxLength = 500;
        txtObservacoes.Multiline = true;
        txtObservacoes.Name = "txtObservacoes";
        txtObservacoes.ScrollBars = ScrollBars.Vertical;
        txtObservacoes.TabIndex = 5;
        //
        // painelBotoes
        //
        painelBotoes.Controls.Add(btnCancelar);
        painelBotoes.Controls.Add(btnSalvar);
        painelBotoes.Dock = DockStyle.Bottom;
        painelBotoes.FlowDirection = FlowDirection.RightToLeft;
        painelBotoes.Height = 50;
        painelBotoes.Name = "painelBotoes";
        painelBotoes.Padding = new Padding(8);
        btnCancelar.DialogResult = DialogResult.Cancel;
        btnCancelar.Name = "btnCancelar";
        btnCancelar.Size = new Size(100, 30);
        btnCancelar.TabIndex = 7;
        btnCancelar.Text = "Cancelar";
        btnSalvar.Name = "btnSalvar";
        btnSalvar.Size = new Size(140, 30);
        btnSalvar.TabIndex = 6;
        btnSalvar.Text = "&Prenotar (Enter)";
        //
        // erros
        //
        erros.BlinkStyle = ErrorBlinkStyle.NeverBlink;
        erros.ContainerControl = this;
        //
        // FormNovoProtocolo
        //
        AcceptButton = btnSalvar;
        CancelButton = btnCancelar;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(620, 380);
        Controls.Add(tabela);
        Controls.Add(painelBotoes);
        MinimumSize = new Size(560, 380);
        Name = "FormNovoProtocolo";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Nova prenotação — Livro 1";
        tabela.ResumeLayout(false);
        tabela.PerformLayout();
        painelBotoes.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)erros).EndInit();
        ResumeLayout(false);
    }

    private TableLayoutPanel tabela;
    private Label lblNome;
    private TextBox txtNome;
    private Label lblDocumento;
    private TextBox txtDocumento;
    private Label lblTelefone;
    private TextBox txtTelefone;
    private Label lblNatureza;
    private ComboBox cboNatureza;
    private Label lblMatricula;
    private TextBox txtMatricula;
    private Label lblObservacoes;
    private TextBox txtObservacoes;
    private FlowLayoutPanel painelBotoes;
    private Button btnCancelar;
    private Button btnSalvar;
    private ErrorProvider erros;
}
