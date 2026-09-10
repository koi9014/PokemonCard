/*
    PiCartChu database schema
    Target: Microsoft SQL Server

    Creates a fresh database, all required tables, and the minimum demo
    administrator records needed by the administration area.

    Safety: if dbo.Users already exists, execution stops without deleting or
    overwriting existing schema or data.
*/

SET NOCOUNT ON;
GO

IF DB_ID(N'Picartchu') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [Picartchu] COLLATE Chinese_Taiwan_Stroke_90_CI_AI;');
END;
GO

USE [Picartchu]
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
    BEGIN
        THROW 50001, N'Picartchu 已存在資料表；為避免覆蓋資料，已停止執行 schema.sql。', 1;
    END;

CREATE TABLE Users
(
    User_ID INT IDENTITY(1,1) NOT NULL,

    Display_Name NVARCHAR(10) NULL,
    Username VARCHAR(255) NULL,
    Email VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NULL,
    Phone VARCHAR(20) NULL,
    Avatar VARCHAR(255) NULL,
    Birthday DATE NULL,

    Provider VARCHAR(20) NOT NULL
        DEFAULT 'LOCAL',

    Provider_ID VARCHAR(255) NULL,

    User_Status VARCHAR(20) NOT NULL
        DEFAULT 'ACTIVE',

    Seller_Verification_Status VARCHAR(20) NOT NULL
        DEFAULT 'NONE',

    Last_Login_At DATETIME2 NULL,

    UserCreated_At DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    UserUpdated_At DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT Users_User_ID_PK 
        PRIMARY KEY (User_ID),

    CONSTRAINT Users_Email_UQ 
        UNIQUE (Email),

    CONSTRAINT Users_Provider_CK
        CHECK (
            Provider IN
            (
                'LOCAL',
                'GOOGLE'
            )
        ),

    CONSTRAINT Users_Provider_ID_CK
    CHECK
    (
        (Provider = 'LOCAL' AND Provider_ID IS NULL)
        OR
        (Provider = 'GOOGLE' AND Provider_ID IS NOT NULL)
    ),
    CONSTRAINT Users_User_Status_CK
CHECK (
    User_Status IN (
        'ACTIVE',
        'SUSPENDED',
        'BANNED',
        'INACTIVE'
    )
),

    CONSTRAINT Users_Seller_Verification_Status_CK
CHECK (
    Seller_Verification_Status IN (
        'NONE',
        'PENDING',
        'APPROVED',
        'REJECTED'
    )
)
);

-- Username 不可重複，但允許 NULL
CREATE UNIQUE INDEX Users_Username_UQ
ON Users(Username)
WHERE Username IS NOT NULL;

-- 第三方登入的 Provider_ID 不可重複
CREATE UNIQUE INDEX Users_Provider_ID_UQ
ON Users(Provider, Provider_ID)
WHERE Provider_ID IS NOT NULL;

CREATE TABLE Seller_Applications
(
    Application_ID INT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    RealName VARCHAR(50) NOT NULL,
    ID_Number VARCHAR(20) NOT NULL,
    Contact_Phone VARCHAR(20) NOT NULL,
    BankCode VARCHAR(10) NOT NULL,
    BankAccount VARCHAR(30) NOT NULL,
    Seller_Status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    Apply_At DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    IDCard_Front VARCHAR(255) NULL,
    IDCard_Back VARCHAR(255) NULL,

    CONSTRAINT Seller_Applications_PK
        PRIMARY KEY (Application_ID),

    CONSTRAINT Seller_Applications_User_ID_FK
        FOREIGN KEY (User_ID)
        REFERENCES Users(User_ID),
    CONSTRAINT Seller_Applications_Status_CK
CHECK (
    Seller_Status IN (
        'PENDING',
        'APPROVED',
        'REJECTED',
        'CANCELLED'
    )
)
);
CREATE UNIQUE INDEX Seller_Applications_User_Pending_UQ
ON Seller_Applications(User_ID)
WHERE Seller_Status = 'PENDING';


CREATE TABLE Sellers
(
    User_ID INT NOT NULL,
    Full_Name NVARCHAR(50) NOT NULL,
    Store_Name NVARCHAR(50) NOT NULL,
    Store_Description NVARCHAR(100) NULL,
    Store_Status VARCHAR(50) NOT NULL
        DEFAULT 'ACTIVE',
    StoreCreated_At DATETIME2 NOT NULL,
    StoreUpdated_At DATETIME2 NOT NULL,

    CONSTRAINT Sellers_User_ID_PK
        PRIMARY KEY (User_ID),

    CONSTRAINT Sellers_User_ID_FK
        FOREIGN KEY (User_ID)
        REFERENCES Users(User_ID),

    CONSTRAINT Sellers_Store_Status_CK
CHECK (
    Store_Status IN (
        'ACTIVE',
        'SUSPENDED',
        'CLOSED'
    )
)
);

CREATE TABLE Roles
(
    Role_ID INT IDENTITY(1,1) NOT NULL,
    Role_Name NVARCHAR(50) NOT NULL,
    Role_Description NVARCHAR(255) NULL,

    CONSTRAINT Roles_PK
        PRIMARY KEY (Role_ID),

    CONSTRAINT Roles_Role_Name_UQ
        UNIQUE (Role_Name)
);

CREATE TABLE Admin_Users
(
    Admin_ID INT IDENTITY(1,1) NOT NULL,
    Username VARCHAR(50) NOT NULL,
    Full_Name NVARCHAR(50) NOT NULL,
    Role_ID INT NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Phone VARCHAR(20) NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Is_Locked BIT NOT NULL DEFAULT 0,
    LastLogin_At DATETIME2 NULL,

    CONSTRAINT Admin_Users_PK
        PRIMARY KEY (Admin_ID),

    CONSTRAINT Admin_Users_Username_UQ
        UNIQUE (Username),

    CONSTRAINT Admin_Users_Email_UQ
        UNIQUE (Email),

    CONSTRAINT Admin_Users_Role_ID_FK
        FOREIGN KEY (Role_ID)
        REFERENCES Roles(Role_ID)
);

CREATE TABLE Seller_Application_Audit
(
    Audit_ID INT IDENTITY(1,1) NOT NULL,
    Application_ID INT NOT NULL,
    Audit_Status NVARCHAR(20) NOT NULL,
    Admin_ID INT NOT NULL,
    Reviewed_At DATETIME2 NOT NULL,
    Audit_Note NVARCHAR(500) NULL,

    CONSTRAINT Seller_Application_Audit_PK
        PRIMARY KEY (Audit_ID),

    CONSTRAINT Seller_Application_Audit_Application_ID_FK
        FOREIGN KEY (Application_ID)
        REFERENCES Seller_Applications(Application_ID),

    CONSTRAINT Seller_Application_Audit_Admin_ID_FK
        FOREIGN KEY (Admin_ID)
        REFERENCES Admin_Users(Admin_ID)
);

CREATE TABLE Product
(
    Product_ID INT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Product_Name NVARCHAR(50) NOT NULL,
    Location NVARCHAR(10) NULL,
    Product_Type NVARCHAR(15) NULL,
    Description NVARCHAR(500) NULL,
    Published_At DATETIME2 NULL,
    Product_Status VARCHAR(50) NULL,
    Created_At DATETIME2 NOT NULL,
    Updated_At DATETIME2 NULL,

    CONSTRAINT Product_ID_PK
        PRIMARY KEY (Product_ID),

    CONSTRAINT Product_User_ID_FK
    FOREIGN KEY (User_ID)
    REFERENCES Sellers(User_ID)

);

CREATE TABLE Product_Image
(
    Image_ID INT IDENTITY(1,1) NOT NULL,
    Product_ID INT NOT NULL,
    Image_URL NVARCHAR(500) NOT NULL,
    Image_Order INT NOT NULL,
    ImgCreated_At DATETIME2 NOT NULL,

    CONSTRAINT Image_ID_PK
        PRIMARY KEY (Image_ID),

    CONSTRAINT Product_Image_Product_ID_FK
        FOREIGN KEY (Product_ID)
        REFERENCES Product(Product_ID),
    CONSTRAINT Product_Image_Order_CK
    CHECK (Image_Order > 0),
    CONSTRAINT Product_Image_Product_Order_UQ
UNIQUE (Product_ID, Image_Order)

);

CREATE TABLE Product_Specs
(
    Specification_ID INT IDENTITY(1,1) NOT NULL,
    Product_ID INT NOT NULL,
    Specs_Category1 NVARCHAR(50) NOT NULL,
    Specs_Category2 NVARCHAR(50) NULL,
    Specs_Price INT NOT NULL,
    Deposit INT NULL,
    Stock INT NOT NULL,
    Pre_Sale bit NOT null,
    SpecsCreated_At DATETIME2 NOT NULL,
    SpecsUpdated_At DATETIME2 NOT NULL,

    CONSTRAINT Specification_ID_PK
    PRIMARY KEY (Specification_ID),

    CONSTRAINT Product_Specs_Product_ID_FK
        FOREIGN KEY (Product_ID)
        REFERENCES Product(Product_ID),

    CONSTRAINT Product_Specs_UNIQUE
        UNIQUE
    (Product_ID, Specs_Category1, Specs_Category2),

    CONSTRAINT Product_Specs_Price_CK
    CHECK (Specs_Price >= 0),

    CONSTRAINT Product_Specs_Deposit_CK
    CHECK (Deposit IS NULL OR Deposit >= 0),

    CONSTRAINT Product_Specs_Stock_CK
    CHECK (Stock >= 0)

);

CREATE TABLE Orders
(
    Order_ID INT IDENTITY(1,1) NOT NULL,
    Order_No VARCHAR(30) NOT NULL,
    Buyer_ID INT NOT NULL,
    Seller_ID INT NOT NULL,
    Ordered_At DATETIME2 NOT NULL,
    Order_Deposit INT NOT NULL,
    Order_Amount INT NOT NULL,
    Ship_Amount INT NOT NULL,
    Order_Status VARCHAR(30) NOT NULL,
    Receiver_Name NVARCHAR(50) NOT NULL,
    Receiver_Phone VARCHAR(10) NOT NULL,
    Shipping_Address NVARCHAR(200) NOT NULL,
    OrderCreated_At DATETIME2 NOT NULL,
    OrderUpdated_At DATETIME2 NULL,

    CONSTRAINT Order_ID_PK
        PRIMARY KEY (Order_ID),

    CONSTRAINT Orders_No_UNIQUE
        UNIQUE (Order_No),

    CONSTRAINT Orders_Buyer_Seller_CK
        CHECK (Buyer_ID <> Seller_ID),

    CONSTRAINT Orders_Buyer_ID_FK
        FOREIGN KEY (Buyer_ID)
        REFERENCES Users(User_ID),

    CONSTRAINT Orders_Seller_ID_FK
        FOREIGN KEY    (Seller_ID)
        REFERENCES Sellers(User_ID),

    CONSTRAINT Orders_Order_Deposit_CK
    CHECK (Order_Deposit >= 0),

    CONSTRAINT Orders_Order_Amount_CK
    CHECK (Order_Amount >= 0),

    CONSTRAINT Orders_Ship_Amount_CK
    CHECK (Ship_Amount >= 0)

);

CREATE TABLE Payment
(
    Payment_ID INT IDENTITY(1,1) NOT NULL,
    Order_ID INT NOT NULL,
    Payment_Type VARCHAR(20) NOT NULL,
    Amount INT NOT NULL,
    Payment_Method VARCHAR(50) NOT NULL,
    Transaction_No VARCHAR(100) NULL,
    Payment_Status VARCHAR(20) NOT NULL,
    Paid_At DATETIME2 NULL,
    PayCreated_At DATETIME2 NOT NULL,
    PayUpdated_At DATETIME2 NULL,

    CONSTRAINT Payment_ID_PK
        PRIMARY KEY (Payment_ID),

    CONSTRAINT Payment_Order_ID_FK
        FOREIGN KEY (Order_ID)
        REFERENCES Orders(Order_ID),
    CONSTRAINT Payment_Amount_CK
CHECK (Amount >= 0)
);

CREATE TABLE Review
(
    Review_ID INT IDENTITY(1,1) NOT NULL,
    Order_ID INT NOT NULL,
    Reviewer_ID INT NOT NULL,
    Reviewee_ID INT NOT NULL,
    Rating INT NOT NULL,
    Comment NVARCHAR(500) NULL,
    ReviewCreated_At DATETIME2 NOT NULL,
    ReviewUpdated_At DATETIME2 NOT NULL,

    CONSTRAINT Review_ID_PK
        PRIMARY KEY (Review_ID),

    CONSTRAINT Review_Order_ID_FK
        FOREIGN KEY (Order_ID)
        REFERENCES Orders(Order_ID),

    CONSTRAINT Review_Reviewer_ID_FK
        FOREIGN KEY (Reviewer_ID)
        REFERENCES Users(User_ID),

    CONSTRAINT Review_Reviewee_ID_FK
        FOREIGN KEY (Reviewee_ID)
        REFERENCES Sellers(User_ID),
    CONSTRAINT Review_Rating_CK
    CHECK (Rating BETWEEN 1 AND 5),
    CONSTRAINT Review_Order_Reviewer_UQ
UNIQUE (Order_ID, Reviewer_ID)

);

CREATE TABLE OrderHistory
(
    History_ID INT IDENTITY(1,1) NOT NULL,
    Order_No VARCHAR(30) NOT NULL,
    Order_Status VARCHAR(30) NOT NULL,
    Change_Time DATETIME2 NOT NULL,
    Change_Reason NVARCHAR(50) NULL,
    Changed_By_User_ID INT NULL,

    CONSTRAINT OrderHistory_History_ID_PK
        PRIMARY KEY (History_ID),

    CONSTRAINT OrderHistory_Order_No_FK
        FOREIGN KEY (Order_No)
        REFERENCES Orders(Order_No),

    CONSTRAINT OrderHistory_Changed_By_User_ID_FK
        FOREIGN KEY (Changed_By_User_ID)
        REFERENCES Users(User_ID)
);

CREATE INDEX OrderHistory_Changed_By_User_ID_IX
ON OrderHistory (Changed_By_User_ID);

CREATE TABLE OrderItems
(
    OrderItems_ID INT IDENTITY(1,1) NOT NULL,
    Order_ID INT NOT NULL,
    Product_ID INT NOT NULL,
    Product_Name NVARCHAR(100) NOT NULL,
    Product_Spec NVARCHAR(50) NULL,
    Product_Spec2 NVARCHAR(50) NULL,
    Pre_Sale BIT NOT NULL,
    Quantity INT NOT NULL,
    Unit_Price INT NOT NULL,

    CONSTRAINT OrderItems_ID_PK
        PRIMARY KEY (OrderItems_ID),

    CONSTRAINT OrderItems_Order_ID_FK
        FOREIGN KEY (Order_ID)
        REFERENCES Orders(Order_ID),

    CONSTRAINT OrderItems_Product_ID_FK
        FOREIGN KEY (Product_ID)
        REFERENCES Product(Product_ID),

    CONSTRAINT OrderItems_Quantity_CK
    CHECK (Quantity > 0),

    CONSTRAINT OrderItems_Unit_Price_CK
    CHECK (Unit_Price >= 0)
);

CREATE TABLE Logistics
(
    Order_No VARCHAR(30) NOT NULL,
    Ship_Way VARCHAR(10) NOT NULL,
    Ship_Number VARCHAR(30) NULL,
    Ship_Status VARCHAR(50) NOT NULL,
    ShipUpdated_At DATETIME2 NOT NULL,

    CONSTRAINT Logistics_Order_No_PK
        PRIMARY KEY (Order_No),

    CONSTRAINT Logistics_Order_No_FK
        FOREIGN KEY (Order_No)
        REFERENCES Orders(Order_No)
);

CREATE TABLE Money_Reconciliations
(
    Money_ID INT IDENTITY(1,1) NOT NULL,
    Order_ID INT NOT NULL,

    -- 財務金額
    Order_Amount INT NOT NULL,
    Seller_Payout INT NOT NULL,

    Platform_Revenue INT NOT NULL,

    -- 人工調整
    Adjust_Amount INT NOT NULL
        DEFAULT 0,

    Is_Manual BIT NOT NULL
        DEFAULT 0,

    Adjust_Reason NVARCHAR(500) NULL,

    -- 經辦管理員
    Admin_ID INT NULL,

    -- 對帳建立時間
    Created_At DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    -- 撥款資訊
    Remit_Status VARCHAR(30) NOT NULL
        DEFAULT 'PENDING',

    Remit_Result NVARCHAR(500) NULL,
    Remit_Date DATETIME2 NULL,


    -- Primary Key
    CONSTRAINT Money_Reconciliations_Money_ID_PK
        PRIMARY KEY (Money_ID),

    -- 一筆 Order 對應一筆財務對帳
    CONSTRAINT Money_Reconciliations_Order_ID_UQ
        UNIQUE (Order_ID),

    -- Order Foreign Key
    CONSTRAINT Money_Reconciliations_Order_ID_FK
        FOREIGN KEY (Order_ID)
        REFERENCES Orders(Order_ID),

    -- Admin Foreign Key
    CONSTRAINT Money_Reconciliations_Admin_ID_FK
    FOREIGN KEY (Admin_ID)
    REFERENCES Admin_Users(Admin_ID),


    -- 撥款狀態限制
    CONSTRAINT Money_Reconciliations_Remit_Status_CK
        CHECK
    (
            Remit_Status IN
    (
                'PENDING',
                'SUCCESS',
                'FAILED'
            )
        )
);

CREATE TABLE BannedWords
(
    BannedWords_ID INT IDENTITY(1,1) NOT NULL,
    BannedWords NVARCHAR(100) NOT NULL,
    Is_Enabled BIT NOT NULL
        DEFAULT 1,
    Admin_ID INT NOT NULL,
    BanCreated_At DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),

    CONSTRAINT BannedWords_ID_PK
        PRIMARY KEY (BannedWords_ID),

    CONSTRAINT BannedWords_Admin_ID_FK
    FOREIGN KEY (Admin_ID)
    REFERENCES Admin_Users(Admin_ID)
);

CREATE TABLE User_Blacklist
(
    Block_ID INT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Reason_Detail NVARCHAR(500) NULL,
    Block_Status NVARCHAR(20) NOT NULL
        DEFAULT 'BLOCKED',
    Admin_ID INT NOT NULL,
    Blocked_At DATETIME2 NOT NULL
        DEFAULT SYSDATETIME(),
    Unblocked_At DATETIME2 NULL,

    CONSTRAINT User_Blacklist_Block_ID_PK
        PRIMARY KEY (Block_ID),

    CONSTRAINT User_Blacklist_User_ID_FK
        FOREIGN KEY (User_ID)
        REFERENCES Users(User_ID),

    CONSTRAINT User_Blacklist_Admin_ID_FK
        FOREIGN KEY (Admin_ID)
        REFERENCES Admin_Users(Admin_ID),

    CONSTRAINT User_Blacklist_Block_Status_CK
        CHECK (
            Block_Status IN
            (
                'BLOCKED',
                'UNBLOCKED'
            )
        )
);

/* =========================================================
   Minimum demo administrator bootstrap data

   These password hashes came from the supplied administrator hash script.
   They are for local demonstration only and must be replaced before deployment.
   ========================================================= */

INSERT INTO Roles
(
    Role_Name,
    Role_Description
)
VALUES
(N'超級管理員', N'擁有所有系統管理權限'),
(N'客服管理員', N'負責使用者、商品與賣家管理'),
(N'財務管理員', N'負責付款與財務對帳管理');

DECLARE @SuperAdminRoleID INT;
DECLARE @CustomerServiceRoleID INT;
DECLARE @FinanceRoleID INT;

SELECT @SuperAdminRoleID = Role_ID
FROM Roles
WHERE Role_Name = N'超級管理員';

SELECT @CustomerServiceRoleID = Role_ID
FROM Roles
WHERE Role_Name = N'客服管理員';

SELECT @FinanceRoleID = Role_ID
FROM Roles
WHERE Role_Name = N'財務管理員';

INSERT INTO Admin_Users
(
    Username,
    Full_Name,
    Role_ID,
    Email,
    Phone,
    PasswordHash,
    Is_Locked,
    LastLogin_At
)
VALUES
(
    'pokemon_admin',
    N'大木博士',
    @SuperAdminRoleID,
    'oak@picartchu.test',
    NULL,
    'AQAAAAIAAYagAAAAEPs9c+ClsAT2xv0D411TOCyGNQ4pbUdhc1I+yd8PhfN9c8W5YRoEJ9L1nla7trOdLA==',
    0,
    NULL
),
(
    'support_misty',
    N'客服小霞',
    @CustomerServiceRoleID,
    'support@picartchu.test',
    NULL,
    'AQAAAAIAAYagAAAAELVV7ss2VeahGjZnCrbD4soQUv9c3JaGK9SQrrjY/hZJ5o4Iant7NGSWpmLcRCVHFQ==',
    0,
    NULL
),
(
    'finance_joy',
    N'喬伊小姐',
    @FinanceRoleID,
    'finance@picartchu.test',
    NULL,
    'AQAAAAIAAYagAAAAEPvuG46Dj8jBRYsB3iXZ03vDbRFvrNLHmg3SUlBORuoek7kicCb37yJ59H3r791eAw==',
    0,
    NULL
);

    COMMIT TRANSACTION;
    PRINT N'Picartchu 資料庫結構與展示管理員已建立完成。';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
