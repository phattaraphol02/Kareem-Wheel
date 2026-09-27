/* ============================================================
   Kareem Wheel — สคริปต์สร้างฐานข้อมูลทั้งหมด
   ------------------------------------------------------------
   ใช้สำหรับติดตั้งระบบบนเครื่องใหม่ หรือส่งพร้อมโปรเจกต์

   วิธีใช้:
     1) เปิด SQL Server Management Studio
     2) สร้างฐานข้อมูลชื่อ wheel  (CREATE DATABASE wheel;)
     3) เปิดไฟล์นี้แล้วกด Execute

   สคริปต์นี้รันซ้ำได้ (ถ้ามีตารางอยู่แล้วจะข้ามไป ไม่ลบข้อมูล)
   ============================================================ */

USE wheel;
GO

/* ------------------------------------------------------------
   1. ข้อมูลที่อยู่ (จังหวัด / อำเภอ / ตำบล)
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Provinces', 'U') IS NULL
CREATE TABLE dbo.Provinces (
    Province_Id     NCHAR(10)     NOT NULL PRIMARY KEY,
    Province_Name   NCHAR(100)    NOT NULL
);
GO

IF OBJECT_ID('dbo.Amphurs', 'U') IS NULL
CREATE TABLE dbo.Amphurs (
    Amphur_Id       NCHAR(10)     NOT NULL PRIMARY KEY,
    Amphur_Name     NCHAR(100)    NOT NULL,
    Province_Id     NCHAR(10)     NOT NULL
);
GO

IF OBJECT_ID('dbo.Districts', 'U') IS NULL
CREATE TABLE dbo.Districts (
    District_Id     NVARCHAR(50)  NOT NULL PRIMARY KEY,
    District_Name   NVARCHAR(50)  NOT NULL,
    Amphur_Id       NCHAR(10)     NOT NULL,
    PostCodeMain    NVARCHAR(50)  NOT NULL
);
GO

/* ------------------------------------------------------------
   2. ผู้ใช้งานระบบ
      Role: 1 = ลูกค้า, 2 = พนักงาน/ช่าง, 9 = เจ้าของร้าน (แอดมิน)
      Password เก็บเป็น hash เท่านั้น (ASP.NET PasswordHasher)
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Customers', 'U') IS NULL
CREATE TABLE dbo.Customers (
    Customer_Id     INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Customer_Name   NCHAR(50)     NOT NULL,
    Customer_Email  NCHAR(50)     NOT NULL,
    Address         NCHAR(100)    NOT NULL,
    Amphur_Id       NCHAR(10)     NOT NULL,
    Province_Id     NCHAR(10)     NOT NULL,
    PostalCode      NCHAR(5)      NOT NULL,
    Phone           NCHAR(15)     NOT NULL,
    Password        NCHAR(255)    NOT NULL,
    Role            INT           NOT NULL DEFAULT 1,
    IsActive        BIT           NOT NULL DEFAULT 1,
    District_Id     NVARCHAR(50)  NULL
);
GO

/* ------------------------------------------------------------
   3. สินค้า
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Brands', 'U') IS NULL
CREATE TABLE dbo.Brands (
    Brand_Id        INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Brand_Name      NVARCHAR(100) NULL
);
GO

IF OBJECT_ID('dbo.Categories', 'U') IS NULL
CREATE TABLE dbo.Categories (
    Category_Id     INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Category_Name   NVARCHAR(100) NOT NULL
);
GO

IF OBJECT_ID('dbo.Products', 'U') IS NULL
CREATE TABLE dbo.Products (
    Product_Id      INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Product_Name    NVARCHAR(500)  NOT NULL,
    Description     NVARCHAR(MAX)  NULL,
    Brand_Id        INT            NOT NULL,
    Price           DECIMAL(18,2)  NULL,
    ImageFileName   NVARCHAR(MAX)  NOT NULL,
    CreateAt        DATETIME       NULL,
    Category_Id     INT            NULL,
    Quantity        INT            NOT NULL DEFAULT 0,   -- สต็อกคงเหลือ
    IsActive        BIT            NOT NULL DEFAULT 1,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (Category_Id) REFERENCES dbo.Categories(Category_Id)
);
GO

/* ------------------------------------------------------------
   4. บริการ
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Services', 'U') IS NULL
CREATE TABLE dbo.Services (
    Service_Id      INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Service_Name    NVARCHAR(MAX)  NOT NULL,
    Price           DECIMAL(18,2)  NOT NULL,
    Description     NVARCHAR(MAX)  NULL,
    Duration        INT            NOT NULL,   -- นาที (ต่ำกว่า 60 = ไม่เปิดให้จองผ่านเว็บ)
    IsActive        BIT            NOT NULL DEFAULT 1
);
GO

/* ------------------------------------------------------------
   5. คำสั่งซื้อ
      Status: New -> PendingVerify -> Processing -> Shipped -> Delivered
      OrderNumber: รูปแบบ ORD0001 (สร้างอัตโนมัติ)
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Orders', 'U') IS NULL
CREATE TABLE dbo.Orders (
    Order_Id        INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Customer_Id     INT            NOT NULL,
    OrderNumber     NVARCHAR(20)   NULL,
    OrderDate       DATETIME       NOT NULL,
    GrandTotal      DECIMAL(18,2)  NOT NULL,
    Status          NVARCHAR(100)  NOT NULL,
    DeliveryMethod  NVARCHAR(20)   NOT NULL DEFAULT 'Delivery',
    AppointmentDate DATETIME2      NULL,       -- กรณีนัดรับที่ร้าน
    SlipImage       NVARCHAR(255)  NULL,
    PaidAt          DATETIME       NULL,
    ShippingCompany NVARCHAR(100)  NULL,
    TrackingNo      NVARCHAR(20)   NULL,
    ShippingStatus  NVARCHAR(50)   NOT NULL DEFAULT 'Pending',
    ShippedAt       DATETIME       NULL,
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (Customer_Id) REFERENCES dbo.Customers(Customer_Id)
);
GO

IF OBJECT_ID('dbo.OrderDetails', 'U') IS NULL
CREATE TABLE dbo.OrderDetails (
    OrderDetail_Id  INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Order_Id        INT            NOT NULL,
    Product_Id      INT            NOT NULL,
    Price           DECIMAL(18,2)  NOT NULL,
    Qty             INT            NOT NULL,
    SubTotal        DECIMAL(18,2)  NOT NULL,
    CONSTRAINT FK_OrderDetails_Orders   FOREIGN KEY (Order_Id)   REFERENCES dbo.Orders(Order_Id),
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (Product_Id) REFERENCES dbo.Products(Product_Id)
);
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
CREATE TABLE dbo.Payments (
    Payment_Id        INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Order_Id          INT            NOT NULL,
    PaidAt            DATETIME       NOT NULL,
    Amount            DECIMAL(18,2)  NOT NULL,
    SlipFileName      NVARCHAR(MAX)  NULL,
    Status            NCHAR(10)      NULL,
    VerifiedAt        DATETIME       NULL,
    VerifiedByAdminId INT            NULL,
    AdminNote         NCHAR(50)      NULL,
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (Order_Id) REFERENCES dbo.Orders(Order_Id)
);
GO

/* ------------------------------------------------------------
   6. การนัดหมายบริการ
      Status: รออนุมัติ -> ยืนยันแล้ว -> กำลังทำ
              -> รอชำระเงิน -> รอยืนยันชำระเงิน -> เสร็จสิ้น
      (หรือ ยกเลิกแล้ว)
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Appointments', 'U') IS NULL
CREATE TABLE dbo.Appointments (
    Id                  INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Customer_Id         INT            NOT NULL,
    Service_Id          INT            NOT NULL,
    Appointment_Date    DATETIME2      NOT NULL,
    Status              NVARCHAR(MAX)  NULL,
    Notes               NVARCHAR(MAX)  NULL,
    Employee_Id         NVARCHAR(100)  NULL,   -- Customer_Id ของช่างที่รับงาน
    IsReminderSent      BIT            NOT NULL DEFAULT 0,
    IsCustomerConfirmed BIT            NOT NULL DEFAULT 0,
    ConfirmToken        NVARCHAR(100)  NULL,   -- โทเค็นยืนยันคิวทางอีเมล (ใช้ครั้งเดียว)
    TokenExpiry         DATETIME2      NULL,
    ServicePaymentSlip  NVARCHAR(255)  NULL,   -- สลิปค่าบริการ
    ServicePaidAt       DATETIME2      NULL,
    CONSTRAINT FK_Appointments_Customers FOREIGN KEY (Customer_Id) REFERENCES dbo.Customers(Customer_Id),
    CONSTRAINT FK_Appointments_Services  FOREIGN KEY (Service_Id)  REFERENCES dbo.Services(Service_Id)
);
GO

/* ------------------------------------------------------------
   7. อะไหล่ และการเบิกอะไหล่
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.SpareParts', 'U') IS NULL
CREATE TABLE dbo.SpareParts (
    Id              INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name            NVARCHAR(255)  NOT NULL,
    Price           DECIMAL(18,2)  NOT NULL,
    StockQuantity   INT            NOT NULL DEFAULT 0
);
GO

-- พนักงานเบิกอะไหล่ -> ตัดสต็อกทันที ไม่ต้องรออนุมัติ
IF OBJECT_ID('dbo.SparePartRequisitions', 'U') IS NULL
CREATE TABLE dbo.SparePartRequisitions (
    Id              INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SparePart_Id    INT            NOT NULL,
    Employee_Id     INT            NOT NULL,
    Qty             INT            NOT NULL,
    RequestedAt     DATETIME2      NOT NULL,
    Appointment_Id  INT            NULL,       -- งานที่นำอะไหล่ไปใช้ (ถ้าระบุ)
    Note            NVARCHAR(255)  NULL,
    CONSTRAINT FK_Req_SparePart FOREIGN KEY (SparePart_Id)   REFERENCES dbo.SpareParts(Id),
    CONSTRAINT FK_Req_Employee  FOREIGN KEY (Employee_Id)    REFERENCES dbo.Customers(Customer_Id),
    CONSTRAINT FK_Req_Appt      FOREIGN KEY (Appointment_Id) REFERENCES dbo.Appointments(Id)
);
GO

/* ------------------------------------------------------------
   8. สั่งซื้อสินค้าเข้าร้าน (จัดซื้อ)
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Suppliers', 'U') IS NULL
CREATE TABLE dbo.Suppliers (
    SupplierId      INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name            NVARCHAR(500)  NOT NULL,
    SupplierType    NVARCHAR(100)  NULL,
    Phone           NVARCHAR(20)   NULL,
    IsActive        BIT            NOT NULL DEFAULT 1
);
GO

IF OBJECT_ID('dbo.PurchaseOrders', 'U') IS NULL
CREATE TABLE dbo.PurchaseOrders (
    PO_Id           INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SupplierId      INT            NOT NULL,
    OrderDate       DATETIME       NULL,
    Status          NVARCHAR(50)   NULL,       -- Pending / PartiallyReceived / Completed
    GrandTotal      DECIMAL(18,2)  NULL,
    ReceivedAt      DATETIME       NULL,
    CONSTRAINT FK_PO_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(SupplierId)
);
GO

-- สั่งได้ 2 แบบต่อแถว: สินค้าขาย (ProductId) หรือ อะไหล่ซ่อม (SparePartId)
-- ItemType บอกว่าแถวนี้คือแบบไหน มีค่าแค่ฝั่งเดียว อีกฝั่งเป็น NULL
IF OBJECT_ID('dbo.PurchaseOrderDetails', 'U') IS NULL
CREATE TABLE dbo.PurchaseOrderDetails (
    DetailId        INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PO_Id           INT            NOT NULL,
    ItemType        NVARCHAR(20)   NOT NULL DEFAULT N'Product',  -- Product / SparePart
    ProductId       INT            NULL,
    SparePartId     INT            NULL,
    Quantity        INT            NOT NULL,
    CostPrice       DECIMAL(18,2)  NOT NULL,   -- ต้นทุน ใช้คำนวณรายจ่ายในหน้าการเงิน
    Condition       NVARCHAR(10)   NOT NULL DEFAULT N'มือ1',  -- มือ1 / มือ2 (อะไหล่บังคับมือ1)
    ReceivedQty     INT            NULL,       -- จำนวนที่ตรวจนับได้จริงตอนรับของ
    CONSTRAINT FK_POD_PurchaseOrders FOREIGN KEY (PO_Id)       REFERENCES dbo.PurchaseOrders(PO_Id),
    CONSTRAINT FK_POD_Products       FOREIGN KEY (ProductId)   REFERENCES dbo.Products(Product_Id),
    CONSTRAINT FK_POD_SpareParts     FOREIGN KEY (SparePartId) REFERENCES dbo.SpareParts(Id)
);
GO

/* ------------------------------------------------------------
   9. รายจ่ายของร้าน (บันทึกเอง)
      ต้นทุนสินค้าเข้าร้านคำนวณจาก PurchaseOrderDetails ไม่ต้องบันทึกซ้ำที่นี่
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Expenses', 'U') IS NULL
CREATE TABLE dbo.Expenses (
    Expense_Id      INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Title           NVARCHAR(200)  NOT NULL,
    Category        NVARCHAR(50)   NOT NULL,
    Amount          DECIMAL(18,2)  NOT NULL,
    ExpenseDate     DATETIME2      NOT NULL,
    Note            NVARCHAR(255)  NULL
);
GO

/* ------------------------------------------------------------
   10. แบนเนอร์หน้าแรก (เจ้าของร้านอัปโหลด/ปิด/ลบเองได้)
       ไฟล์รูปเก็บที่ wwwroot/banners/
       ถ้าไม่มีแบนเนอร์ที่ IsActive = 1 หน้าแรกจะใช้รูปเริ่มต้นของระบบ
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Banners', 'U') IS NULL
CREATE TABLE dbo.Banners (
    Banner_Id       INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Title           NVARCHAR(150)  NOT NULL,
    Subtitle        NVARCHAR(200)  NULL,
    ImageFileName   NVARCHAR(255)  NOT NULL,
    IsActive        BIT            NOT NULL DEFAULT 1,
    SortOrder       INT            NOT NULL DEFAULT 0,   -- เลขน้อยแสดงก่อน
    StartDate       DATETIME2      NULL,                 -- NULL = ไม่จำกัดวันเริ่ม
    EndDate         DATETIME2      NULL,                 -- NULL = ไม่มีวันหมดอายุ
    CreatedAt       DATETIME2      NOT NULL
);
GO

/* ------------------------------------------------------------
   11. งานที่แอดมินมอบหมายให้พนักงาน
   ------------------------------------------------------------ */

IF OBJECT_ID('dbo.Tasks', 'U') IS NULL
CREATE TABLE dbo.Tasks (
    Id              INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Title           NVARCHAR(255)  NOT NULL,
    Description     NVARCHAR(MAX)  NULL,
    EmployeeId      NVARCHAR(50)   NOT NULL,
    CustomerId      NVARCHAR(50)   NULL,
    Priority        NVARCHAR(20)   NOT NULL,
    Status          NVARCHAR(20)   NOT NULL,
    DueDate         DATETIME       NOT NULL,
    CreatedAt       DATETIME       NOT NULL
);
GO

PRINT 'สร้างตารางเรียบร้อยแล้ว';
GO
