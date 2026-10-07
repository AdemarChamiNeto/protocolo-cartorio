using Cartorio.Core.Apresentacao;
using Cartorio.Core.Dominio;
using Cartorio.Core.Servicos;
using Cartorio.WinForms.Impressao;
using Cartorio.Core.Relatorios;

namespace Cartorio.WinForms.Forms;

/// <summary>Prenotação de um título. A validação e a gravação ficam no <see cref="NovoProtocoloPresenter"/>.</summary>
public partial class FormNovoProtocolo : Form, INovoProtocoloView
{
    private readonly NovoProtocoloPresenter _presenter;
    private readonly string _nomeCartorio;

    public FormNovoProtocolo(ProtocoloService servico, string nomeCartorio)
    {
        InitializeComponent();
        _nomeCartorio = nomeCartorio;
        _presenter = new NovoProtocoloPresenter(this, servico);

        cboNatureza.DisplayMember = "Texto";
        cboNatureza.ValueMember = "Valor";
        cboNatureza.DataSource = NovoProtocoloPresenter.Naturezas.Select(n => new { n.Valor, n.Texto }).ToList();
        // o binding seleciona o 1º item quando o handle é criado; a natureza deve ser escolhida de propósito
        Load += (_, _) => cboNatureza.SelectedIndex = -1;

        txtDocumento.Leave += (_, _) => _presenter.FormatarDocumento();
        btnSalvar.Click += (_, _) => _presenter.Salvar();
    }

    public Protocolo? Criado { get; private set; }

    // ---- INovoProtocoloView ----
    public string Nome => txtNome.Text;
    public string Documento { get => txtDocumento.Text; set => txtDocumento.Text = value; }
    public string Telefone => txtTelefone.Text;
    public NaturezaTitulo? Natureza => cboNatureza.SelectedValue is NaturezaTitulo n ? n : null;
    public string Matricula => txtMatricula.Text;
    public string Observacoes => txtObservacoes.Text;

    public void LimparErros()
    {
        foreach (var c in new Control[] { txtNome, txtDocumento, cboNatureza, txtMatricula }) erros.SetError(c, "");
    }

    public void MostrarErros(IReadOnlyDictionary<string, string> lista)
    {
        var campos = new Dictionary<string, Control>
        {
            ["Nome"] = txtNome, ["Documento"] = txtDocumento, ["Natureza"] = cboNatureza, ["Matricula"] = txtMatricula,
        };
        foreach (var (campo, mensagem) in lista)
            if (campos.TryGetValue(campo, out var c)) erros.SetError(c, mensagem);
        // foco no primeiro campo com erro, na ordem da tela
        campos.Values.FirstOrDefault(c => erros.GetError(c).Length > 0)?.Focus();
    }

    public void Concluir(Protocolo criado)
    {
        Criado = criado;
        var r = MessageBox.Show(this,
            $"Protocolo nº {criado.Numero} lançado.\n\nPrazo para registro: {criado.PrazoRegistro:dd/MM/yyyy}\n\nImprimir o comprovante para o apresentante?",
            "Prenotação", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (r == DialogResult.Yes)
            ImpressorRecibo.Visualizar(this, ReciboProtocolo.Montar(criado, _nomeCartorio), $"Protocolo {criado.Numero}");
        DialogResult = DialogResult.OK;
        Close();
    }

    public void MostrarErro(string mensagem) => MessageBox.Show(this, mensagem, "Prenotação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
