USE shopkeeper_db;
GO

IF NOT EXISTS (SELECT * FROM tblProduct WHERE pcode = '1104')
BEGIN
    INSERT INTO tblProduct (pcode, barcode, pdescription, price, cost_price, qty, reorder, is_active)
    VALUES 
    ('1104', '890103001', 'Coca Cola 500ml Bottle', 180.00, 140.00, 50, 12, 1),
    ('1105', '890103002', 'Sprite Lemon Lime 500ml', 180.00, 140.00, 38, 12, 1),
    ('1106', '890103003', 'Nescafe Classic Coffee 50g', 450.00, 380.00, 25, 8, 1),
    ('1107', '890103004', 'Maggi 2-Minute Noodles', 110.00, 85.00, 100, 25, 1),
    ('1108', '890103005', 'Anchor Milk Powder 400g', 980.00, 890.00, 18, 5, 1),
    ('1109', '890103006', 'Munchee Chocolate Biscuits', 160.00, 125.00, 75, 15, 1),
    ('1110', '890103007', 'Comfort Fabric Conditioner', 680.00, 560.00, 14, 5, 1);
END
GO
