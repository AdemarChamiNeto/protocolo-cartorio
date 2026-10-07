using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Cartorio.Data;

/// <summary>Aplica os scripts SQL embutidos (Scripts/*.sql) em ordem. Os scripts são idempotentes.</summary>
public static partial class BancoDeDados
{
    public static void Atualizar(Func<DbConnection> abrirConexao)
    {
        using var con = abrirConexao();
        con.Open();
        var asm = Assembly.GetExecutingAssembly();
        foreach (var nome in asm.GetManifestResourceNames().Where(n => n.EndsWith(".sql")).Order())
        {
            using var stream = asm.GetManifestResourceStream(nome)!;
            using var leitor = new StreamReader(stream);
            foreach (var lote in Lotes(leitor.ReadToEnd()))
            {
                using var cmd = con.CreateCommand();
                cmd.CommandText = lote;
                cmd.ExecuteNonQuery();
            }
        }
    }

    /// <summary>Separa o script nos "GO" (que é comando do SSMS/sqlcmd, não T-SQL).</summary>
    public static IEnumerable<string> Lotes(string script) =>
        Go().Split(script).Select(l => l.Trim()).Where(l => l.Length > 0 && !SoComentario().IsMatch(l));

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex Go();

    [GeneratedRegex(@"^(\s*--[^\n]*\n?)+$")]
    private static partial Regex SoComentario();
}
