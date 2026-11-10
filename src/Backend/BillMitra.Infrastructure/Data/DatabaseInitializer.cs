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

            // Seed default units if empty
            await SeedDefaultUnitsAsync(connection);

            _logger.LogInformation("Core schema verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing core database schema.");
        }
    }

    private static async Task SeedDefaultUnitsAsync(IDbConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM units");
        if (count == 0)
        {
            var defaultUnits = new[]
            {
                new { Id = Guid.NewGuid(), Code = "PCS", Name = "Pieces", IsActive = 1 },
                new { Id = Guid.NewGuid(), Code = "BOX", Name = "Boxes", IsActive = 1 },
                new { Id = Guid.NewGuid(), Code = "KGS", Name = "Kilograms", IsActive = 1 },
                new { Id = Guid.NewGuid(), Code = "MTR", Name = "Meters", IsActive = 1 },
                new { Id = Guid.NewGuid(), Code = "NOS", Name = "Numbers", IsActive = 1 },
                new { Id = Guid.NewGuid(), Code = "BAG", Name = "Bags", IsActive = 1 },
                new { Id = Guid.NewGuid(), Code = "SET", Name = "Sets", IsActive = 1 },
                new { Id = Guid.NewGuid(), Code = "LTR", Name = "Liters", IsActive = 1 }
            };

            foreach (var u in defaultUnits)
            {
                await conn.ExecuteAsync("INSERT INTO units (id, code, name, is_active) VALUES (@Id, @Code, @Name, @IsActive)", u);
            }
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

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='units' AND xtype='U')
CREATE TABLE units (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    code NVARCHAR(10) NOT NULL UNIQUE,
    name NVARCHAR(50) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='product_categories' AND xtype='U')
CREATE TABLE product_categories (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    name NVARCHAR(100) NOT NULL UNIQUE,
    description NVARCHAR(255) NULL,
    is_active BIT NOT NULL DEFAULT 1
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='products' AND xtype='U')
CREATE TABLE products (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    item_code NVARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(200) NOT NULL,
    description NVARCHAR(500) NULL,
    item_type INT NOT NULL DEFAULT 1,
    category_id UNIQUEIDENTIFIER NULL,
    hsn_sac_code NVARCHAR(10) NOT NULL,
    unit_code NVARCHAR(10) NOT NULL DEFAULT 'PCS',
    purchase_price DECIMAL(18,2) NOT NULL DEFAULT 0,
    sale_price DECIMAL(18,2) NOT NULL DEFAULT 0,
    gst_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 18.00,
    cess_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    is_tax_inclusive BIT NOT NULL DEFAULT 0,
    opening_stock DECIMAL(18,2) NOT NULL DEFAULT 0,
    current_stock DECIMAL(18,2) NOT NULL DEFAULT 0,
    min_stock_alert DECIMAL(18,2) NOT NULL DEFAULT 0,
    is_active BIT NOT NULL DEFAULT 1,
    created_at_utc DATETIME2 NOT NULL,
    updated_at_utc DATETIME2 NOT NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='parties' AND xtype='U')
CREATE TABLE parties (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    party_type INT NOT NULL DEFAULT 1,
    name NVARCHAR(200) NOT NULL,
    trade_name NVARCHAR(200) NULL,
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
    credit_limit DECIMAL(18,2) NOT NULL DEFAULT 0,
    current_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    is_active BIT NOT NULL DEFAULT 1,
    created_at_utc DATETIME2 NOT NULL,
    updated_at_utc DATETIME2 NOT NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='document_sequences' AND xtype='U')
CREATE TABLE document_sequences (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    financial_year_id UNIQUEIDENTIFIER NOT NULL,
    document_type INT NOT NULL,
    prefix NVARCHAR(20) NOT NULL,
    financial_year_token NVARCHAR(20) NOT NULL,
    next_number BIGINT NOT NULL DEFAULT 1,
    padding_length INT NOT NULL DEFAULT 4,
    suffix NVARCHAR(20) NULL,
    is_active BIT NOT NULL DEFAULT 1
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sales_documents' AND xtype='U')
CREATE TABLE sales_documents (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    document_type INT NOT NULL,
    document_status INT NOT NULL,
    document_number NVARCHAR(50) NULL,
    draft_number NVARCHAR(50) NOT NULL,
    document_date DATE NOT NULL,
    due_date DATE NULL,
    company_id UNIQUEIDENTIFIER NOT NULL,
    party_id UNIQUEIDENTIFIER NULL,
    customer_name NVARCHAR(200) NOT NULL,
    customer_gstin NVARCHAR(15) NULL,
    billing_address NVARCHAR(500) NULL,
    shipping_address NVARCHAR(500) NULL,
    place_of_supply_state_code NVARCHAR(2) NOT NULL,
    supplier_state_code NVARCHAR(2) NOT NULL,
    is_reverse_charge BIT NOT NULL DEFAULT 0,
    supply_type INT NOT NULL DEFAULT 1,
    currency_code NVARCHAR(3) NOT NULL DEFAULT 'INR',
    sub_total_taxable DECIMAL(18,2) NOT NULL DEFAULT 0,
    cgst_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    sgst_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    igst_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    cess_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_tax DECIMAL(18,2) NOT NULL DEFAULT 0,
    round_off_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    grand_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_amount_in_words NVARCHAR(500) NOT NULL,
    notes NVARCHAR(MAX) NULL,
    terms_and_conditions NVARCHAR(MAX) NULL,
    idempotency_key NVARCHAR(100) NULL,
    created_at_utc DATETIME2 NOT NULL,
    finalized_at_utc DATETIME2 NULL,
    cancelled_at_utc DATETIME2 NULL,
    cancellation_reason NVARCHAR(500) NULL,
    created_by NVARCHAR(100) NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='sales_document_items' AND xtype='U')
CREATE TABLE sales_document_items (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    sales_document_id UNIQUEIDENTIFIER NOT NULL,
    product_id UNIQUEIDENTIFIER NULL,
    line_number INT NOT NULL,
    item_name NVARCHAR(200) NOT NULL,
    hsn_sac_code NVARCHAR(10) NOT NULL,
    quantity DECIMAL(18,3) NOT NULL,
    unit_code NVARCHAR(10) NOT NULL,
    unit_price DECIMAL(18,2) NOT NULL,
    discount_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    discount_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    taxable_amount DECIMAL(18,2) NOT NULL,
    gst_rate_percent DECIMAL(5,2) NOT NULL,
    cgst_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    cgst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    sgst_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    sgst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    igst_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    igst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    cess_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    cess_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_amount DECIMAL(18,2) NOT NULL
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

CREATE TABLE IF NOT EXISTS units (
    id TEXT PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS product_categories (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    description TEXT NULL,
    is_active INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS products (
    id TEXT PRIMARY KEY,
    item_code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    description TEXT NULL,
    item_type INTEGER NOT NULL DEFAULT 1,
    category_id TEXT NULL,
    hsn_sac_code TEXT NOT NULL,
    unit_code TEXT NOT NULL DEFAULT 'PCS',
    purchase_price NUMERIC NOT NULL DEFAULT 0,
    sale_price NUMERIC NOT NULL DEFAULT 0,
    gst_rate_percent NUMERIC NOT NULL DEFAULT 18.00,
    cess_percent NUMERIC NOT NULL DEFAULT 0,
    is_tax_inclusive INTEGER NOT NULL DEFAULT 0,
    opening_stock NUMERIC NOT NULL DEFAULT 0,
    current_stock NUMERIC NOT NULL DEFAULT 0,
    min_stock_alert NUMERIC NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS parties (
    id TEXT PRIMARY KEY,
    party_type INTEGER NOT NULL DEFAULT 1,
    name TEXT NOT NULL,
    trade_name TEXT NULL,
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
    credit_limit NUMERIC NOT NULL DEFAULT 0,
    current_balance NUMERIC NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS document_sequences (
    id TEXT PRIMARY KEY,
    financial_year_id TEXT NOT NULL,
    document_type INTEGER NOT NULL,
    prefix TEXT NOT NULL,
    financial_year_token TEXT NOT NULL,
    next_number INTEGER NOT NULL DEFAULT 1,
    padding_length INTEGER NOT NULL DEFAULT 4,
    suffix TEXT NULL,
    is_active INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS sales_documents (
    id TEXT PRIMARY KEY,
    document_type INTEGER NOT NULL,
    document_status INTEGER NOT NULL,
    document_number TEXT NULL,
    draft_number TEXT NOT NULL,
    document_date TEXT NOT NULL,
    due_date TEXT NULL,
    company_id TEXT NOT NULL,
    party_id TEXT NULL,
    customer_name TEXT NOT NULL,
    customer_gstin TEXT NULL,
    billing_address TEXT NULL,
    shipping_address TEXT NULL,
    place_of_supply_state_code TEXT NOT NULL,
    supplier_state_code TEXT NOT NULL,
    is_reverse_charge INTEGER NOT NULL DEFAULT 0,
    supply_type INTEGER NOT NULL DEFAULT 1,
    currency_code TEXT NOT NULL DEFAULT 'INR',
    sub_total_taxable NUMERIC NOT NULL DEFAULT 0,
    cgst_total NUMERIC NOT NULL DEFAULT 0,
    sgst_total NUMERIC NOT NULL DEFAULT 0,
    igst_total NUMERIC NOT NULL DEFAULT 0,
    cess_total NUMERIC NOT NULL DEFAULT 0,
    total_tax NUMERIC NOT NULL DEFAULT 0,
    round_off_amount NUMERIC NOT NULL DEFAULT 0,
    grand_total NUMERIC NOT NULL DEFAULT 0,
    total_amount_in_words TEXT NOT NULL,
    notes TEXT NULL,
    terms_and_conditions TEXT NULL,
    idempotency_key TEXT NULL,
    created_at_utc TEXT NOT NULL,
    finalized_at_utc TEXT NULL,
    cancelled_at_utc TEXT NULL,
    cancellation_reason TEXT NULL,
    created_by TEXT NULL
);

CREATE TABLE IF NOT EXISTS sales_document_items (
    id TEXT PRIMARY KEY,
    sales_document_id TEXT NOT NULL,
    product_id TEXT NULL,
    line_number INTEGER NOT NULL,
    item_name TEXT NOT NULL,
    hsn_sac_code TEXT NOT NULL,
    quantity NUMERIC NOT NULL,
    unit_code TEXT NOT NULL,
    unit_price NUMERIC NOT NULL,
    discount_percent NUMERIC NOT NULL DEFAULT 0,
    discount_amount NUMERIC NOT NULL DEFAULT 0,
    taxable_amount NUMERIC NOT NULL,
    gst_rate_percent NUMERIC NOT NULL,
    cgst_rate_percent NUMERIC NOT NULL DEFAULT 0,
    cgst_amount NUMERIC NOT NULL DEFAULT 0,
    sgst_rate_percent NUMERIC NOT NULL DEFAULT 0,
    sgst_amount NUMERIC NOT NULL DEFAULT 0,
    igst_rate_percent NUMERIC NOT NULL DEFAULT 0,
    igst_amount NUMERIC NOT NULL DEFAULT 0,
    cess_rate_percent NUMERIC NOT NULL DEFAULT 0,
    cess_amount NUMERIC NOT NULL DEFAULT 0,
    total_amount NUMERIC NOT NULL
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

CREATE TABLE IF NOT EXISTS units (
    id UUID PRIMARY KEY,
    code VARCHAR(10) NOT NULL UNIQUE,
    name VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS product_categories (
    id UUID PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS products (
    id UUID PRIMARY KEY,
    item_code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(500) NULL,
    item_type INT NOT NULL DEFAULT 1,
    category_id UUID NULL,
    hsn_sac_code VARCHAR(10) NOT NULL,
    unit_code VARCHAR(10) NOT NULL DEFAULT 'PCS',
    purchase_price DECIMAL(18,2) NOT NULL DEFAULT 0,
    sale_price DECIMAL(18,2) NOT NULL DEFAULT 0,
    gst_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 18.00,
    cess_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    is_tax_inclusive BOOLEAN NOT NULL DEFAULT FALSE,
    opening_stock DECIMAL(18,2) NOT NULL DEFAULT 0,
    current_stock DECIMAL(18,2) NOT NULL DEFAULT 0,
    min_stock_alert DECIMAL(18,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS parties (
    id UUID PRIMARY KEY,
    party_type INT NOT NULL DEFAULT 1,
    name VARCHAR(200) NOT NULL,
    trade_name VARCHAR(200) NULL,
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
    credit_limit DECIMAL(18,2) NOT NULL DEFAULT 0,
    current_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS document_sequences (
    id UUID PRIMARY KEY,
    financial_year_id UUID NOT NULL,
    document_type INT NOT NULL,
    prefix VARCHAR(20) NOT NULL,
    financial_year_token VARCHAR(20) NOT NULL,
    next_number BIGINT NOT NULL DEFAULT 1,
    padding_length INT NOT NULL DEFAULT 4,
    suffix VARCHAR(20) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS sales_documents (
    id UUID PRIMARY KEY,
    document_type INT NOT NULL,
    document_status INT NOT NULL,
    document_number VARCHAR(50) NULL,
    draft_number VARCHAR(50) NOT NULL,
    document_date DATE NOT NULL,
    due_date DATE NULL,
    company_id UUID NOT NULL,
    party_id UUID NULL,
    customer_name VARCHAR(200) NOT NULL,
    customer_gstin VARCHAR(15) NULL,
    billing_address VARCHAR(500) NULL,
    shipping_address VARCHAR(500) NULL,
    place_of_supply_state_code VARCHAR(2) NOT NULL,
    supplier_state_code VARCHAR(2) NOT NULL,
    is_reverse_charge BOOLEAN NOT NULL DEFAULT FALSE,
    supply_type INT NOT NULL DEFAULT 1,
    currency_code VARCHAR(3) NOT NULL DEFAULT 'INR',
    sub_total_taxable DECIMAL(18,2) NOT NULL DEFAULT 0,
    cgst_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    sgst_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    igst_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    cess_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_tax DECIMAL(18,2) NOT NULL DEFAULT 0,
    round_off_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    grand_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_amount_in_words VARCHAR(500) NOT NULL,
    notes TEXT NULL,
    terms_and_conditions TEXT NULL,
    idempotency_key VARCHAR(100) NULL,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    finalized_at_utc TIMESTAMP WITH TIME ZONE NULL,
    cancelled_at_utc TIMESTAMP WITH TIME ZONE NULL,
    cancellation_reason VARCHAR(500) NULL,
    created_by VARCHAR(100) NULL
);

CREATE TABLE IF NOT EXISTS sales_document_items (
    id UUID PRIMARY KEY,
    sales_document_id UUID NOT NULL,
    product_id UUID NULL,
    line_number INT NOT NULL,
    item_name VARCHAR(200) NOT NULL,
    hsn_sac_code VARCHAR(10) NOT NULL,
    quantity DECIMAL(18,3) NOT NULL,
    unit_code VARCHAR(10) NOT NULL,
    unit_price DECIMAL(18,2) NOT NULL,
    discount_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    discount_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    taxable_amount DECIMAL(18,2) NOT NULL,
    gst_rate_percent DECIMAL(5,2) NOT NULL,
    cgst_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    cgst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    sgst_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    sgst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    igst_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    igst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    cess_rate_percent DECIMAL(5,2) NOT NULL DEFAULT 0,
    cess_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_amount DECIMAL(18,2) NOT NULL
);
";
        await conn.ExecuteAsync(sql);
    }
}
