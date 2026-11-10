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
    letterhead_image_path NVARCHAR(500) NULL,
    default_letterhead_mode INT NOT NULL DEFAULT 1,
    preprinted_header_margin_mm INT NOT NULL DEFAULT 50,
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
    paid_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_amount_in_words NVARCHAR(500) NOT NULL,
    notes NVARCHAR(MAX) NULL,
    terms_and_conditions NVARCHAR(MAX) NULL,
    idempotency_key NVARCHAR(100) NULL,
    created_at_utc DATETIME2 NOT NULL,
    finalized_at_utc DATETIME2 NULL,
    cancelled_at_utc DATETIME2 NULL,
    cancellation_reason NVARCHAR(500) NULL,
    created_by NVARCHAR(100) NULL,
    source_document_id UNIQUEIDENTIFIER NULL,
    source_document_number NVARCHAR(50) NULL,
    converted_to_document_id UNIQUEIDENTIFIER NULL
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

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='payments' AND xtype='U')
CREATE TABLE payments (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    payment_number NVARCHAR(50) NOT NULL UNIQUE,
    party_id UNIQUEIDENTIFIER NOT NULL,
    party_name NVARCHAR(200) NOT NULL,
    payment_type INT NOT NULL,
    payment_mode INT NOT NULL,
    payment_date DATE NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    allocated_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    reference_number NVARCHAR(100) NULL,
    bank_name NVARCHAR(100) NULL,
    cheque_date DATE NULL,
    notes NVARCHAR(MAX) NULL,
    status INT NOT NULL DEFAULT 1,
    created_at_utc DATETIME2 NOT NULL,
    created_by NVARCHAR(100) NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='payment_allocations' AND xtype='U')
CREATE TABLE payment_allocations (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    payment_id UNIQUEIDENTIFIER NOT NULL,
    sales_document_id UNIQUEIDENTIFIER NOT NULL,
    allocated_amount DECIMAL(18,2) NOT NULL,
    allocation_date DATE NOT NULL,
    notes NVARCHAR(255) NULL,
    created_at_utc DATETIME2 NOT NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='payment_reversals' AND xtype='U')
CREATE TABLE payment_reversals (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    payment_id UNIQUEIDENTIFIER NOT NULL,
    reversal_date DATETIME2 NOT NULL,
    reason NVARCHAR(500) NOT NULL,
    reversed_by NVARCHAR(100) NULL,
    created_at_utc DATETIME2 NOT NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='gst_return_filings' AND xtype='U')
CREATE TABLE gst_return_filings (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    company_id UNIQUEIDENTIFIER NOT NULL,
    return_type INT NOT NULL,
    financial_year NVARCHAR(20) NOT NULL,
    tax_period NVARCHAR(20) NOT NULL,
    filing_status INT NOT NULL,
    arn NVARCHAR(100) NULL,
    arn_date DATETIME2 NULL,
    validation_errors_json NVARCHAR(MAX) NULL,
    payload_json NVARCHAR(MAX) NULL,
    provider_name NVARCHAR(100) NULL,
    provider_reference NVARCHAR(100) NULL,
    provider_response_json NVARCHAR(MAX) NULL,
    total_taxable_value DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_tax_liability DECIMAL(18,2) NOT NULL DEFAULT 0,
    created_at_utc DATETIME2 NOT NULL,
    updated_at_utc DATETIME2 NOT NULL,
    filed_at_utc DATETIME2 NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='eway_bills' AND xtype='U')
CREATE TABLE eway_bills (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    sales_document_id UNIQUEIDENTIFIER NOT NULL,
    eway_bill_number NVARCHAR(20) NOT NULL,
    eway_bill_date DATETIME2 NOT NULL,
    valid_until DATETIME2 NOT NULL,
    status INT NOT NULL,
    supply_type INT NOT NULL,
    sub_supply_type INT NOT NULL,
    doc_type NVARCHAR(20) NOT NULL,
    doc_number NVARCHAR(50) NOT NULL,
    doc_date DATETIME2 NOT NULL,
    from_gstin NVARCHAR(15) NOT NULL,
    from_name NVARCHAR(200) NOT NULL,
    from_address NVARCHAR(500) NOT NULL,
    from_place NVARCHAR(100) NOT NULL,
    from_pincode NVARCHAR(10) NOT NULL,
    from_state_code NVARCHAR(2) NOT NULL,
    to_gstin NVARCHAR(15) NULL,
    to_name NVARCHAR(200) NOT NULL,
    to_address NVARCHAR(500) NOT NULL,
    to_place NVARCHAR(100) NOT NULL,
    to_pincode NVARCHAR(10) NOT NULL,
    to_state_code NVARCHAR(2) NOT NULL,
    total_invoice_value DECIMAL(18,2) NOT NULL DEFAULT 0,
    taxable_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    cgst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    sgst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    igst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    cess_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    transportation_mode INT NOT NULL DEFAULT 1,
    transporter_id NVARCHAR(50) NULL,
    transporter_name NVARCHAR(200) NULL,
    transport_doc_no NVARCHAR(50) NULL,
    transport_doc_date DATETIME2 NULL,
    vehicle_no NVARCHAR(50) NULL,
    vehicle_type INT NOT NULL DEFAULT 1,
    approximate_distance_km INT NOT NULL DEFAULT 10,
    cancel_reason NVARCHAR(100) NULL,
    cancel_remarks NVARCHAR(500) NULL,
    cancelled_at_utc DATETIME2 NULL,
    created_at_utc DATETIME2 NOT NULL,
    updated_at_utc DATETIME2 NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='einvoices' AND xtype='U')
CREATE TABLE einvoices (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    sales_document_id UNIQUEIDENTIFIER NOT NULL,
    irn NVARCHAR(100) NOT NULL,
    ack_no NVARCHAR(50) NOT NULL,
    ack_date DATETIME2 NOT NULL,
    signed_invoice NVARCHAR(MAX) NOT NULL,
    signed_qr_code NVARCHAR(MAX) NOT NULL,
    status INT NOT NULL,
    cancel_reason INT NULL,
    cancel_remarks NVARCHAR(500) NULL,
    cancelled_at_utc DATETIME2 NULL,
    created_at_utc DATETIME2 NOT NULL,
    updated_at_utc DATETIME2 NULL
);

IF NOT EXISTS (SELECT * FROM syscolumns WHERE id=OBJECT_ID('sales_documents') AND name='paid_amount')
ALTER TABLE sales_documents ADD paid_amount DECIMAL(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM syscolumns WHERE id=OBJECT_ID('company_profile') AND name='letterhead_image_path')
ALTER TABLE company_profile ADD letterhead_image_path NVARCHAR(500) NULL, default_letterhead_mode INT NOT NULL DEFAULT 1, preprinted_header_margin_mm INT NOT NULL DEFAULT 50;

IF NOT EXISTS (SELECT * FROM syscolumns WHERE id=OBJECT_ID('sales_documents') AND name='source_document_id')
ALTER TABLE sales_documents ADD source_document_id UNIQUEIDENTIFIER NULL, source_document_number NVARCHAR(50) NULL, converted_to_document_id UNIQUEIDENTIFIER NULL;
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
    letterhead_image_path TEXT NULL,
    default_letterhead_mode INTEGER NOT NULL DEFAULT 1,
    preprinted_header_margin_mm INTEGER NOT NULL DEFAULT 50,
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
    paid_amount NUMERIC NOT NULL DEFAULT 0,
    total_amount_in_words TEXT NOT NULL,
    notes TEXT NULL,
    terms_and_conditions TEXT NULL,
    idempotency_key TEXT NULL,
    created_at_utc TEXT NOT NULL,
    finalized_at_utc TEXT NULL,
    cancelled_at_utc TEXT NULL,
    cancellation_reason TEXT NULL,
    created_by TEXT NULL,
    source_document_id TEXT NULL,
    source_document_number TEXT NULL,
    converted_to_document_id TEXT NULL
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

CREATE TABLE IF NOT EXISTS payments (
    id TEXT PRIMARY KEY,
    payment_number TEXT NOT NULL UNIQUE,
    party_id TEXT NOT NULL,
    party_name TEXT NOT NULL,
    payment_type INTEGER NOT NULL,
    payment_mode INTEGER NOT NULL,
    payment_date TEXT NOT NULL,
    amount NUMERIC NOT NULL,
    allocated_amount NUMERIC NOT NULL DEFAULT 0,
    reference_number TEXT NULL,
    bank_name TEXT NULL,
    cheque_date TEXT NULL,
    notes TEXT NULL,
    status INTEGER NOT NULL DEFAULT 1,
    created_at_utc TEXT NOT NULL,
    created_by TEXT NULL
);

CREATE TABLE IF NOT EXISTS payment_allocations (
    id TEXT PRIMARY KEY,
    payment_id TEXT NOT NULL,
    sales_document_id TEXT NOT NULL,
    allocated_amount NUMERIC NOT NULL,
    allocation_date TEXT NOT NULL,
    notes TEXT NULL,
    created_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS payment_reversals (
    id TEXT PRIMARY KEY,
    payment_id TEXT NOT NULL,
    reversal_date TEXT NOT NULL,
    reason TEXT NOT NULL,
    reversed_by TEXT NULL,
    created_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS gst_return_filings (
    id TEXT PRIMARY KEY,
    company_id TEXT NOT NULL,
    return_type INTEGER NOT NULL,
    financial_year TEXT NOT NULL,
    tax_period TEXT NOT NULL,
    filing_status INTEGER NOT NULL,
    arn TEXT NULL,
    arn_date TEXT NULL,
    validation_errors_json TEXT NULL,
    payload_json TEXT NULL,
    provider_name TEXT NULL,
    provider_reference TEXT NULL,
    provider_response_json TEXT NULL,
    total_taxable_value NUMERIC NOT NULL DEFAULT 0,
    total_tax_liability NUMERIC NOT NULL DEFAULT 0,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL,
    filed_at_utc TEXT NULL
);

CREATE TABLE IF NOT EXISTS eway_bills (
    id TEXT PRIMARY KEY,
    sales_document_id TEXT NOT NULL,
    eway_bill_number TEXT NOT NULL,
    eway_bill_date TEXT NOT NULL,
    valid_until TEXT NOT NULL,
    status INTEGER NOT NULL,
    supply_type INTEGER NOT NULL,
    sub_supply_type INTEGER NOT NULL,
    doc_type TEXT NOT NULL,
    doc_number TEXT NOT NULL,
    doc_date TEXT NOT NULL,
    from_gstin TEXT NOT NULL,
    from_name TEXT NOT NULL,
    from_address TEXT NOT NULL,
    from_place TEXT NOT NULL,
    from_pincode TEXT NOT NULL,
    from_state_code TEXT NOT NULL,
    to_gstin TEXT NULL,
    to_name TEXT NOT NULL,
    to_address TEXT NOT NULL,
    to_place TEXT NOT NULL,
    to_pincode TEXT NOT NULL,
    to_state_code TEXT NOT NULL,
    total_invoice_value NUMERIC NOT NULL DEFAULT 0,
    taxable_amount NUMERIC NOT NULL DEFAULT 0,
    cgst_amount NUMERIC NOT NULL DEFAULT 0,
    sgst_amount NUMERIC NOT NULL DEFAULT 0,
    igst_amount NUMERIC NOT NULL DEFAULT 0,
    cess_amount NUMERIC NOT NULL DEFAULT 0,
    transportation_mode INTEGER NOT NULL DEFAULT 1,
    transporter_id TEXT NULL,
    transporter_name TEXT NULL,
    transport_doc_no TEXT NULL,
    transport_doc_date TEXT NULL,
    vehicle_no TEXT NULL,
    vehicle_type INTEGER NOT NULL DEFAULT 1,
    approximate_distance_km INTEGER NOT NULL DEFAULT 10,
    cancel_reason TEXT NULL,
    cancel_remarks TEXT NULL,
    cancelled_at_utc TEXT NULL,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NULL
);

CREATE TABLE IF NOT EXISTS einvoices (
    id TEXT PRIMARY KEY,
    sales_document_id TEXT NOT NULL,
    irn TEXT NOT NULL,
    ack_no TEXT NOT NULL,
    ack_date TEXT NOT NULL,
    signed_invoice TEXT NOT NULL,
    signed_qr_code TEXT NOT NULL,
    status INTEGER NOT NULL,
    cancel_reason INTEGER NULL,
    cancel_remarks TEXT NULL,
    cancelled_at_utc TEXT NULL,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NULL
);
";
        await conn.ExecuteAsync(sql);

        try
        {
            await conn.ExecuteAsync("ALTER TABLE sales_documents ADD COLUMN paid_amount NUMERIC NOT NULL DEFAULT 0;");
        }
        catch
        {
            // Column already exists
        }

        try { await conn.ExecuteAsync("ALTER TABLE company_profile ADD COLUMN letterhead_image_path TEXT NULL;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE company_profile ADD COLUMN default_letterhead_mode INTEGER NOT NULL DEFAULT 1;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE company_profile ADD COLUMN preprinted_header_margin_mm INTEGER NOT NULL DEFAULT 50;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE sales_documents ADD COLUMN source_document_id TEXT NULL;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE sales_documents ADD COLUMN source_document_number TEXT NULL;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE sales_documents ADD COLUMN converted_to_document_id TEXT NULL;"); } catch {}
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
    letterhead_image_path VARCHAR(500) NULL,
    default_letterhead_mode INT NOT NULL DEFAULT 1,
    preprinted_header_margin_mm INT NOT NULL DEFAULT 50,
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
    paid_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_amount_in_words VARCHAR(500) NOT NULL,
    notes TEXT NULL,
    terms_and_conditions TEXT NULL,
    idempotency_key VARCHAR(100) NULL,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    finalized_at_utc TIMESTAMP WITH TIME ZONE NULL,
    cancelled_at_utc TIMESTAMP WITH TIME ZONE NULL,
    cancellation_reason VARCHAR(500) NULL,
    created_by VARCHAR(100) NULL,
    source_document_id UUID NULL,
    source_document_number VARCHAR(50) NULL,
    converted_to_document_id UUID NULL
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

CREATE TABLE IF NOT EXISTS payments (
    id UUID PRIMARY KEY,
    payment_number VARCHAR(50) NOT NULL UNIQUE,
    party_id UUID NOT NULL,
    party_name VARCHAR(200) NOT NULL,
    payment_type INT NOT NULL,
    payment_mode INT NOT NULL,
    payment_date DATE NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    allocated_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    reference_number VARCHAR(100) NULL,
    bank_name VARCHAR(100) NULL,
    cheque_date DATE NULL,
    notes TEXT NULL,
    status INT NOT NULL DEFAULT 1,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    created_by VARCHAR(100) NULL
);

CREATE TABLE IF NOT EXISTS payment_allocations (
    id UUID PRIMARY KEY,
    payment_id UUID NOT NULL,
    sales_document_id UUID NOT NULL,
    allocated_amount DECIMAL(18,2) NOT NULL,
    allocation_date DATE NOT NULL,
    notes VARCHAR(255) NULL,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS payment_reversals (
    id UUID PRIMARY KEY,
    payment_id UUID NOT NULL,
    reversal_date TIMESTAMP WITH TIME ZONE NOT NULL,
    reason VARCHAR(500) NOT NULL,
    reversed_by VARCHAR(100) NULL,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS gst_return_filings (
    id UUID PRIMARY KEY,
    company_id UUID NOT NULL,
    return_type INT NOT NULL,
    financial_year VARCHAR(20) NOT NULL,
    tax_period VARCHAR(20) NOT NULL,
    filing_status INT NOT NULL,
    arn VARCHAR(100) NULL,
    arn_date TIMESTAMP WITH TIME ZONE NULL,
    validation_errors_json TEXT NULL,
    payload_json TEXT NULL,
    provider_name VARCHAR(100) NULL,
    provider_reference VARCHAR(100) NULL,
    provider_response_json TEXT NULL,
    total_taxable_value DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_tax_liability DECIMAL(18,2) NOT NULL DEFAULT 0,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    filed_at_utc TIMESTAMP WITH TIME ZONE NULL
);

CREATE TABLE IF NOT EXISTS eway_bills (
    id UUID PRIMARY KEY,
    sales_document_id UUID NOT NULL,
    eway_bill_number VARCHAR(20) NOT NULL,
    eway_bill_date TIMESTAMP WITH TIME ZONE NOT NULL,
    valid_until TIMESTAMP WITH TIME ZONE NOT NULL,
    status INT NOT NULL,
    supply_type INT NOT NULL,
    sub_supply_type INT NOT NULL,
    doc_type VARCHAR(20) NOT NULL,
    doc_number VARCHAR(50) NOT NULL,
    doc_date TIMESTAMP WITH TIME ZONE NOT NULL,
    from_gstin VARCHAR(15) NOT NULL,
    from_name VARCHAR(200) NOT NULL,
    from_address VARCHAR(500) NOT NULL,
    from_place VARCHAR(100) NOT NULL,
    from_pincode VARCHAR(10) NOT NULL,
    from_state_code VARCHAR(2) NOT NULL,
    to_gstin VARCHAR(15) NULL,
    to_name VARCHAR(200) NOT NULL,
    to_address VARCHAR(500) NOT NULL,
    to_place VARCHAR(100) NOT NULL,
    to_pincode VARCHAR(10) NOT NULL,
    to_state_code VARCHAR(2) NOT NULL,
    total_invoice_value DECIMAL(18,2) NOT NULL DEFAULT 0,
    taxable_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    cgst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    sgst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    igst_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    cess_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    transportation_mode INT NOT NULL DEFAULT 1,
    transporter_id VARCHAR(50) NULL,
    transporter_name VARCHAR(200) NULL,
    transport_doc_no VARCHAR(50) NULL,
    transport_doc_date TIMESTAMP WITH TIME ZONE NULL,
    vehicle_no VARCHAR(50) NULL,
    vehicle_type INT NOT NULL DEFAULT 1,
    approximate_distance_km INT NOT NULL DEFAULT 10,
    cancel_reason VARCHAR(100) NULL,
    cancel_remarks VARCHAR(500) NULL,
    cancelled_at_utc TIMESTAMP WITH TIME ZONE NULL,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at_utc TIMESTAMP WITH TIME ZONE NULL
);

CREATE TABLE IF NOT EXISTS einvoices (
    id UUID PRIMARY KEY,
    sales_document_id UUID NOT NULL,
    irn VARCHAR(100) NOT NULL,
    ack_no VARCHAR(50) NOT NULL,
    ack_date TIMESTAMP WITH TIME ZONE NOT NULL,
    signed_invoice TEXT NOT NULL,
    signed_qr_code TEXT NOT NULL,
    status INT NOT NULL,
    cancel_reason INT NULL,
    cancel_remarks VARCHAR(500) NULL,
    cancelled_at_utc TIMESTAMP WITH TIME ZONE NULL,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at_utc TIMESTAMP WITH TIME ZONE NULL
);
";
        await conn.ExecuteAsync(sql);

        try
        {
            await conn.ExecuteAsync("ALTER TABLE sales_documents ADD COLUMN IF NOT EXISTS paid_amount DECIMAL(18,2) NOT NULL DEFAULT 0;");
        }
        catch
        {
            // Column already exists
        }

        try { await conn.ExecuteAsync("ALTER TABLE company_profile ADD COLUMN IF NOT EXISTS letterhead_image_path VARCHAR(500) NULL;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE company_profile ADD COLUMN IF NOT EXISTS default_letterhead_mode INT NOT NULL DEFAULT 1;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE company_profile ADD COLUMN IF NOT EXISTS preprinted_header_margin_mm INT NOT NULL DEFAULT 50;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE sales_documents ADD COLUMN IF NOT EXISTS source_document_id UUID NULL;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE sales_documents ADD COLUMN IF NOT EXISTS source_document_number VARCHAR(50) NULL;"); } catch {}
        try { await conn.ExecuteAsync("ALTER TABLE sales_documents ADD COLUMN IF NOT EXISTS converted_to_document_id UUID NULL;"); } catch {}
    }
}
