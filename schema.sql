-- =============================================================
-- Payments Matching Tool – SQL Server Database Schema
-- =============================================================

CREATE DATABASE PaymentsMatchingDb;
GO

USE PaymentsMatchingDb;
GO

-- -------------------------------------------------------------
-- Table: MatchSessions
-- Stores one record per "Run Match" action
-- -------------------------------------------------------------
CREATE TABLE MatchSessions (
    Id              INT             IDENTITY(1,1)   PRIMARY KEY,
    CreatedAt       DATETIME2       NOT NULL        DEFAULT GETUTCDATE(),
    TotalCount      INT             NOT NULL        DEFAULT 0,
    MatchedCount    INT             NOT NULL        DEFAULT 0,
    OnlySystemCount INT             NOT NULL        DEFAULT 0,
    OnlyProviderCount INT           NOT NULL        DEFAULT 0,
    AmountMismatchCount INT         NOT NULL        DEFAULT 0
);
GO

-- -------------------------------------------------------------
-- Table: MatchResults
-- One row per orderId+currency pair found across both files
-- -------------------------------------------------------------
CREATE TABLE MatchResults (
    Id              INT             IDENTITY(1,1)   PRIMARY KEY,
    SessionId       INT             NOT NULL,
    OrderId         NVARCHAR(100)   NOT NULL,
    Currency        NVARCHAR(10)    NOT NULL,
    SystemAmount    DECIMAL(18,4)   NULL,
    ProviderAmount  DECIMAL(18,4)   NULL,
    Status          NVARCHAR(30)    NOT NULL,   -- MATCHED | ONLYSYSTEM | ONLYPROVIDER | AMOUNTMISMATCH
    IsResolved      BIT             NOT NULL    DEFAULT 0,
    ResolutionSide  NVARCHAR(20)    NULL,       -- System | Provider | NULL
    ResolvedAt      DATETIME2       NULL,

    CONSTRAINT FK_MatchResults_Session FOREIGN KEY (SessionId) REFERENCES MatchSessions(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_MatchResults_SessionOrderCurrency UNIQUE (SessionId, OrderId, Currency)
);
GO

-- Index for fast filtering by session + resolution status
CREATE INDEX IX_MatchResults_SessionId_IsResolved
    ON MatchResults (SessionId, IsResolved);
GO

-- Index for status-based queries
CREATE INDEX IX_MatchResults_Status
    ON MatchResults (Status);
GO

-- =============================================================
-- Seed / sample data (optional – for dev/demo purposes)
-- =============================================================
-- INSERT INTO MatchSessions DEFAULT VALUES;
-- GO
