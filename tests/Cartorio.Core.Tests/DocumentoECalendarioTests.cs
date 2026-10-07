using Cartorio.Core.Dominio;

namespace Cartorio.Core.Tests;

public class DocumentoFiscalTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    public void CpfValido(string cpf) => Assert.True(DocumentoFiscal.CpfValido(cpf));

    [Theory]
    [InlineData("529.982.247-24")] // DV errado
    [InlineData("111.111.111-11")] // todos iguais passam no cálculo, mas são inválidos
    [InlineData("1234567890")]
    [InlineData("")]
    public void CpfInvalido(string cpf) => Assert.False(DocumentoFiscal.CpfValido(cpf));

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("12.ABC.345/01DE-35")] // exemplo de CNPJ alfanumérico da Receita
    [InlineData("12abc34501de35")]     // minúsculas e sem pontuação
    public void CnpjValido(string cnpj) => Assert.True(DocumentoFiscal.CnpjValido(cnpj));

    [Theory]
    [InlineData("11.222.333/0001-82")]
    [InlineData("12.ABC.345/01DE-36")]
    [InlineData("12.ABC.345/01DE-3A")] // DV tem que ser número
    [InlineData("00.000.000/0000-00")]
    public void CnpjInvalido(string cnpj) => Assert.False(DocumentoFiscal.CnpjValido(cnpj));

    [Fact]
    public void FormataEIdentifica()
    {
        Assert.Equal("529.982.247-25", DocumentoFiscal.Formatar("52998224725"));
        Assert.Equal("12.ABC.345/01DE-35", DocumentoFiscal.Formatar("12abc34501de35"));
        Assert.Equal("123", DocumentoFiscal.Formatar("123"));
        Assert.Equal(TipoDocumento.Cnpj, DocumentoFiscal.Identificar("11222333000181"));
        Assert.Null(DocumentoFiscal.Identificar("abc"));
    }
}

public class CalendarioUtilTests
{
    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2038, 4, 25)]
    public void Pascoa(int ano, int mes, int dia) => Assert.Equal(new DateOnly(ano, mes, dia), FeriadosNacionais.Pascoa(ano));

    [Fact]
    public void FeriadosNacionaisDe2026()
    {
        var f = FeriadosNacionais.DoAno(2026);
        Assert.Contains(f, x => x.Data == new DateOnly(2026, 4, 3) && x.Nome == "Sexta-feira Santa");
        Assert.Contains(f, x => x.Data == new DateOnly(2026, 11, 20));
        Assert.DoesNotContain(f, x => x.Nome.Contains("Carnaval"));
        Assert.Equal(10, f.Count);
        Assert.DoesNotContain(FeriadosNacionais.DoAno(2023), x => x.Data.Month == 11 && x.Data.Day == 20);
    }

    [Fact]
    public void DiaUtil()
    {
        var cal = new CalendarioUtil([new DateOnly(2026, 6, 4)]); // Corpus Christi como feriado local
        Assert.True(cal.EhDiaUtil(new DateOnly(2026, 10, 7)));   // quarta
        Assert.False(cal.EhDiaUtil(new DateOnly(2026, 10, 10))); // sábado
        Assert.False(cal.EhDiaUtil(new DateOnly(2026, 10, 12))); // Aparecida
        Assert.False(cal.EhDiaUtil(new DateOnly(2026, 6, 4)));   // local
        Assert.Equal("Feriado local", cal.NomeFeriado(new DateOnly(2026, 6, 4)));
        Assert.Equal("Finados", cal.NomeFeriado(new DateOnly(2026, 11, 2)));
    }

    [Theory]
    [InlineData("2026-10-07", 1, "2026-10-08")]  // quarta + 1 = quinta (o próprio dia não conta)
    [InlineData("2026-10-09", 1, "2026-10-13")]  // sexta + 1 pula fim de semana e o feriado de segunda
    [InlineData("2026-10-10", 1, "2026-10-13")]  // protocolado no sábado
    [InlineData("2026-10-07", 10, "2026-10-22")] // 10 dias úteis com o feriado de 12/10 no meio
    [InlineData("2026-12-23", 3, "2026-12-29")]  // Natal
    [InlineData("2026-10-07", 0, "2026-10-07")]
    public void AdicionarDiasUteis(string inicio, int dias, string esperado)
    {
        var cal = new CalendarioUtil();
        Assert.Equal(DateOnly.Parse(esperado), cal.AdicionarDiasUteis(DateOnly.Parse(inicio), dias));
    }

    [Fact]
    public void DiasUteisEntreEhOInversoDeAdicionar()
    {
        var cal = new CalendarioUtil();
        var inicio = new DateOnly(2026, 10, 1);
        foreach (var n in new[] { 1, 5, 10, 20, 37 })
            Assert.Equal(n, cal.DiasUteisEntre(inicio, cal.AdicionarDiasUteis(inicio, n)));
        Assert.Equal(-1, cal.DiasUteisEntre(new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 7)));
    }
}
