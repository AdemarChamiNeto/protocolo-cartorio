-- Schema do Protocolo (SQL Server 2019+). Idempotente: pode rodar de novo sem erro.

IF OBJECT_ID(N'dbo.Contadores', N'U') IS NULL
    CREATE TABLE dbo.Contadores (
        Nome  varchar(30) NOT NULL CONSTRAINT PK_Contadores PRIMARY KEY,
        Valor int         NOT NULL
    );
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Contadores WHERE Nome = 'Protocolo')
    INSERT INTO dbo.Contadores (Nome, Valor) VALUES ('Protocolo', 0);
GO

IF OBJECT_ID(N'dbo.Protocolos', N'U') IS NULL
    CREATE TABLE dbo.Protocolos (
        Id                    int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Protocolos PRIMARY KEY,
        Numero                int            NOT NULL CONSTRAINT UQ_Protocolos_Numero UNIQUE,
        PrenotadoEm           datetime2(0)   NOT NULL,
        ApresentanteNome      nvarchar(150)  NOT NULL,
        ApresentanteDocumento varchar(14)    NOT NULL,
        ApresentanteTelefone  varchar(20)    NULL,
        Natureza              tinyint        NOT NULL,
        Matricula             varchar(15)    NULL,
        Observacoes           nvarchar(500)  NULL,
        Status                tinyint        NOT NULL CONSTRAINT CK_Protocolos_Status CHECK (Status BETWEEN 1 AND 5),
        PrazoRegistro         date           NOT NULL,
        VencimentoPrenotacao  date           NOT NULL,
        Ato                   varchar(30)    NULL,
        MotivoCancelamento    nvarchar(300)  NULL,
        ConcluidoEm           datetime2(0)   NULL,
        Versao                rowversion     NOT NULL
    );
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Protocolos_Status')
    CREATE INDEX IX_Protocolos_Status ON dbo.Protocolos (Status, VencimentoPrenotacao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Protocolos_Documento')
    CREATE INDEX IX_Protocolos_Documento ON dbo.Protocolos (ApresentanteDocumento);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Protocolos_Matricula')
    CREATE INDEX IX_Protocolos_Matricula ON dbo.Protocolos (Matricula);
GO

IF OBJECT_ID(N'dbo.Exigencias', N'U') IS NULL
    CREATE TABLE dbo.Exigencias (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Exigencias PRIMARY KEY,
        ProtocoloId int            NOT NULL CONSTRAINT FK_Exigencias_Protocolos REFERENCES dbo.Protocolos (Id),
        Texto       nvarchar(1000) NOT NULL,
        EmitidaEm   datetime2(0)   NOT NULL,
        CumpridaEm  datetime2(0)   NULL
    );
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Exigencias_Protocolo')
    CREATE INDEX IX_Exigencias_Protocolo ON dbo.Exigencias (ProtocoloId);
GO

IF OBJECT_ID(N'dbo.Eventos', N'U') IS NULL
    CREATE TABLE dbo.Eventos (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Eventos PRIMARY KEY,
        ProtocoloId int           NOT NULL CONSTRAINT FK_Eventos_Protocolos REFERENCES dbo.Protocolos (Id),
        Quando      datetime2(0)  NOT NULL,
        Usuario     nvarchar(60)  NOT NULL,
        Descricao   nvarchar(400) NOT NULL
    );
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Eventos_Protocolo')
    CREATE INDEX IX_Eventos_Protocolo ON dbo.Eventos (ProtocoloId);
GO

-- feriados municipais/estaduais (os nacionais são calculados no código)
IF OBJECT_ID(N'dbo.FeriadosLocais', N'U') IS NULL
    CREATE TABLE dbo.FeriadosLocais (
        Data date          NOT NULL CONSTRAINT PK_FeriadosLocais PRIMARY KEY,
        Nome nvarchar(100) NOT NULL
    );
GO
