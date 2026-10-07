namespace Cartorio.WinForms.Forms;

partial class FormEntradaTexto
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblInstrucao = new Label();
        txtValor = new TextBox();
        btnOk = new Button();
        btnCancelar = new Button();
        SuspendLayout();
        //
        // lblInstrucao
        //
        lblInstrucao.AutoSize = true;
        lblInstrucao.Location = new Point(12, 12);
        lblInstrucao.Name = "lblInstrucao";
        lblInstrucao.Text = "Instrução";
        //
        // txtValor
        //
        txtValor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtValor.Location = new Point(12, 36);
        txtValor.Name = "txtValor";
        txtValor.Size = new Size(456, 160);
        txtValor.TabIndex = 0;
        //
        // btnOk
        //
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(292, 210);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(85, 30);
        btnOk.TabIndex = 1;
        btnOk.Text = "OK";
        //
        // btnCancelar
        //
        btnCancelar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancelar.DialogResult = DialogResult.Cancel;
        btnCancelar.Location = new Point(383, 210);
        btnCancelar.Name = "btnCancelar";
        btnCancelar.Size = new Size(85, 30);
        btnCancelar.TabIndex = 2;
        btnCancelar.Text = "Cancelar";
        //
        // FormEntradaTexto
        //
        AcceptButton = btnOk;
        CancelButton = btnCancelar;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(480, 252);
        Controls.Add(lblInstrucao);
        Controls.Add(txtValor);
        Controls.Add(btnOk);
        Controls.Add(btnCancelar);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "FormEntradaTexto";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ResumeLayout(false);
        PerformLayout();
    }

    private Label lblInstrucao;
    private TextBox txtValor;
    private Button btnOk;
    private Button btnCancelar;
}
