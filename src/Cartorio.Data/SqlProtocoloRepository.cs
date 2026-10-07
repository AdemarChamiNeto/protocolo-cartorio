using System.Data;
using System.Data.Common;
using Cartorio.Core.Dominio;
using Cartorio.Core.Servicos;

namespace Cartorio.Data;

/// <summary>
/// Repositório em ADO.NET puro para SQL Server. Recebe uma fábrica de conexões, então este projeto
/// não depende do driver: o app passa <c>() =&gt; new SqlConnection(cs)</c>.
/// </summary>
public sealed class SqlProtocoloRepository(Func<DbConnection> abrirConexao) : IProtocoloRepository
{
    private const string Colunas = """
        p.Id, p.Numero, p.PrenotadoEm, p.ApresentanteNome, p.ApresentanteDocumento, p.ApresentanteTelefone, p.Natureza,
        p.Matricula, p.Observacoes, p.Status, p.PrazoRegistro, p.VencimentoPrenotacao, p.Ato, p.MotivoCancelamento,
        p.ConcluidoEm, p.Versao
        """;

    public void Inserir(Protocolo p)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction(IsolationLevel.ReadCommitted);

        // Livro 1 não pode ter buraco na numeração: o contador é incrementado na MESMA transação do insert
        // (uma SEQUENCE perderia números em caso de rollback). O UPDATE trava a linha até o commit,
        // então duas estações nunca pegam o mesmo número.
        p.Numero = (int)Escalar(con, tx, "UPDATE dbo.Contadores SET Valor = Valor + 1 OUTPUT inserted.Valor WHERE Nome = 'Protocolo'")!;

        using (var cmd = Comando(con, tx, """
            INSERT INTO dbo.Protocolos
                (Numero, PrenotadoEm, ApresentanteNome, ApresentanteDocumento, ApresentanteTelefone, Natureza, Matricula,
                 Observacoes, Status, PrazoRegistro, VencimentoPrenotacao, Ato, MotivoCancelamento, ConcluidoEm)
            OUTPUT inserted.Id, inserted.Versao
            VALUES (@Numero, @PrenotadoEm, @Nome, @Documento, @Telefone, @Natureza, @Matricula,
                    @Observacoes, @Status, @PrazoRegistro, @Vencimento, @Ato, @Motivo, @ConcluidoEm)
            """))
        {
            Param(cmd, "@Numero", p.Numero);
            Param(cmd, "@PrenotadoEm", p.PrenotadoEm);
            Param(cmd, "@Nome", p.Apresentante.Nome);
            Param(cmd, "@Documento", p.Apresentante.Documento);
            Param(cmd, "@Telefone", p.Apresentante.Telefone);
            ParamsComuns(cmd, p);
            using var r = cmd.ExecuteReader();
            r.Read();
            p.Id = r.GetInt32(0);
            p.Versao = (byte[])r.GetValue(1);
        }

        GravarFilhos(con, tx, p);
        tx.Commit();
    }

    public void Atualizar(Protocolo p)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction(IsolationLevel.ReadCommitted);

        using (var cmd = Comando(con, tx, """
            UPDATE dbo.Protocolos SET
                Natureza = @Natureza, Matricula = @Matricula, Observacoes = @Observacoes, Status = @Status,
                PrazoRegistro = @PrazoRegistro, VencimentoPrenotacao = @Vencimento, Ato = @Ato,
                MotivoCancelamento = @Motivo, ConcluidoEm = @ConcluidoEm
            OUTPUT inserted.Versao
            WHERE Id = @Id AND Versao = @Versao
            """))
        {
            Param(cmd, "@Id", p.Id);
            Param(cmd, "@Versao", p.Versao);
            ParamsComuns(cmd, p);
            // concorrência otimista: se outra estação gravou antes, a versão mudou e nenhuma linha é afetada
            p.Versao = cmd.ExecuteScalar() as byte[]
                ?? throw new DominioException($"O protocolo {p.Numero} foi alterado em outra estação. Feche e abra de novo para ver a versão atual.");
        }

        GravarFilhos(con, tx, p);
        tx.Commit();
    }

    public Protocolo? ObterPorId(int id)
    {
        using var con = Abrir();
        var p = Ler(con, $"SELECT {Colunas} FROM dbo.Protocolos p WHERE p.Id = @Id", c => Param(c, "@Id", id)).SingleOrDefault();
        return p is null ? null : ComFilhos(con, p);
    }

    public PaginaProtocolos Buscar(FiltroProtocolos f)
    {
        var where = new List<string>();
        var parametros = new List<(string, object?)>();
        if (f.Status is { } s) { where.Add("p.Status = @Status"); parametros.Add(("@Status", (byte)s)); }
        if (f.SoEmAndamento) where.Add("p.Status IN (1, 2, 3)");
        if (f.De is { } de) { where.Add("p.PrenotadoEm >= @De"); parametros.Add(("@De", de.ToDateTime(TimeOnly.MinValue))); }
        if (f.Ate is { } ate) { where.Add("p.PrenotadoEm < @Ate"); parametros.Add(("@Ate", ate.AddDays(1).ToDateTime(TimeOnly.MinValue))); }
        if (!string.IsNullOrWhiteSpace(f.Texto))
        {
            var t = f.Texto.Trim();
            var condicoes = new List<string> { "p.ApresentanteNome LIKE @Nome ESCAPE '!'" };
            parametros.Add(("@Nome", "%" + EscaparLike(t) + "%"));
            if (int.TryParse(t, out var numero)) { condicoes.Add("p.Numero = @Numero"); parametros.Add(("@Numero", numero)); }
            var doc = DocumentoFiscal.Limpar(t);
            if (doc.Length >= 3) { condicoes.Add("p.ApresentanteDocumento LIKE @Doc ESCAPE '!'"); parametros.Add(("@Doc", "%" + EscaparLike(doc) + "%")); }
            if (Protocolo.NormalizarMatricula(t) is { } mat) { condicoes.Add("p.Matricula = @Matricula"); parametros.Add(("@Matricula", mat)); }
            where.Add("(" + string.Join(" OR ", condicoes) + ")");
        }
        var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
        void Aplicar(DbCommand c) { foreach (var (n, v) in parametros) Param(c, n, v); }

        using var con = Abrir();
        var total = (int)Escalar(con, null, $"SELECT COUNT(*) FROM dbo.Protocolos p {whereSql}", Aplicar)!;
        var itens = Ler(con, $"""
            SELECT {Colunas} FROM dbo.Protocolos p {whereSql}
            ORDER BY p.Numero DESC
            OFFSET @Pular ROWS FETCH NEXT @Pegar ROWS ONLY
            """, c =>
        {
            Aplicar(c);
            Param(c, "@Pular", (long)(f.Pagina - 1) * f.TamanhoPagina);
            Param(c, "@Pegar", f.TamanhoPagina);
        });
        // a lista não precisa de exigências e histórico; o detalhe carrega tudo por ObterPorId
        return new PaginaProtocolos(itens, total);
    }

    public IReadOnlyList<Protocolo> ComPrenotacaoVencida(DateOnly hoje)
    {
        using var con = Abrir();
        return Ler(con, $"SELECT {Colunas} FROM dbo.Protocolos p WHERE p.Status = 3 AND p.VencimentoPrenotacao < @Hoje",
                c => Param(c, "@Hoje", hoje.ToDateTime(TimeOnly.MinValue)))
            .Select(p => ComFilhos(con, p)).ToList();
    }

    public IReadOnlyDictionary<StatusProtocolo, int> ContarPorStatus()
    {
        using var con = Abrir();
        using var cmd = Comando(con, null, "SELECT Status, COUNT(*) FROM dbo.Protocolos GROUP BY Status");
        using var r = cmd.ExecuteReader();
        var d = new Dictionary<StatusProtocolo, int>();
        while (r.Read()) d[(StatusProtocolo)r.GetByte(0)] = r.GetInt32(1);
        return d;
    }

    public IReadOnlyList<DateOnly> FeriadosLocais()
    {
        using var con = Abrir();
        using var cmd = Comando(con, null, "SELECT Data FROM dbo.FeriadosLocais");
        using var r = cmd.ExecuteReader();
        var lista = new List<DateOnly>();
        while (r.Read()) lista.Add(DateOnly.FromDateTime(r.GetDateTime(0)));
        return lista;
    }

    public void AdicionarFeriadoLocal(DateOnly data, string nome)
    {
        using var con = Abrir();
        using var cmd = Comando(con, null, """
            IF NOT EXISTS (SELECT 1 FROM dbo.FeriadosLocais WHERE Data = @Data)
                INSERT INTO dbo.FeriadosLocais (Data, Nome) VALUES (@Data, @Nome)
            """);
        Param(cmd, "@Data", data.ToDateTime(TimeOnly.MinValue));
        Param(cmd, "@Nome", nome);
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------

    /// <summary>Exigências novas são inseridas e as já gravadas têm o cumprimento atualizado; eventos são só acrescentados.</summary>
    private static void GravarFilhos(DbConnection con, DbTransaction tx, Protocolo p)
    {
        foreach (var e in p.Exigencias)
        {
            if (e.Id == 0)
            {
                e.Id = (int)Escalar(con, tx, """
                    INSERT INTO dbo.Exigencias (ProtocoloId, Texto, EmitidaEm, CumpridaEm)
                    OUTPUT inserted.Id VALUES (@P, @Texto, @Emitida, @Cumprida)
                    """, c => { Param(c, "@P", p.Id); Param(c, "@Texto", e.Texto); Param(c, "@Emitida", e.EmitidaEm); Param(c, "@Cumprida", e.CumpridaEm); })!;
            }
            else
            {
                Executar(con, tx, "UPDATE dbo.Exigencias SET CumpridaEm = @Cumprida WHERE Id = @Id AND ProtocoloId = @P",
                    c => { Param(c, "@Cumprida", e.CumpridaEm); Param(c, "@Id", e.Id); Param(c, "@P", p.Id); });
            }
        }
        foreach (var ev in p.Eventos.Where(ev => ev.Id == 0))
        {
            ev.Id = (int)Escalar(con, tx, """
                INSERT INTO dbo.Eventos (ProtocoloId, Quando, Usuario, Descricao)
                OUTPUT inserted.Id VALUES (@P, @Quando, @Usuario, @Descricao)
                """, c => { Param(c, "@P", p.Id); Param(c, "@Quando", ev.Quando); Param(c, "@Usuario", ev.Usuario); Param(c, "@Descricao", ev.Descricao); })!;
        }
    }

    private static Protocolo ComFilhos(DbConnection con, Protocolo p)
    {
        var exigencias = new List<Exigencia>();
        using (var cmd = Comando(con, null, "SELECT Id, Texto, EmitidaEm, CumpridaEm FROM dbo.Exigencias WHERE ProtocoloId = @P ORDER BY Id"))
        {
            Param(cmd, "@P", p.Id);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                exigencias.Add(new Exigencia { Id = r.GetInt32(0), Texto = r.GetString(1), EmitidaEm = r.GetDateTime(2), CumpridaEm = r.IsDBNull(3) ? null : r.GetDateTime(3) });
        }
        var eventos = new List<EventoProtocolo>();
        using (var cmd = Comando(con, null, "SELECT Id, Quando, Usuario, Descricao FROM dbo.Eventos WHERE ProtocoloId = @P ORDER BY Id"))
        {
            Param(cmd, "@P", p.Id);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                eventos.Add(new EventoProtocolo(r.GetDateTime(1), r.GetString(2), r.GetString(3)) { Id = r.GetInt32(0) });
        }
        var completo = Protocolo.Reconstituir(p.Id, p.Numero, p.PrenotadoEm, p.Apresentante, p.Natureza, p.Matricula, p.Observacoes,
            p.Status, p.PrazoRegistro, p.VencimentoPrenotacao, p.Ato, p.MotivoCancelamento, p.ConcluidoEm, exigencias, eventos);
        completo.Versao = p.Versao;
        return completo;
    }

    private static List<Protocolo> Ler(DbConnection con, string sql, Action<DbCommand> parametros)
    {
        using var cmd = Comando(con, null, sql);
        parametros(cmd);
        using var r = cmd.ExecuteReader();
        var lista = new List<Protocolo>();
        string? Texto(int i) => r.IsDBNull(i) ? null : r.GetString(i);
        while (r.Read())
        {
            var p = Protocolo.Reconstituir(
                id: r.GetInt32(0), numero: r.GetInt32(1), prenotadoEm: r.GetDateTime(2),
                apresentante: new Apresentante(r.GetString(3), r.GetString(4), Texto(5)),
                natureza: (NaturezaTitulo)r.GetByte(6), matricula: Texto(7), observacoes: Texto(8),
                status: (StatusProtocolo)r.GetByte(9),
                prazoRegistro: DateOnly.FromDateTime(r.GetDateTime(10)), vencimentoPrenotacao: DateOnly.FromDateTime(r.GetDateTime(11)),
                ato: Texto(12), motivoCancelamento: Texto(13), concluidoEm: r.IsDBNull(14) ? null : r.GetDateTime(14),
                exigencias: [], eventos: []);
            p.Versao = (byte[])r.GetValue(15);
            lista.Add(p);
        }
        return lista;
    }

    private static void ParamsComuns(DbCommand cmd, Protocolo p)
    {
        Param(cmd, "@Natureza", (byte)p.Natureza);
        Param(cmd, "@Matricula", p.Matricula);
        Param(cmd, "@Observacoes", p.Observacoes);
        Param(cmd, "@Status", (byte)p.Status);
        Param(cmd, "@PrazoRegistro", p.PrazoRegistro.ToDateTime(TimeOnly.MinValue));
        Param(cmd, "@Vencimento", p.VencimentoPrenotacao.ToDateTime(TimeOnly.MinValue));
        Param(cmd, "@Ato", p.Ato);
        Param(cmd, "@Motivo", p.MotivoCancelamento);
        Param(cmd, "@ConcluidoEm", p.ConcluidoEm);
    }

    private DbConnection Abrir()
    {
        var con = abrirConexao();
        con.Open();
        return con;
    }

    private static DbCommand Comando(DbConnection con, DbTransaction? tx, string sql)
    {
        var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        return cmd;
    }

    private static object? Escalar(DbConnection con, DbTransaction? tx, string sql, Action<DbCommand>? parametros = null)
    {
        using var cmd = Comando(con, tx, sql);
        parametros?.Invoke(cmd);
        return cmd.ExecuteScalar();
    }

    private static void Executar(DbConnection con, DbTransaction? tx, string sql, Action<DbCommand> parametros)
    {
        using var cmd = Comando(con, tx, sql);
        parametros(cmd);
        cmd.ExecuteNonQuery();
    }

    private static void Param(DbCommand cmd, string nome, object? valor)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = nome;
        p.Value = valor ?? DBNull.Value;
        if (valor is null) p.DbType = DbType.String; // tipo explícito para NULL
        if (valor is byte[]) p.DbType = DbType.Binary;
        cmd.Parameters.Add(p);
    }

    private static string EscaparLike(string s) => s.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_").Replace("[", "![");
}
