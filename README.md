# Protocolo de Cartório

Sistema desktop em **C# / WinForms + SQL Server** para o **Livro 1 (Protocolo)** de um Registro de Imóveis. Ele controla a entrada dos títulos (prenotação), a qualificação, a nota devolutiva com exigências, a reapresentação e o registro, sempre com os **prazos em dias úteis**.

**Stack:** C# 12 · .NET 8 · Windows Forms · padrão **MVP** (Model-View-Presenter) · SQL Server (ADO.NET) · xUnit · GitHub Actions (testes com SQL Server 2022 em container + build do app no Windows)

> Dados de demonstração **fictícios**. Os prazos padrão seguem a Lei 6.015/73 (registro em 10 dias e vigência da prenotação de 20 dias, contados em dias úteis desde a Lei 14.382/2022) e podem ser ajustados no `config.json`. Confira sempre com a corregedoria do seu estado.

## O que faz

- **Prenotação:** número de ordem sequencial e **sem buracos**, data e hora, apresentante com validação de **CPF/CNPJ**, natureza do título e matrícula.
  - O CNPJ aceita o novo formato **alfanumérico** que a Receita emite a partir de julho/2026.
- **Comprovante de protocolo** impresso (pré-visualização e impressora), entregue ao apresentante.
- **Andamento:** `Prenotado → Em análise → Com exigência ⇄ Em análise → Registrado`, ou `Cancelado`. Cada passo fica no histórico com data, hora e usuário.
- **Nota devolutiva:** várias exigências por título, marcadas como cumpridas na reapresentação.
- **Registro:** valida e normaliza o ato (`r 3` vira `R-3/12.345`; averbação gera `AV-n`).
- **Prazos em dias úteis:**
  - descontam fins de semana, feriados nacionais (calculados por ano, inclusive a Sexta-feira Santa pela data da Páscoa) e feriados locais cadastrados;
  - a fila fica **amarela** quando o prazo está vencendo e **vermelha** quando venceu.
- **Rotina de vencimento:** ao abrir o sistema, cancela as prenotações com exigência não cumprida dentro da vigência e avisa quais foram.
- **Busca** por número, apresentante, CPF/CNPJ (parcial) ou matrícula; filtros por status, período e "só em andamento"; paginação.
- **Exportação para CSV**, que abre direto no Excel com os acentos certos e é protegida contra *CSV injection*.
- **Teclado:** F2 nova prenotação, Enter abre, F5 atualiza, Ctrl+F busca.

## Arquitetura

```
src/
  Cartorio.Core/          sem WinForms e sem banco → 100% testável
    Dominio/              Protocolo (regras e transições), CalendarioUtil, FeriadosNacionais, DocumentoFiscal
    Servicos/             ProtocoloService (casos de uso) + interfaces IProtocoloRepository e IRelogio
    Apresentacao/         Presenters do MVP + interfaces das views
    Relatorios/           comprovante e CSV
    Infra/                repositório em memória e dados de demonstração
  Cartorio.Data/          SQL Server em ADO.NET puro + scripts SQL embutidos (idempotentes)
  Cartorio.WinForms/      formulários: só repassam eventos e desenham o que o presenter manda
tests/
  Cartorio.Core.Tests/    domínio, serviço, presenters (com views falsas) e relatórios
  Cartorio.Data.Tests/    integração com SQL Server de verdade
```

### Decisões

- **MVP em vez de lógica no code-behind.** Num app WinForms tradicional a regra fica no `button1_Click` e só dá para testar clicando. Aqui o formulário implementa uma interface (`IDetalheProtocoloView`), e o presenter decide o que habilitar, o que perguntar e o que mostrar. Os testes usam uma view falsa: o fluxo inteiro da tela (análise → exigência → reapresentação → registro) é testado sem abrir janela.
- **Numeração do Livro 1 sem buracos.** Uma `SEQUENCE` ou `IDENTITY` perde números quando uma transação é desfeita. O número sai de uma tabela de contadores incrementada **na mesma transação** do insert, com `UPDATE ... OUTPUT`. A trava da linha garante que duas estações nunca pegam o mesmo número. Há um teste com 20 prenotações em paralelo.
- **Concorrência otimista com `rowversion`.** Se duas estações abrem o mesmo protocolo e as duas tentam gravar, a segunda recebe "alterado em outra estação" em vez de sobrescrever em silêncio.
- **ADO.NET puro, sem ORM.** O SQL fica explícito (paginação com `OFFSET/FETCH`, `LIKE` com escape, parâmetros em tudo). O projeto `Data` depende só de `System.Data.Common`; o driver `SqlClient` entra no app e nos testes.
- **Relógio injetável.** Prazo depende de data; nos testes o tempo é fixo e avançado de propósito ("reapresentou depois da vigência").
- **Comprovante separado da impressão.** O Core monta as linhas e o WinForms só desenha no `PrintDocument`. Dá para testar o conteúdo e, depois, gerar PDF com o mesmo código.

## Rodando

Requisitos: Windows, [.NET 8 SDK](https://dotnet.microsoft.com/download) e SQL Server. O **LocalDB** que vem com o Visual Studio basta.

```powershell
dotnet run --project src/Cartorio.WinForms
```

- Na primeira execução o sistema **cria o banco** `ProtocoloCartorio` e as tabelas (connection string em `src/Cartorio.WinForms/config.json`).
- Sem SQL Server? Rode `dotnet run --project src/Cartorio.WinForms -- --demo`, ou use `"Modo": "Memoria"` no `config.json`. O app abre com dados fictícios em memória e não grava nada.
- O executável pronto também sai como artefato em cada execução do **Actions**.

Os feriados municipais ficam na tabela `dbo.FeriadosLocais`. Exemplo:

```sql
INSERT INTO dbo.FeriadosLocais (Data, Nome) VALUES ('2026-06-04', N'Corpus Christi');
```

## Testes

```powershell
dotnet test tests/Cartorio.Core.Tests        # 67 testes, sem banco
$env:CARTORIO_TEST_SQL = "Server=(localdb)\MSSQLLocalDB;Database=CartorioTestes;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test tests/Cartorio.Data.Tests        # integração; cria o banco de testes se não existir
```

- **CPF/CNPJ:** dígitos verificadores, sequências repetidas e CNPJ alfanumérico (exemplo oficial da Receita).
- **Calendário:** Páscoa de vários anos, Consciência Negra só a partir de 2024, feriado local, e a propriedade "somar N dias úteis e depois contar dá N".
- **Protocolo:** prazos, todas as transições válidas e inválidas, vigência vencida, normalização de ato e matrícula, situação do prazo.
- **Presenters:** paginação, aviso de cancelamentos automáticos, erros por campo no formulário, usuário cancelando caixas de diálogo, erro de regra mostrado na tela.
- **SQL Server:** ida e volta do agregado, numeração sequencial e concorrente, conflito de versão com rollback, filtros e paginação, rotina de vencimento, feriados.

## Próximos passos

- Cálculo de emolumentos pela tabela do estado.
- Login com perfis (escrevente, substituto, oficial).
- Comprovante em PDF e envio por email.
- Tela de cadastro de feriados locais.

## Autor

Ademar C. Neto
