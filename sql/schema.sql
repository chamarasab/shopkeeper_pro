-- ShopKeeperPro SQL Database Schema & Migration Script
-- Compatible with local MSSQL on Windows (localhost / .)

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'shopkeeper_db')
BEGIN
    CREATE DATABASE shopkeeper_db;
END
GO

USE shopkeeper_db;
GO

-- 1. tblBrand
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblBrand')
BEGIN
    CREATE TABLE tblBrand (
        id INT IDENTITY(1,1) PRIMARY KEY,
        brand NVARCHAR(100) NOT NULL
    );
END
GO

-- 2. tblCategory
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblCategory')
BEGIN
    CREATE TABLE tblCategory (
        id INT IDENTITY(1,1) PRIMARY KEY,
        category NVARCHAR(100) NOT NULL
    );
END
GO

-- 3. tblProduct
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblProduct')
BEGIN
    CREATE TABLE tblProduct (
        pcode VARCHAR(50) PRIMARY KEY,
        barcode VARCHAR(50) NOT NULL,
        pdescription NVARCHAR(255) NOT NULL,
        brand_id INT NULL,
        category_id INT NULL,
        price DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        cost_price DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        qty INT NOT NULL DEFAULT 0,
        reorder INT NOT NULL DEFAULT 10,
        is_active BIT NOT NULL DEFAULT 1
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblProduct' AND COLUMN_NAME = 'cost_price')
        ALTER TABLE tblProduct ADD cost_price DECIMAL(18,2) NOT NULL DEFAULT 0.00;
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblProduct' AND COLUMN_NAME = 'is_active')
        ALTER TABLE tblProduct ADD is_active BIT NOT NULL DEFAULT 1;
END
GO

-- Create index on barcode for ultra-fast barcode scanner lookups (<1ms)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblProduct_Barcode' AND object_id = OBJECT_ID('tblProduct'))
BEGIN
    CREATE INDEX IX_tblProduct_Barcode ON tblProduct (barcode);
END
GO

-- 4. tblUser
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblUser')
BEGIN
    CREATE TABLE tblUser (
        username VARCHAR(50) PRIMARY KEY,
        password VARCHAR(50) NOT NULL,
        role VARCHAR(50) NOT NULL DEFAULT 'Cashier',
        name VARCHAR(50) NOT NULL,
        is_active BIT NOT NULL DEFAULT 1
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblUser' AND COLUMN_NAME = 'is_active')
        ALTER TABLE tblUser ADD is_active BIT NOT NULL DEFAULT 1;
END
GO

-- 5. tblVat
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblVat')
BEGIN
    CREATE TABLE tblVat (
        vat DECIMAL(18,2) NOT NULL DEFAULT 0.12
    );
    INSERT INTO tblVat (vat) VALUES (0.12);
END
GO

-- 6. tblCart (Active session / pending cart items)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblCart')
BEGIN
    CREATE TABLE tblCart (
        id INT IDENTITY(1,1) PRIMARY KEY,
        tr_no VARCHAR(50) NOT NULL,
        pid VARCHAR(50) NOT NULL,
        price DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        qty INT NOT NULL DEFAULT 1,
        disc DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        total DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        sdate DATETIME NOT NULL DEFAULT GETDATE(),
        status VARCHAR(50) NOT NULL DEFAULT 'Pending',
        cashier VARCHAR(50) NOT NULL
    );
END
GO

-- 7. tblSale (Completed transaction records)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblSale')
BEGIN
    CREATE TABLE tblSale (
        tr_no VARCHAR(50) PRIMARY KEY,
        sdate DATETIME NOT NULL DEFAULT GETDATE(),
        subtotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        discount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        vat DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        total DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        cash_tendered DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        change_due DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        payment_method VARCHAR(50) NOT NULL DEFAULT 'Cash',
        cashier VARCHAR(50) NOT NULL,
        customer_name VARCHAR(100) NULL,
        item_count INT NOT NULL DEFAULT 0
    );
END
GO

-- 8. tblVendor
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblVendor')
BEGIN
    CREATE TABLE tblVendor (
        id INT IDENTITY(1,1) PRIMARY KEY,
        vendor VARCHAR(100) NOT NULL,
        address VARCHAR(255) NULL,
        contactperson VARCHAR(100) NULL,
        telephone VARCHAR(50) NULL,
        email VARCHAR(100) NULL,
        fax VARCHAR(50) NULL
    );
END
GO

-- 9. tblStockIn
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblStockIn')
BEGIN
    CREATE TABLE tblStockIn (
        id INT IDENTITY(1,1) PRIMARY KEY,
        refno VARCHAR(50) NOT NULL,
        pcode VARCHAR(50) NOT NULL,
        qty INT NOT NULL DEFAULT 0,
        sdate DATETIME NOT NULL DEFAULT GETDATE(),
        stockinby VARCHAR(50) NOT NULL,
        status VARCHAR(50) NOT NULL DEFAULT 'Pending',
        vendorid INT NULL
    );
END
GO

-- 10. tblAdjustment
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblAdjustment')
BEGIN
    CREATE TABLE tblAdjustment (
        id INT IDENTITY(1,1) PRIMARY KEY,
        referenceno VARCHAR(50) NOT NULL,
        pcode VARCHAR(50) NOT NULL,
        qty INT NOT NULL DEFAULT 0,
        action VARCHAR(50) NOT NULL,
        remarks VARCHAR(255) NULL,
        sdate DATETIME NOT NULL DEFAULT GETDATE(),
        [user] VARCHAR(50) NOT NULL
    );
END
GO

-- Views
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vwCriticalItems')
BEGIN
    EXEC('CREATE VIEW vwCriticalItems AS
    SELECT p.pcode, p.barcode, p.pdescription, ISNULL(b.brand, ''N/A'') AS brand, ISNULL(c.category, ''N/A'') AS category, p.price, p.reorder, p.qty
    FROM tblProduct AS p
    LEFT JOIN tblBrand AS b ON b.id = p.brand_id
    LEFT JOIN tblCategory AS c ON c.id = p.category_id
    WHERE p.qty <= p.reorder;');
END
GO

IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vwSoldItems')
BEGIN
    EXEC('CREATE VIEW vwSoldItems AS
    SELECT c.pid, c.qty, p.pdescription, c.sdate, c.status, c.total, c.cashier, c.tr_no
    FROM tblCart AS c
    INNER JOIN tblProduct AS p ON p.pcode = c.pid;');
END
GO

IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vwStockIn')
BEGIN
    EXEC('CREATE VIEW vwStockIn AS
    SELECT s.id, s.refno, s.pcode, s.qty, s.sdate, s.stockinby, s.status, ISNULL(v.vendor, ''Direct'') AS vendor, p.pdescription
    FROM tblStockIn AS s
    INNER JOIN tblProduct AS p ON p.pcode = s.pcode
    LEFT JOIN tblVendor AS v ON v.id = s.vendorid;');
END
GO

-- Seed Baseline Admin User if tblUser is empty
IF NOT EXISTS (SELECT * FROM tblUser WHERE username = 'admin')
BEGIN
    INSERT INTO tblUser (username, password, role, name)
    VALUES ('admin', 'admin123', 'Administrator', 'System Admin');
END
IF NOT EXISTS (SELECT * FROM tblUser WHERE username = 'cashier1')
BEGIN
    INSERT INTO tblUser (username, password, role, name)
    VALUES ('cashier1', '1234', 'Cashier', 'Alex Cashier');
END
GO

-- Seed Sample Categories if empty
IF NOT EXISTS (SELECT * FROM tblCategory)
BEGIN
    INSERT INTO tblCategory (category) VALUES 
    ('Beverages'),
    ('Snacks & Biscuits'),
    ('Personal Care'),
    ('Dairy & Eggs'),
    ('Canned Goods'),
    ('Household Essentials');
END
GO

-- Seed Sample Brands if empty
IF NOT EXISTS (SELECT * FROM tblBrand)
BEGIN
    INSERT INTO tblBrand (brand) VALUES 
    ('Coca-Cola'),
    ('Nestle'),
    ('Unilever'),
    ('P&G'),
    ('Kraft Heinz'),
    ('Local Fresh');
END
GO

-- Seed Products if empty
IF NOT EXISTS (SELECT * FROM tblProduct)
BEGIN
    INSERT INTO tblProduct (pcode, barcode, pdescription, brand_id, category_id, price, cost_price, qty, reorder)
    VALUES 
    ('P1001', '123456789', 'Sunlight Soap 120g', 3, 3, 120.00, 95.00, 45, 15),
    ('P1002', '123456788', 'Lux Beauty Bar 100g', 3, 3, 150.00, 115.00, 32, 10),
    ('P1003', '123456787', 'Cloguard Toothbrush Medium', 3, 3, 70.00, 48.00, 60, 20),
    ('P1004', '890103001', 'Coca Cola 500ml Bottle', 1, 1, 180.00, 140.00, 50, 12),
    ('P1005', '890103002', 'Sprite Lemon Lime 500ml', 1, 1, 180.00, 140.00, 38, 12),
    ('P1006', '890103003', 'Nescafe Classic Instant Coffee 50g', 2, 1, 450.00, 380.00, 25, 8),
    ('P1007', '890103004', 'Maggi 2-Minute Noodles Chicken', 2, 5, 110.00, 85.00, 100, 25),
    ('P1008', '890103005', 'Anchor Full Cream Milk Powder 400g', 2, 4, 980.00, 890.00, 18, 5),
    ('P1009', '890103006', 'Munchee Chocolate Cream Biscuits', 6, 2, 160.00, 125.00, 75, 15),
    ('P1010', '890103007', 'Comfort Fabric Conditioner 800ml', 3, 6, 680.00, 560.00, 14, 5);
END
GO
