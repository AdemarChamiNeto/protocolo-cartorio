using System.ComponentModel;
using Cartorio.Core.Apresentacao;
using Cartorio.Core.Dominio;
using Cartorio.Core.Servicos;

namespace Cartorio.WinForms.Forms;

/// <summary>Tela principal: fila do Livro 1 com busca, filtros, paginação e cores por situação do prazo.</summary>
public partial class FormPrincipal : Form, IListaProtocolosView
{
    private readonly ProtocoloService _servico;
    private readonly string _nomeCartorio;
    private readonly ListaProtocolosPresenter _presenter;
    private readonly BindingList<LinhaProtocolo> _linhas = new();
    private readonly System.Windows.Forms.Timer _debounceBusca = new() { Interval = 350 };

    public FormPrincipal(ProtocoloService servico, string nomeCartorio, string subtitulo)
    {
        InitializeComponent();
        _servico = servico;
        _nomeCartorio = nomeCartorio;
        _presenter = new ListaProtocolosPresenter(this, servico);
        Text = $"Protocolo — {nomeCartorio}" + (string.IsNullOrEmpty(subtitulo) ? "" : $"  [{subtitulo}]");

        ConfigurarGrade();
        cboStatus.Items.Add("Todos");
        foreach (var s in Enum.GetValues<StatusProtocolo>()) cboStatus.Items.Add(s.Descricao());
        cboStatus.SelectedIndex = 0;

        // filtros: busca com atraso (enquanto digita), o resto na hora
        txtBusca.TextChanged += (_, _) => { _debounceBusca.Stop(); _debounceBusca.Start(); };
        _debounceBusca.Tick += (_, _) => { _debounceBusca.Stop(); _presenter.Buscar(); };
        cboStatus.SelectedIndexChanged += (_, _) => _presenter.Buscar();
        chkAndamento.CheckedChanged += (_, _) => _presenter.Buscar();
        dtpDe.ValueChanged += (_, _) => _presenter.Buscar();
        dtpAte.ValueChanged += (_, _) => _presenter.Buscar();

        btnNovo.Click += (_, _) => NovoProtocolo();
        btnAbrir.Click += (_, _) => AbrirSelecionado();
        btnExportar.Click += (_, _) => Exportar();
        btnAtualizar.Click += (_, _) => _presenter.Recarregar();
        btnAnterior.Click += (_, _) => _presenter.PaginaAnterior();
        btnProxima.Click += (_, _) => _presenter.ProximaPagina();
        grade.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) AbrirSelecionado(); };
        grade.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; AbrirSelecionado(); } };
        KeyDown += AtalhosDeTeclado;
        Load += (_, _) => _presenter.Iniciar();
        FormClosed += (_, _) => _debounceBusca.Dispose();
    }

    // ---- IListaProtocolosView ----
    public string TextoBusca => txtBusca.Text;
    public StatusProtocolo? StatusSelecionado => cboStatus.SelectedIndex <= 0 ? null : Enum.GetValues<StatusProtocolo>()[cboStatus.SelectedIndex - 1];
    public bool SoEmAndamento => chkAndamento.Checked;
    public DateOnly? De => dtpDe.Checked ? DateOnly.FromDateTime(dtpDe.Value) : null;
    public DateOnly? Ate => dtpAte.Checked ? DateOnly.FromDateTime(dtpAte.Value) : null;

    public void MostrarProtocolos(IReadOnlyList<LinhaProtocolo> linhas, string rodape, bool temAnterior, bool temProxima)
    {
        var selecionado = Selecionado()?.Id;
        _linhas.RaiseListChangedEvents = false;
        _linhas.Clear();
        foreach (var l in linhas) _linhas.Add(l);
        _linhas.RaiseListChangedEvents = true;
        _linhas.ResetBindings();

        // mantém a seleção depois de recarregar
        foreach (DataGridViewRow row in grade.Rows)
            if (row.DataBoundItem is LinhaProtocolo l && l.Id == selecionado) { row.Selected = true; grade.CurrentCell = row.Cells[0]; }

        lblPagina.Text = rodape;
        btnAnterior.Enabled = temAnterior;
        btnProxima.Enabled = temProxima;
    }

    public void MostrarResumo(string resumo) => lblResumo.Text = resumo;

    public void MostrarAviso(string mensagem) => MessageBox.Show(this, mensagem, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public void MostrarErro(string mensagem) => MessageBox.Show(this, mensagem, "Protocolo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    // ---- ações ----

    private void NovoProtocolo()
    {
        using var f = new FormNovoProtocolo(_servico, _nomeCartorio);
        if (f.ShowDialog(this) == DialogResult.OK) _presenter.Buscar();
    }

    private void AbrirSelecionado()
    {
        if (Selecionado() is not { } linha) return;
        using var f = new FormDetalhe(_servico, linha.Id, _nomeCartorio);
        f.ShowDialog(this);
        if (f.Alterado) _presenter.Recarregar();
    }

    private void Exportar()
    {
        using var dialogo = new SaveFileDialog
        {
            Filter = "CSV (Excel)|*.csv",
            FileName = $"protocolos-{DateTime.Now:yyyy-MM-dd}.csv",
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            File.WriteAllBytes(dialogo.FileName, _presenter.ExportarCsv());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DominioException)
        {
            MostrarErro(ex.Message);
        }
    }

    private void AtalhosDeTeclado(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F2: NovoProtocolo(); break;
            case Keys.F5: _presenter.Recarregar(); break;
            case Keys.F when e.Control: txtBusca.Focus(); txtBusca.SelectAll(); break;
            default: return;
        }
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private LinhaProtocolo? Selecionado() => grade.CurrentRow?.DataBoundItem as LinhaProtocolo;

    private void ConfigurarGrade()
    {
        grade.AutoGenerateColumns = false;
        void Coluna(string propriedade, string titulo, int largura, bool preencher = false, DataGridViewContentAlignment alinhamento = DataGridViewContentAlignment.MiddleLeft)
        {
            grade.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = propriedade,
                HeaderText = titulo,
                Width = largura,
                AutoSizeMode = preencher ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = { Alignment = alinhamento },
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
        }
        Coluna(nameof(LinhaProtocolo.Numero), "Nº", 70, alinhamento: DataGridViewContentAlignment.MiddleRight);
        Coluna(nameof(LinhaProtocolo.Data), "Prenotado em", 120);
        Coluna(nameof(LinhaProtocolo.Apresentante), "Apresentante", 200, preencher: true);
        Coluna(nameof(LinhaProtocolo.Natureza), "Natureza", 210);
        Coluna(nameof(LinhaProtocolo.Matricula), "Matrícula", 85, alinhamento: DataGridViewContentAlignment.MiddleRight);
        Coluna(nameof(LinhaProtocolo.Status), "Status", 110);
        Coluna(nameof(LinhaProtocolo.Prazo), "Prazo", 150);
        grade.DataSource = _linhas;

        // cor da linha conforme o prazo: o escrevente bate o olho e vê o que está apertado
        grade.RowPrePaint += (_, e) =>
        {
            if (grade.Rows[e.RowIndex].DataBoundItem is not LinhaProtocolo l) return;
            var estilo = grade.Rows[e.RowIndex].DefaultCellStyle;
            (estilo.BackColor, estilo.ForeColor) = l.Situacao switch
            {
                SituacaoPrazo.Vencido => (Color.MistyRose, Color.DarkRed),
                SituacaoPrazo.Vencendo => (Color.LemonChiffon, Color.SaddleBrown),
                SituacaoPrazo.Concluido => (SystemColors.Window, SystemColors.GrayText),
                _ => (SystemColors.Window, SystemColors.ControlText),
            };
        };
    }
}
