using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace BillMitra.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly HybridDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(HybridDbConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            var provider = _connectionFactory.ProviderName;

            _logger.LogInformation("Ensuring core schema tables exist using provider: {Provider}", provider);

            if (provider == "SqlServer")
            {
                await CreateSqlServerTablesAsync(connection);
            }
            else if (provider == "Sqlite")
            {
                await CreateSqliteTablesAsync(connection);
            }
            else
            {
                await CreatePostgreSqlTablesAsync(connection);
            }

            _logger.LogInformation("Core schema verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing core database schema.");
        }
    }

    private static async Task CreateSqlServerTablesAsync(IDbConnection conn)
    {
        const string sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='company_profile' AND xtype='U')
CREATE TABLE company_profile (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    legal_name NVARCHAR(200) NOT NULL,
    trade_name NVARCHAR(200) NOT NULL,
    gstin NVARCHAR(15) NULL,
    pan NVARCHAR(10) NULL,
    gst_registration_type INT NOT NULL DEFAULT 1,
    state_code NVARCHAR(2) NOT NULL,
    state_name NVARCHAR(100) NOT NULL,
    address_line1 NVARCHAR(255) NOT NULL,
    address_line2 NVARCHAR(255) NULL,
    city NVARCHAR(100) NOT NULL,
    pincode NVARCHAR(10) NOT NULL,
    phone NVARCHAR(20) NOT NULL,
    email NVARCHAR(100) NULL,
    bank_name NVARCHAR(100) NULL,
    bank_account_number NVARCHAR(50) NULL,
    bank_ifsc NVARCHAR(20) NULL,
    bank_branch NVARCHAR(100) NULL,
    upi_id NVARCHAR(100) NULL,
    invoice_footer_notes NVARCHAR(MAX) NULL,
    terms_and_conditions NVARCHAR(MAX) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at_utc DATETIME2 NOT NULL,
    updated_at_utc DATETIME2 NOT NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='financial_years' AND xtype='U')
CREATE TABLE financial_years (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    code NVARCHAR(20) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    is_current BIT NOT NULL DEFAULT 1,
    is_closed BIT NOT NULL DEFAULT 0,
    created_at_utc DATETIME2 NOT NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='users' AND xtype='U')
CREATE TABLE users (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    username NVARCHAR(50) NOT NULL UNIQUE,
    full_name NVARCHAR(100) NOT NULL,
    email NVARCHAR(100) NOT NULL,
    password_hash NVARCHAR(500) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at_utc DATETIME2 NOT NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='roles' AND xtype='U')
CREATE TABLE roles (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    name NVARCHAR(50) NOT NULL UNIQUE,
    description NVARCHAR(255) NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='user_roles' AND xtype='U')
CREATE TABLE user_roles (
    user_id UNIQUEIDENTIFIER NOT NULL,
    role_id UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (user_id, role_id)
);
";
        await conn.ExecuteAsync(sql);
    }

    private static async Task CreateSqliteTablesAsync(IDbConnection conn)
    {
        const string sql = @"
CREATE TABLE IF NOT EXISTS company_profile (
    id TEXT PRIMARY KEY,
    legal_name TEXT NOT NULL,
    trade_name TEXT NOT NULL,
    gstin TEXT NULL,
    pan TEXT NULL,
    gst_registration_type INTEGER NOT NULL DEFAULT 1,
    state_code TEXT NOT NULL,
    state_name TEXT NOT NULL,
    address_line1 TEXT NOT NULL,
    address_line2 TEXT NULL,
    city TEXT NOT NULL,
    pincode TEXT NOT NULL,
    phone TEXT NOT NULL,
    email TEXT NULL,
    bank_name TEXT NULL,
    bank_account_number TEXT NULL,
    bank_ifsc TEXT NULL,
    bank_branch TEXT NULL,
    upi_id TEXT NULL,
    invoice_footer_notes TEXT NULL,
    terms_and_conditions TEXT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS financial_years (
    id TEXT PRIMARY KEY,
    code TEXT NOT NULL,
    start_date TEXT NOT NULL,
    end_date TEXT NOT NULL,
    is_current INTEGER NOT NULL DEFAULT 1,
    is_closed INTEGER NOT NULL DEFAULT 0,
    created_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    full_name TEXT NOT NULL,
    email TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS roles (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    description TEXT NULL
);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id TEXT NOT NULL,
    role_id TEXT NOT NULL,
    PRIMARY KEY (user_id, role_id)
);
";
        await conn.ExecuteAsync(sql);
    }

    private static async Task CreatePostgreSqlTablesAsync(IDbConnection conn)
    {
        const string sql = @"
CREATE TABLE IF NOT EXISTS company_profile (
    id UUID PRIMARY KEY,
    legal_name VARCHAR(200) NOT NULL,
    trade_name VARCHAR(200) NOT NULL,
    gstin VARCHAR(15) NULL,
    pan VARCHAR(10) NULL,
    gst_registration_type INT NOT NULL DEFAULT 1,
    state_code VARCHAR(2) NOT NULL,
    state_name VARCHAR(100) NOT NULL,
    address_line1 VARCHAR(255) NOT NULL,
    address_line2 VARCHAR(255) NULL,
    city VARCHAR(100) NOT NULL,
    pincode VARCHAR(10) NOT NULL,
    phone VARCHAR(20) NOT NULL,
    email VARCHAR(100) NULL,
    bank_name VARCHAR(100) NULL,
    bank_account_number VARCHAR(50) NULL,
    bank_ifsc VARCHAR(20) NULL,
    bank_branch VARCHAR(100) NULL,
    upi_id VARCHAR(100) NULL,
    invoice_footer_notes TEXT NULL,
    terms_and_conditions TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS financial_years (
    id UUID PRIMARY KEY,
    code VARCHAR(20) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    is_current BOOLEAN NOT NULL DEFAULT TRUE,
    is_closed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL,
    password_hash VARCHAR(500) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255) NULL
);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    PRIMARY KEY (user_id, role_id)
);
";
        await conn.ExecuteAsync(sql);
    }
}
