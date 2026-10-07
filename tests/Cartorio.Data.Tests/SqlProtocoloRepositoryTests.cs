using Cartorio.Core.Dominio;
using Cartorio.Core.Servicos;
using Microsoft.Data.SqlClient;

namespace Cartorio.Data.Tests;

/// <summary>
/// Integração com SQL Server de verdade. Defina CARTORIO_TEST_SQL com a connection string de um banco vazio
/// (no CI sobe um container do SQL Server 2022). Cada teste limpa as tabelas antes de rodar.
/// </summary>
public sealed class SqlProtocoloRepositoryTests
{
    private static readonly string Cs = Environment.GetEnvironmentVariable("CARTORIO_TEST_SQL")
        ?? throw new InvalidOperationException("Defina CARTORIO_TEST_SQL para rodar os testes de integração.");

    private readonly SqlProtocoloRepository _repo;
    private readonly ProtocoloService _servico;
    private readonly RelogioFixo _relogio = new(new DateTime(2026, 10, 7, 9, 30, 0));

    static SqlProtocoloRepositoryTests()
    {
        // cria o banco de testes se ainda não existir (conectando no master)
        var builder = new SqlConnectionStringBuilder(Cs);
        var banco = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        using var con = new SqlConnection(builder.ConnectionString);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "IF DB_ID(@b) IS NULL BEGIN DECLARE @sql nvarchar(400) = N'CREATE DATABASE ' + QUOTENAME(@b); EXEC (@sql); END";
        cmd.Parameters.AddWithValue("@b", banco);
        cmd.ExecuteNonQuery();
    }

    public SqlProtocoloRepositoryTests()
    {
        BancoDeDados.Atualizar(Conexao);
        BancoDeDados.Atualizar(Conexao); // idempotente
        using (var con = Conexao())
        {
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = """
                DELETE FROM dbo.Eventos; DELETE FROM dbo.Exigencias; DELETE FROM dbo.Protocolos; DELETE FROM dbo.FeriadosLocais;
                UPDATE dbo.Contadores SET Valor = 0 WHERE Nome = 'Protocolo';
                """;
            cmd.ExecuteNonQuery();
        }
        _repo = new SqlProtocoloRepository(Conexao);
        _servico = new ProtocoloService(_repo, _relogio, "teste");
    }

    private static SqlConnection Conexao() => new(Cs);

    private Protocolo Novo(string nome = "Maria Silva", string doc = "52998224725", string? matricula = "12.345") =>
        _servico.Prenotar(new Apresentante(nome, doc, "(42) 3333-0000"), NaturezaTitulo.CompraEVenda, matricula, "obs");

    [Fact]
    public void InsereELeOAgregadoCompleto()
    {
        var p = Novo();
        Assert.Equal(1, p.Numero);
        Assert.NotNull(p.Versao);

        var lido = _repo.ObterPorId(p.Id)!;
        Assert.Equal(p.Numero, lido.Numero);
        Assert.Equal(new DateTime(2026, 10, 7, 9, 30, 0), lido.PrenotadoEm);
        Assert.Equal("Maria Silva", lido.Apresentante.Nome);
        Assert.Equal("52998224725", lido.Apresentante.Documento);
        Assert.Equal("(42) 3333-0000", lido.Apresentante.Telefone);
        Assert.Equal("12.345", lido.Matricula);
        Assert.Equal(new DateOnly(2026, 10, 22), lido.PrazoRegistro);
        Assert.Equal(new DateOnly(2026, 11, 6), lido.VencimentoPrenotacao);
        Assert.Single(lido.Eventos);
        Assert.Null(_repo.ObterPorId(999_999));
    }

    [Fact]
    public void NumeracaoSemBuracos()
    {
        var numeros = Enumerable.Range(0, 5).Select(i => Novo($"Pessoa {i}").Numero).ToList();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, numeros);
    }

    [Fact]
    public void NumeracaoConcorrenteNaoRepete()
    {
        // 4 "estações" protocolando ao mesmo tempo
        var numeros = new System.Collections.Concurrent.ConcurrentBag<int>();
        Parallel.For(0, 20, new ParallelOptions { MaxDegreeOfParallelism = 4 }, i =>
        {
            var servico = new ProtocoloService(new SqlProtocoloRepository(Conexao), _relogio, $"estacao{i % 4}");
            numeros.Add(servico.Prenotar(new Apresentante($"Pessoa {i}", "52998224725", null), NaturezaTitulo.Doacao, null, null).Numero);
        });
        Assert.Equal(Enumerable.Range(1, 20), numeros.Order());
    }

    [Fact]
    public void GravaExigenciasEventosERegistro()
    {
        var p = Novo();
        _servico.IniciarAnalise(p.Id);
        _servico.EmitirExigencias(p.Id, ["Apresentar certidão de ônus reais.", "Comprovar o pagamento do ITBI."]);
        _relogio.Agora = _relogio.Agora.AddDays(2);
        _servico.Reapresentar(p.Id);
        _servico.Registrar(p.Id, "R-4", null);

        var lido = _repo.ObterPorId(p.Id)!;
        Assert.Equal(StatusProtocolo.Registrado, lido.Status);
        Assert.Equal("R-4/12.345", lido.Ato);
        Assert.Equal(2, lido.Exigencias.Count);
        Assert.All(lido.Exigencias, e => Assert.Equal(new DateTime(2026, 10, 9, 9, 30, 0), e.CumpridaEm));
        Assert.Equal(5, lido.Eventos.Count);
        Assert.Equal("Registrado: R-4/12.345", lido.Eventos[^1].Descricao);
    }

    [Fact]
    public void ConcorrenciaOtimistaBloqueiaGravacaoSobreVersaoVelha()
    {
        var id = Novo().Id;
        var estacaoA = _repo.ObterPorId(id)!;
        var estacaoB = _repo.ObterPorId(id)!;

        estacaoA.IniciarAnalise(_relogio.Agora, "a");
        _repo.Atualizar(estacaoA);

        estacaoB.Cancelar("Desistência", _relogio.Agora, "b");
        var erro = Assert.Throws<DominioException>(() => _repo.Atualizar(estacaoB));
        Assert.Contains("outra estação", erro.Message);
        Assert.Equal(StatusProtocolo.EmAnalise, _repo.ObterPorId(id)!.Status);
        Assert.Equal(2, _repo.ObterPorId(id)!.Eventos.Count); // o evento da estação B não foi gravado (rollback)
    }

    [Fact]
    public void BuscaFiltraEPagina()
    {
        Novo("Maria Silva", "52998224725", "12.345");
        Novo("José Santos", "11222333000181", "999");
        var c = Novo("Ana 50% Souza", "11144477735", null);
        _servico.Cancelar(c.Id, "Desistência");

        int Total(FiltroProtocolos f) => _repo.Buscar(f).Total;
        Assert.Equal(3, Total(new()));
        Assert.Equal(1, Total(new(Texto: "maria")));
        Assert.Equal(1, Total(new(Texto: "2")));            // número do protocolo
        Assert.Equal(1, Total(new(Texto: "11.222.333")));   // documento parcial
        Assert.Equal(1, Total(new(Texto: "12345")));        // matrícula sem ponto
        Assert.Equal(1, Total(new(Texto: "50%")));          // % é literal, não curinga
        Assert.Equal(2, Total(new(SoEmAndamento: true)));
        Assert.Equal(1, Total(new(Status: StatusProtocolo.Cancelado)));
        Assert.Equal(3, Total(new(De: new DateOnly(2026, 10, 7), Ate: new DateOnly(2026, 10, 7))));
        Assert.Equal(0, Total(new(De: new DateOnly(2026, 10, 8))));

        var pagina2 = _repo.Buscar(new FiltroProtocolos(Pagina: 2, TamanhoPagina: 2));
        Assert.Equal(3, pagina2.Total);
        Assert.Equal(1, Assert.Single(pagina2.Itens).Numero); // ordem: mais recente primeiro
    }

    [Fact]
    public void RotinaDeVencimentoEContagem()
    {
        var p = Novo();
        _servico.IniciarAnalise(p.Id);
        _servico.EmitirExigencias(p.Id, ["Apresentar certidão atualizada."]);
        Novo("Outro");

        Assert.Empty(_repo.ComPrenotacaoVencida(new DateOnly(2026, 11, 6)));
        var vencidos = _repo.ComPrenotacaoVencida(new DateOnly(2026, 11, 9));
        Assert.Equal(p.Id, Assert.Single(vencidos).Id);
        Assert.Single(vencidos[0].Exigencias); // vem com os filhos

        _relogio.Agora = new DateTime(2026, 11, 9, 8, 0, 0);
        Assert.Equal(new[] { 1 }, _servico.CancelarPrenotacoesVencidas().ToArray());
        var contagem = _repo.ContarPorStatus();
        Assert.Equal(1, contagem[StatusProtocolo.Cancelado]);
        Assert.Equal(1, contagem[StatusProtocolo.Prenotado]);
    }

    [Fact]
    public void FeriadoLocalAfetaOsPrazos()
    {
        _repo.AdicionarFeriadoLocal(new DateOnly(2026, 10, 8), "Aniversário da cidade (exemplo)");
        _repo.AdicionarFeriadoLocal(new DateOnly(2026, 10, 8), "duplicado é ignorado");
        Assert.Equal(new[] { new DateOnly(2026, 10, 8) }, _repo.FeriadosLocais());

        var servico = new ProtocoloService(_repo, _relogio, "teste"); // lê os feriados ao criar
        var p = servico.Prenotar(new Apresentante("Maria Silva", "52998224725", null), NaturezaTitulo.Doacao, null, null);
        Assert.Equal(new DateOnly(2026, 10, 23), p.PrazoRegistro);
    }

    private sealed class RelogioFixo(DateTime agora) : IRelogio
    {
        public DateTime Agora { get; set; } = agora;
    }
}
