using System.Text.Json;
using Cartorio.Core.Dominio;

namespace Cartorio.WinForms;

/// <summary>config.json ao lado do executável. "Modo": "SqlServer" ou "Memoria" (demonstração, sem banco).</summary>
public sealed class Configuracao
{
    public string Modo { get; init; } = "SqlServer";
    public string ConnectionString { get; init; } = "";
    public string NomeCartorio { get; init; } = "Registro de Imóveis";
    public RegrasPrazo Prazos { get; init; } = new();

    public bool ModoDemonstracao => string.Equals(Modo, "Memoria", StringComparison.OrdinalIgnoreCase);

    public static Configuracao Carregar(string caminho)
    {
        if (!File.Exists(caminho)) return new Configuracao { Modo = "Memoria" };
        var json = File.ReadAllText(caminho);
        return JsonSerializer.Deserialize<Configuracao>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        }) ?? new Configuracao();
    }
}
