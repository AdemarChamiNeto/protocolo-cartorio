using System.Data.Common;
using Cartorio.Core.Infra;
using Cartorio.Core.Servicos;
using Cartorio.Data;
using Cartorio.WinForms.Forms;
using Microsoft.Data.SqlClient;

namespace Cartorio.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Erro("Erro inesperado", e.Exception);

        var config = Configuracao.Carregar(Path.Combine(AppContext.BaseDirectory, "config.json"));
        var repo = CriarRepositorio(config, demo: config.ModoDemonstracao || args.Contains("--demo"), out var titulo);
        if (repo is null) return;

        var servico = new ProtocoloService(repo, new RelogioDoSistema(), Environment.UserName, config.Prazos);
        Application.Run(new FormPrincipal(servico, config.NomeCartorio, titulo));
    }

    private static IProtocoloRepository? CriarRepositorio(Configuracao config, bool demo, out string titulo)
    {
        if (!demo)
        {
            try
            {
                CriarBancoSeNaoExistir(config.ConnectionString);
                DbConnection Abrir() => new SqlConnection(config.ConnectionString);
                BancoDeDados.Atualizar(Abrir);
                titulo = new SqlConnectionStringBuilder(config.ConnectionString).InitialCatalog;
                return new SqlProtocoloRepository(Abrir);
            }
            catch (Exception ex) when (ex is SqlException or InvalidOperationException or ArgumentException)
            {
                var resposta = MessageBox.Show(
                    $"Não foi possível conectar ao SQL Server:\n\n{ex.Message}\n\nConfira a ConnectionString no config.json.\n\nAbrir em modo demonstração (dados fictícios em memória)?",
                    "Banco de dados", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (resposta != DialogResult.Yes) { titulo = ""; return null; }
            }
        }
        var memoria = new RepositorioEmMemoria();
        DadosDemonstracao.Popular(memoria, DateTime.Now);
        titulo = "MODO DEMONSTRAÇÃO — dados fictícios, nada é gravado";
        return memoria;
    }

    /// <summary>Na primeira execução cria o banco (útil com o LocalDB que vem com o Visual Studio).</summary>
    private static void CriarBancoSeNaoExistir(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var banco = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(banco)) throw new ArgumentException("A ConnectionString precisa de Database=...");
        builder.InitialCatalog = "master";
        using var con = new SqlConnection(builder.ConnectionString);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "IF DB_ID(@banco) IS NULL BEGIN DECLARE @sql nvarchar(400) = N'CREATE DATABASE ' + QUOTENAME(@banco); EXEC (@sql); END";
        cmd.Parameters.AddWithValue("@banco", banco);
        cmd.ExecuteNonQuery();
    }

    internal static void Erro(string titulo, Exception ex) =>
        MessageBox.Show(ex.Message, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
}
