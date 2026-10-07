using Cartorio.Core.Dominio;
using Cartorio.Core.Servicos;

namespace Cartorio.Core.Infra;

/// <summary>
/// Protocolos fictícios para a demonstração, criados pelas próprias regras (com o relógio avançando),
/// então prazos e histórico ficam coerentes. Nomes inventados; CPFs/CNPJs válidos só no cálculo.
/// </summary>
public static class DadosDemonstracao
{
    public static void Popular(IProtocoloRepository repo, DateTime agora)
    {
        // a numeração do Livro 1 é cronológica, então os protocolos são criados em ordem de data
        var relogio = new RelogioManual(agora.Date.AddDays(-45).AddHours(11));
        var s = new ProtocoloService(repo, relogio, "escrevente.demo");
        // cartório não abre no fim de semana: datas que caem em sábado/domingo vão para segunda
        void Util() { while (relogio.Agora.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) relogio.Agora = relogio.Agora.AddDays(1); }
        void Em(DateTime quando) { relogio.Agora = quando; Util(); }
        void Dia(int dias, int hora = 9) { relogio.Agora = relogio.Agora.Date.AddDays(dias).AddHours(hora).AddMinutes(dias * 7 % 50); Util(); }
        Util();

        // 1. com exigência e prenotação já vencida (a rotina diária cancela)
        var p1 = s.Prenotar(new("Ana Souza", "390.533.447-05", null), NaturezaTitulo.FormalDePartilha, "3.210", null);
        Dia(1); s.IniciarAnalise(p1.Id);
        s.EmitirExigencias(p1.Id, ["Apresentar o formal de partilha com a certidão de trânsito em julgado."]);

        // 2. fluxo completo: análise, exigência, reapresentação e registro
        Em(agora.Date.AddDays(-30).AddHours(9));
        var p2 = s.Prenotar(new("Mariana Costa Lima", "529.982.247-25", "(42) 99999-0001"), NaturezaTitulo.CompraEVenda, "12.345", "Imóvel urbano, lote 7");
        Dia(1); s.IniciarAnalise(p2.Id);
        Dia(1); s.EmitirExigencias(p2.Id, ["Apresentar certidão negativa de débitos municipais do imóvel.", "Comprovar recolhimento do ITBI."]);
        Dia(3); s.Reapresentar(p2.Id);
        Dia(1); s.Registrar(p2.Id, "R-4", null);

        // 3. averbação registrada
        Dia(1);
        var p3 = s.Prenotar(new("Construtora Exemplo Ltda", "11.222.333/0001-81", null), NaturezaTitulo.Averbacao, "8.901", "Averbação de construção");
        Dia(2); s.IniciarAnalise(p3.Id);
        s.Registrar(p3.Id, "AV-2", null);

        // 4. desistência
        Dia(1);
        var p4 = s.Prenotar(new("João Pereira", "111.444.777-35", null), NaturezaTitulo.Doacao, null, null);
        Dia(2); s.Cancelar(p4.Id, "Desistência do apresentante");

        // 5. em análise, perto de vencer o prazo de registro
        Em(agora.Date.AddDays(-12).AddHours(14));
        var p5 = s.Prenotar(new("Carlos Mendes", "714.602.380-01", null), NaturezaTitulo.CartaDeArrematacao, "777", null);
        Dia(2); s.IniciarAnalise(p5.Id);

        // 6. com exigência, ainda na vigência (CNPJ alfanumérico)
        Em(agora.Date.AddDays(-6).AddHours(10));
        var p6 = s.Prenotar(new("Banco Fictício S.A.", "12.ABC.345/01DE-35", null), NaturezaTitulo.AlienacaoFiduciaria, "15.002", "Contrato de financiamento");
        Dia(1); s.IniciarAnalise(p6.Id);
        s.EmitirExigencias(p6.Id, ["Corrigir a qualificação do devedor fiduciante (estado civil divergente da matrícula)."]);

        // 7. recém-prenotado
        relogio.Agora = agora.AddHours(-2);
        s.Prenotar(new("Fernanda Rocha", "153.509.460-56", "(42) 98888-0002"), NaturezaTitulo.CedulaDeCredito, "21.400", "Cédula rural pignoratícia e hipotecária");
    }
}
