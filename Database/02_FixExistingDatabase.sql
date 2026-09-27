/* ============================================================
   Kareem Wheel — แก้ไขฐานข้อมูลเดิมที่ใช้งานอยู่
   ------------------------------------------------------------
   ⚠️ อ่านก่อนรัน! สคริปต์นี้แก้ฐานข้อมูลที่มีข้อมูลจริงอยู่แล้ว
      ควรสำรองข้อมูลก่อน (Backup Database) ทุกครั้ง

   แบ่งเป็น 3 ส่วน รันแยกทีละส่วนได้:
     ส่วนที่ 1 — ตรวจสอบข้อมูลกำพร้า (รันก่อนเสมอ ไม่แก้อะไร)
     ส่วนที่ 2 — แก้ชนิดข้อมูลที่ผิด
     ส่วนที่ 3 — เพิ่ม Foreign Key ที่ขาด
   ============================================================ */

USE wheel;
GO

/* ============================================================
   ส่วนที่ 1 — ตรวจสอบก่อน (ปลอดภัย ไม่แก้ข้อมูล)
   ------------------------------------------------------------
   ถ้าทุกบรรทัดขึ้น 0 แปลว่าเพิ่ม Foreign Key ได้เลย
   ถ้ามีตัวเลขมากกว่า 0 = มีข้อมูลกำพร้า ต้องลบ/แก้ก่อน
   ============================================================ */

SELECT 'Orders ที่ไม่มีลูกค้าจริง' AS ปัญหา,
       COUNT(*) AS จำนวน
FROM dbo.Orders o
WHERE NOT EXISTS (SELECT 1 FROM dbo.Customers c WHERE c.Customer_Id = o.Customer_Id)

UNION ALL
SELECT 'OrderDetails ที่ไม่มีออเดอร์จริง',
       COUNT(*)
FROM dbo.OrderDetails d
WHERE NOT EXISTS (SELECT 1 FROM dbo.Orders o WHERE o.Order_Id = d.Order_Id)

UNION ALL
SELECT 'OrderDetails ที่ไม่มีสินค้าจริง',
       COUNT(*)
FROM dbo.OrderDetails d
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products p WHERE p.Product_Id = d.Product_Id)

UNION ALL
SELECT 'Products ที่ไม่มีแบรนด์จริง',
       COUNT(*)
FROM dbo.Products p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Brands b WHERE b.Brand_Id = p.Brand_Id);
GO


/* ============================================================
   ส่วนที่ 2 — แก้ชนิดข้อมูลที่ผิด
   ------------------------------------------------------------
   Payments.Amount เป็น DECIMAL(18,0) = ไม่เก็บทศนิยม
   ทำให้ยอดเงิน 1,250.75 ถูกปัดเป็น 1,251 บาท
   ============================================================ */

IF EXISTS (
    SELECT 1 FROM sys.columns c
    JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = 'Payments' AND c.name = 'Amount' AND c.scale = 0
)
BEGIN
    ALTER TABLE dbo.Payments ALTER COLUMN Amount DECIMAL(18,2) NOT NULL;
    PRINT 'แก้ Payments.Amount เป็น DECIMAL(18,2) เรียบร้อย';
END
ELSE
    PRINT 'Payments.Amount ถูกต้องอยู่แล้ว ข้ามไป';
GO


/* ============================================================
   ส่วนที่ 3 — เพิ่ม Foreign Key ที่ขาด
   ------------------------------------------------------------
   ป้องกันข้อมูลขยะ เช่น ออเดอร์ที่ชี้ไปหาลูกค้าที่ถูกลบไปแล้ว
   ⚠️ รันส่วนที่ 1 ให้ผ่าน (ได้ 0 ทุกบรรทัด) ก่อนรันส่วนนี้
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_Customers')
    ALTER TABLE dbo.Orders
    ADD CONSTRAINT FK_Orders_Customers
        FOREIGN KEY (Customer_Id) REFERENCES dbo.Customers(Customer_Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderDetails_Orders')
    ALTER TABLE dbo.OrderDetails
    ADD CONSTRAINT FK_OrderDetails_Orders
        FOREIGN KEY (Order_Id) REFERENCES dbo.Orders(Order_Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderDetails_Products')
    ALTER TABLE dbo.OrderDetails
    ADD CONSTRAINT FK_OrderDetails_Products
        FOREIGN KEY (Product_Id) REFERENCES dbo.Products(Product_Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Products_Brands')
    ALTER TABLE dbo.Products
    ADD CONSTRAINT FK_Products_Brands
        FOREIGN KEY (Brand_Id) REFERENCES dbo.Brands(Brand_Id);
GO

PRINT 'เพิ่ม Foreign Key เรียบร้อยแล้ว';
GO


/* ============================================================
   ส่วนที่ 4 (ทางเลือก) — ลบตารางเก่าที่ไม่ได้ใช้
   ------------------------------------------------------------
   PartRequisitions เป็นตารางเบิกอะไหล่รุ่นเก่า
   ไม่มีโค้ดส่วนไหนในระบบเรียกใช้แล้ว
   ระบบปัจจุบันใช้ SparePartRequisitions แทน

   ⚠️ ตรวจสอบก่อนว่าไม่มีข้อมูลสำคัญ:
       SELECT * FROM dbo.PartRequisitions;

   ถ้าแน่ใจแล้วค่อยลบ comment ออกแล้วรัน
   ============================================================ */

-- DROP TABLE dbo.PartRequisitions;
-- GO


/* ============================================================
   ส่วนที่ 5 — ระบบจัดซื้อสินค้าเข้าร้าน (PO) รุ่นใหม่
   ------------------------------------------------------------
   เพิ่มคอลัมน์ที่ขาด: มือ1/มือ2, จำนวนที่ได้รับจริง,
   ข้อมูลติดต่อซัพพลายเออร์ และวันที่รับของ
   รันซ้ำได้ ไม่กระทบข้อมูลเดิม
   ============================================================ */

IF COL_LENGTH('dbo.Suppliers', 'Phone') IS NULL
    ALTER TABLE dbo.Suppliers ADD Phone NVARCHAR(20) NULL;
GO

IF COL_LENGTH('dbo.Suppliers', 'IsActive') IS NULL
    ALTER TABLE dbo.Suppliers ADD IsActive BIT NOT NULL DEFAULT 1;
GO

IF COL_LENGTH('dbo.PurchaseOrders', 'ReceivedAt') IS NULL
    ALTER TABLE dbo.PurchaseOrders ADD ReceivedAt DATETIME NULL;
GO

IF COL_LENGTH('dbo.PurchaseOrderDetails', 'Condition') IS NULL
    ALTER TABLE dbo.PurchaseOrderDetails ADD Condition NVARCHAR(10) NOT NULL DEFAULT N'มือ1';
GO

IF COL_LENGTH('dbo.PurchaseOrderDetails', 'ReceivedQty') IS NULL
    ALTER TABLE dbo.PurchaseOrderDetails ADD ReceivedQty INT NULL;
GO

-- รองรับสั่งซื้อ "อะไหล่" (SparePart) ร่วมกับ "สินค้า" (Product) ในใบเดียวกัน
-- ต้องทำ ProductId เป็น NULL ได้ก่อน เพราะแถวที่เป็นอะไหล่จะไม่มี ProductId
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PurchaseOrderDetails') AND name='ProductId' AND is_nullable = 0)
BEGIN
    DECLARE @fk NVARCHAR(200);
    SELECT @fk = name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.PurchaseOrderDetails') AND referenced_object_id = OBJECT_ID('dbo.Products');
    IF @fk IS NOT NULL EXEC('ALTER TABLE dbo.PurchaseOrderDetails DROP CONSTRAINT ' + @fk);
    ALTER TABLE dbo.PurchaseOrderDetails ALTER COLUMN ProductId INT NULL;
    ALTER TABLE dbo.PurchaseOrderDetails ADD CONSTRAINT FK_POD_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Product_Id);
END
GO

IF COL_LENGTH('dbo.PurchaseOrderDetails','ItemType') IS NULL
    ALTER TABLE dbo.PurchaseOrderDetails ADD ItemType NVARCHAR(20) NOT NULL DEFAULT N'Product';
GO

IF COL_LENGTH('dbo.PurchaseOrderDetails','SparePartId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderDetails ADD SparePartId INT NULL;
    ALTER TABLE dbo.PurchaseOrderDetails ADD CONSTRAINT FK_POD_SpareParts FOREIGN KEY (SparePartId) REFERENCES dbo.SpareParts(Id);
END
GO

PRINT 'อัปเดตระบบจัดซื้อสินค้าเรียบร้อยแล้ว';
GO
