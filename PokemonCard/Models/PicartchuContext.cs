using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PokemonCard.Models;

public partial class PicartchuContext : DbContext
{
    public PicartchuContext()
    {
    }

    public PicartchuContext(DbContextOptions<PicartchuContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminUser> AdminUsers { get; set; }

    public virtual DbSet<BannedWord> BannedWords { get; set; }

    public virtual DbSet<Logistic> Logistics { get; set; }

    public virtual DbSet<MoneyReconciliation> MoneyReconciliations { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderHistory> OrderHistories { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductSpec> ProductSpecs { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Seller> Sellers { get; set; }

    public virtual DbSet<SellerApplication> SellerApplications { get; set; }

    public virtual DbSet<SellerApplicationAudit> SellerApplicationAudits { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserBlacklist> UserBlacklists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=Picartchu;Integrated Security=True;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Chinese_Taiwan_Stroke_90_CI_AI");

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("Admin_Users_PK");

            entity.ToTable("Admin_Users", tb => tb.HasComment("管理員"));

            entity.HasIndex(e => e.Email, "Admin_Users_Email_UQ").IsUnique();

            entity.HasIndex(e => e.Username, "Admin_Users_Username_UQ").IsUnique();

            entity.Property(e => e.AdminId)
                .HasComment("管理員編號")
                .HasColumnName("Admin_ID");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("電子郵件");
            entity.Property(e => e.FullName)
                .HasMaxLength(50)
                .HasComment("姓名")
                .HasColumnName("Full_Name");
            entity.Property(e => e.IsLocked)
                .HasComment("是否鎖定")
                .HasColumnName("Is_Locked");
            entity.Property(e => e.LastLoginAt)
                .HasComment("最後登入時間")
                .HasColumnName("LastLogin_At");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("密碼雜湊值");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("電話");
            entity.Property(e => e.RoleId)
                .HasComment("角色編號")
                .HasColumnName("Role_ID");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("管理員帳號");

            entity.HasOne(d => d.Role).WithMany(p => p.AdminUsers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Admin_Users_Role_ID_FK");
        });

        modelBuilder.Entity<BannedWord>(entity =>
        {
            entity.HasKey(e => e.BannedWordsId).HasName("BannedWords_ID_PK");

            entity.ToTable(tb => tb.HasComment("禁用詞"));

            entity.Property(e => e.BannedWordsId)
                .HasComment("禁用詞編號")
                .HasColumnName("BannedWords_ID");
            entity.Property(e => e.AdminId)
                .HasComment("管理員編號")
                .HasColumnName("Admin_ID");
            entity.Property(e => e.BanCreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("禁用詞建立時間")
                .HasColumnName("BanCreated_At");
            entity.Property(e => e.BannedWords)
                .HasMaxLength(100)
                .HasComment("禁用詞");
            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(true)
                .HasComment("是否啟用")
                .HasColumnName("Is_Enabled");

            entity.HasOne(d => d.Admin).WithMany(p => p.BannedWords)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BannedWords_Admin_ID_FK");
        });

        modelBuilder.Entity<Logistic>(entity =>
        {
            entity.HasKey(e => e.OrderNo).HasName("Logistics_Order_No_PK");

            entity.ToTable(tb => tb.HasComment("物流"));

            entity.Property(e => e.OrderNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasComment("訂單號碼")
                .HasColumnName("Order_No");
            entity.Property(e => e.ShipNumber)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasComment("物流單號")
                .HasColumnName("Ship_Number");
            entity.Property(e => e.ShipStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("物流狀態")
                .HasColumnName("Ship_Status");
            entity.Property(e => e.ShipUpdatedAt)
                .HasComment("物流更新時間")
                .HasColumnName("ShipUpdated_At");
            entity.Property(e => e.ShipWay)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("配送方式")
                .HasColumnName("Ship_Way");

            entity.HasOne(d => d.OrderNoNavigation).WithOne(p => p.Logistic)
                .HasPrincipalKey<Order>(p => p.OrderNo)
                .HasForeignKey<Logistic>(d => d.OrderNo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Logistics_Order_No_FK");
        });

        modelBuilder.Entity<MoneyReconciliation>(entity =>
        {
            entity.HasKey(e => e.MoneyId).HasName("Money_Reconciliations_Money_ID_PK");

            entity.ToTable("Money_Reconciliations", tb => tb.HasComment("金流對帳"));

            entity.HasIndex(e => e.OrderId, "Money_Reconciliations_Order_ID_UQ").IsUnique();

            entity.Property(e => e.MoneyId)
                .HasComment("對帳編號")
                .HasColumnName("Money_ID");
            entity.Property(e => e.AdjustAmount)
                .HasComment("人工調整金額")
                .HasColumnName("Adjust_Amount");
            entity.Property(e => e.AdjustReason)
                .HasMaxLength(500)
                .HasComment("調整原因")
                .HasColumnName("Adjust_Reason");
            entity.Property(e => e.AdminId)
                .HasComment("管理員編號")
                .HasColumnName("Admin_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("對帳建立時間")
                .HasColumnName("Created_At");
            entity.Property(e => e.IsManual)
                .HasComment("是否人工調整")
                .HasColumnName("Is_Manual");
            entity.Property(e => e.OrderAmount)
                .HasComment("訂單金額")
                .HasColumnName("Order_Amount");
            entity.Property(e => e.OrderId)
                .HasComment("訂單編號")
                .HasColumnName("Order_ID");
            entity.Property(e => e.PlatformRevenue)
                .HasComment("平台收入")
                .HasColumnName("Platform_Revenue");
            entity.Property(e => e.RemitDate)
                .HasComment("撥款日期")
                .HasColumnName("Remit_Date");
            entity.Property(e => e.RemitResult)
                .HasMaxLength(500)
                .HasComment("撥款結果")
                .HasColumnName("Remit_Result");
            entity.Property(e => e.RemitStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("PENDING")
                .HasComment("撥款狀態")
                .HasColumnName("Remit_Status");
            entity.Property(e => e.SellerPayout)
                .HasComment("賣家撥款金額")
                .HasColumnName("Seller_Payout");

            entity.HasOne(d => d.Admin).WithMany(p => p.MoneyReconciliations)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("Money_Reconciliations_Admin_ID_FK");

            entity.HasOne(d => d.Order).WithOne(p => p.MoneyReconciliation)
                .HasForeignKey<MoneyReconciliation>(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Money_Reconciliations_Order_ID_FK");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("Order_ID_PK");

            entity.ToTable(tb => tb.HasComment("訂單"));

            entity.HasIndex(e => e.OrderNo, "Orders_No_UNIQUE").IsUnique();

            entity.Property(e => e.OrderId)
                .HasComment("訂單編號")
                .HasColumnName("Order_ID");
            entity.Property(e => e.BuyerId)
                .HasComment("買家編號")
                .HasColumnName("Buyer_ID");
            entity.Property(e => e.OrderAmount)
                .HasComment("訂單金額")
                .HasColumnName("Order_Amount");
            entity.Property(e => e.OrderCreatedAt)
                .HasComment("訂單建立時間")
                .HasColumnName("OrderCreated_At");
            entity.Property(e => e.OrderDeposit)
                .HasComment("訂金")
                .HasColumnName("Order_Deposit");
            entity.Property(e => e.OrderNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasComment("訂單號碼")
                .HasColumnName("Order_No");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasComment("訂單狀態")
                .HasColumnName("Order_Status");
            entity.Property(e => e.OrderUpdatedAt)
                .HasComment("訂單更新時間")
                .HasColumnName("OrderUpdated_At");
            entity.Property(e => e.OrderedAt)
                .HasComment("下單時間")
                .HasColumnName("Ordered_At");
            entity.Property(e => e.ReceiverName)
                .HasMaxLength(50)
                .HasComment("收件人姓名")
                .HasColumnName("Receiver_Name");
            entity.Property(e => e.ReceiverPhone)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("收件人電話")
                .HasColumnName("Receiver_Phone");
            entity.Property(e => e.SellerId)
                .HasComment("賣家編號")
                .HasColumnName("Seller_ID");
            entity.Property(e => e.ShipAmount)
                .HasComment("運費")
                .HasColumnName("Ship_Amount");
            entity.Property(e => e.ShippingAddress)
                .HasMaxLength(200)
                .HasComment("配送地址")
                .HasColumnName("Shipping_Address");

            entity.HasOne(d => d.Buyer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.BuyerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Orders_Buyer_ID_FK");

            entity.HasOne(d => d.Seller).WithMany(p => p.Orders)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Orders_Seller_ID_FK");
        });

        modelBuilder.Entity<OrderHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("OrderHistory_History_ID_PK");

            entity.ToTable("OrderHistory", tb => tb.HasComment("訂單狀態歷程"));

            entity.Property(e => e.HistoryId)
                .HasComment("歷程編號")
                .HasColumnName("History_ID");
            entity.Property(e => e.ChangeTime)
                .HasComment("狀態變更時間")
                .HasColumnName("Change_Time");
            entity.Property(e => e.OrderNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasComment("訂單號碼")
                .HasColumnName("Order_No");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasComment("訂單狀態")
                .HasColumnName("Order_Status");

            entity.HasOne(d => d.OrderNoNavigation).WithMany(p => p.OrderHistories)
                .HasPrincipalKey(p => p.OrderNo)
                .HasForeignKey(d => d.OrderNo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("OrderHistory_Order_No_FK");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemsId).HasName("OrderItems_ID_PK");

            entity.ToTable(tb => tb.HasComment("訂單明細"));

            entity.Property(e => e.OrderItemsId)
                .HasComment("訂單明細編號")
                .HasColumnName("OrderItems_ID");
            entity.Property(e => e.OrderId)
                .HasComment("訂單編號")
                .HasColumnName("Order_ID");
            entity.Property(e => e.PreSale)
                .HasComment("是否預售")
                .HasColumnName("Pre_Sale");
            entity.Property(e => e.ProductId)
                .HasComment("商品編號")
                .HasColumnName("Product_ID");
            entity.Property(e => e.ProductName)
                .HasMaxLength(100)
                .HasComment("商品名稱")
                .HasColumnName("Product_Name");
            entity.Property(e => e.ProductSpec)
                .HasMaxLength(50)
                .HasComment("商品規格一")
                .HasColumnName("Product_Spec");
            entity.Property(e => e.ProductSpec2)
                .HasMaxLength(50)
                .HasComment("商品規格二")
                .HasColumnName("Product_Spec2");
            entity.Property(e => e.Quantity).HasComment("購買數量");
            entity.Property(e => e.UnitPrice)
                .HasComment("單價")
                .HasColumnName("Unit_Price");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("OrderItems_Order_ID_FK");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("OrderItems_Product_ID_FK");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("Payment_ID_PK");

            entity.ToTable("Payment", tb => tb.HasComment("付款紀錄"));

            entity.Property(e => e.PaymentId)
                .HasComment("付款編號")
                .HasColumnName("Payment_ID");
            entity.Property(e => e.Amount).HasComment("付款金額");
            entity.Property(e => e.OrderId)
                .HasComment("訂單編號")
                .HasColumnName("Order_ID");
            entity.Property(e => e.PaidAt)
                .HasComment("付款時間")
                .HasColumnName("Paid_At");
            entity.Property(e => e.PayCreatedAt)
                .HasComment("付款紀錄建立時間")
                .HasColumnName("PayCreated_At");
            entity.Property(e => e.PayUpdatedAt)
                .HasComment("付款紀錄更新時間")
                .HasColumnName("PayUpdated_At");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("付款方式")
                .HasColumnName("Payment_Method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("付款狀態")
                .HasColumnName("Payment_Status");
            entity.Property(e => e.PaymentType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("付款類型")
                .HasColumnName("Payment_Type");
            entity.Property(e => e.TransactionNo)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("交易編號")
                .HasColumnName("Transaction_No");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Payment_Order_ID_FK");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("Product_ID_PK");

            entity.ToTable("Product", tb => tb.HasComment("商品"));

            entity.Property(e => e.ProductId)
                .HasComment("商品編號")
                .HasColumnName("Product_ID");
            entity.Property(e => e.CreatedAt)
                .HasComment("建立時間")
                .HasColumnName("Created_At");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasComment("商品描述");
            entity.Property(e => e.Location)
                .HasMaxLength(10)
                .HasComment("商品所在地");
            entity.Property(e => e.ProductName)
                .HasMaxLength(50)
                .HasComment("商品名稱")
                .HasColumnName("Product_Name");
            entity.Property(e => e.ProductStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("商品狀態")
                .HasColumnName("Product_Status");
            entity.Property(e => e.ProductType)
                .HasMaxLength(15)
                .HasComment("商品類型")
                .HasColumnName("Product_Type");
            entity.Property(e => e.PublishedAt)
                .HasComment("發布時間")
                .HasColumnName("Published_At");
            entity.Property(e => e.UpdatedAt)
                .HasComment("更新時間")
                .HasColumnName("Updated_At");
            entity.Property(e => e.UserId)
                .HasComment("賣家編號")
                .HasColumnName("User_ID");

            entity.HasOne(d => d.Seller).WithMany(p => p.Products)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Product_User_ID_FK");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("Image_ID_PK");

            entity.ToTable("Product_Image", tb => tb.HasComment("商品圖片"));

            entity.HasIndex(e => new { e.ProductId, e.ImageOrder }, "Product_Image_Product_Order_UQ").IsUnique();

            entity.Property(e => e.ImageId)
                .HasComment("圖片編號")
                .HasColumnName("Image_ID");
            entity.Property(e => e.ImageOrder)
                .HasComment("圖片排序")
                .HasColumnName("Image_Order");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasComment("圖片網址")
                .HasColumnName("Image_URL");
            entity.Property(e => e.ImgCreatedAt)
                .HasComment("圖片建立時間")
                .HasColumnName("ImgCreated_At");
            entity.Property(e => e.ProductId)
                .HasComment("商品編號")
                .HasColumnName("Product_ID");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Product_Image_Product_ID_FK");
        });

        modelBuilder.Entity<ProductSpec>(entity =>
        {
            entity.HasKey(e => e.SpecificationId).HasName("Specification_ID_PK");

            entity.ToTable("Product_Specs", tb => tb.HasComment("商品規格"));

            entity.HasIndex(e => new { e.ProductId, e.SpecsCategory1, e.SpecsCategory2 }, "Product_Specs_UNIQUE").IsUnique();

            entity.Property(e => e.SpecificationId)
                .HasComment("規格編號")
                .HasColumnName("Specification_ID");
            entity.Property(e => e.Deposit)
                .HasComment("訂金金額")
                .HasColumnName("Deposit");
            entity.Property(e => e.PreSale)
                .HasComment("是否預售")
                .HasColumnName("Pre_Sale");
            entity.Property(e => e.ProductId)
                .HasComment("商品編號")
                .HasColumnName("Product_ID");
            entity.Property(e => e.SpecsCategory1)
                .HasMaxLength(50)
                .HasComment("規格類別一")
                .HasColumnName("Specs_Category1");
            entity.Property(e => e.SpecsCategory2)
                .HasMaxLength(50)
                .HasComment("規格類別二")
                .HasColumnName("Specs_Category2");
            entity.Property(e => e.SpecsCreatedAt)
                .HasComment("規格建立時間")
                .HasColumnName("SpecsCreated_At");
            entity.Property(e => e.SpecsPrice)
                .HasComment("規格價格")
                .HasColumnName("Specs_Price");
            entity.Property(e => e.SpecsUpdatedAt)
                .HasComment("規格更新時間")
                .HasColumnName("SpecsUpdated_At");
            entity.Property(e => e.Stock).HasComment("庫存數量");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSpecs)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Product_Specs_Product_ID_FK");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("Review_ID_PK");

            entity.ToTable("Review", tb => tb.HasComment("訂單評價"));

            entity.HasIndex(e => new { e.OrderId, e.ReviewerId }, "Review_Order_Reviewer_UQ").IsUnique();

            entity.Property(e => e.ReviewId)
                .HasComment("評價編號")
                .HasColumnName("Review_ID");
            entity.Property(e => e.Comment)
                .HasMaxLength(500)
                .HasComment("評價內容");
            entity.Property(e => e.OrderId)
                .HasComment("訂單編號")
                .HasColumnName("Order_ID");
            entity.Property(e => e.Rating).HasComment("評分");
            entity.Property(e => e.ReviewCreatedAt)
                .HasComment("評價建立時間")
                .HasColumnName("ReviewCreated_At");
            entity.Property(e => e.ReviewUpdatedAt)
                .HasComment("評價更新時間")
                .HasColumnName("ReviewUpdated_At");
            entity.Property(e => e.RevieweeId)
                .HasComment("被評價者編號")
                .HasColumnName("Reviewee_ID");
            entity.Property(e => e.ReviewerId)
                .HasComment("評價者編號")
                .HasColumnName("Reviewer_ID");

            entity.HasOne(d => d.Order).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Review_Order_ID_FK");

            entity.HasOne(d => d.Reviewee).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.RevieweeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Review_Reviewee_ID_FK");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ReviewerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Review_Reviewer_ID_FK");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("Roles_PK");

            entity.ToTable(tb => tb.HasComment("管理員角色"));

            entity.HasIndex(e => e.RoleName, "Roles_Role_Name_UQ").IsUnique();

            entity.Property(e => e.RoleId)
                .HasComment("角色編號")
                .HasColumnName("Role_ID");
            entity.Property(e => e.RoleDescription)
                .HasMaxLength(255)
                .HasComment("角色說明")
                .HasColumnName("Role_Description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasComment("角色名稱")
                .HasColumnName("Role_Name");
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("Sellers_User_ID_PK");

            entity.ToTable(tb => tb.HasComment("賣家"));

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasComment("使用者編號")
                .HasColumnName("User_ID");
            entity.Property(e => e.FullName)
                .HasMaxLength(50)
                .HasComment("姓名")
                .HasColumnName("Full_Name");
            entity.Property(e => e.StoreCreatedAt)
                .HasComment("商店建立時間")
                .HasColumnName("StoreCreated_At");
            entity.Property(e => e.StoreDescription)
                .HasMaxLength(100)
                .HasComment("商店介紹")
                .HasColumnName("Store_Description");
            entity.Property(e => e.StoreName)
                .HasMaxLength(50)
                .HasComment("商店名稱")
                .HasColumnName("Store_Name");
            entity.Property(e => e.StoreStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE")
                .HasComment("商店狀態")
                .HasColumnName("Store_Status");
            entity.Property(e => e.StoreUpdatedAt)
                .HasComment("商店更新時間")
                .HasColumnName("StoreUpdated_At");

            entity.HasOne(d => d.User).WithOne(p => p.Seller)
                .HasForeignKey<Seller>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Sellers_User_ID_FK");
        });

        modelBuilder.Entity<SellerApplication>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("Seller_Applications_PK");

            entity.ToTable("Seller_Applications", tb => tb.HasComment("賣家申請"));

            entity.HasIndex(e => e.UserId, "Seller_Applications_User_Pending_UQ")
                .IsUnique()
                .HasFilter("([Seller_Status]='PENDING')");

            entity.Property(e => e.ApplicationId)
                .HasComment("申請編號")
                .HasColumnName("Application_ID");
            entity.Property(e => e.ApplyAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("申請時間")
                .HasColumnName("Apply_At");
            entity.Property(e => e.BankAccount)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasComment("銀行帳號");
            entity.Property(e => e.BankCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("銀行代碼");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("聯絡電話")
                .HasColumnName("Contact_Phone");
            entity.Property(e => e.IdNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("身分證字號")
                .HasColumnName("ID_Number");
            entity.Property(e => e.IdcardBack)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("身分證反面照片")
                .HasColumnName("IDCard_Back");
            entity.Property(e => e.IdcardFront)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("身分證正面照片")
                .HasColumnName("IDCard_Front");
            entity.Property(e => e.RealName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("真實姓名");
            entity.Property(e => e.SellerStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING")
                .HasComment("賣家申請狀態")
                .HasColumnName("Seller_Status");
            entity.Property(e => e.UserId)
                .HasComment("使用者編號")
                .HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithOne(p => p.SellerApplication)
                .HasForeignKey<SellerApplication>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Seller_Applications_User_ID_FK");
        });

        modelBuilder.Entity<SellerApplicationAudit>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("Seller_Application_Audit_PK");

            entity.ToTable("Seller_Application_Audit", tb => tb.HasComment("賣家申請審核紀錄"));

            entity.Property(e => e.AuditId)
                .HasComment("審核紀錄編號")
                .HasColumnName("Audit_ID");
            entity.Property(e => e.AdminId)
                .HasComment("管理員編號")
                .HasColumnName("Admin_ID");
            entity.Property(e => e.ApplicationId)
                .HasComment("申請編號")
                .HasColumnName("Application_ID");
            entity.Property(e => e.AuditNote)
                .HasMaxLength(500)
                .HasComment("審核備註")
                .HasColumnName("Audit_Note");
            entity.Property(e => e.AuditStatus)
                .HasMaxLength(20)
                .HasComment("審核狀態")
                .HasColumnName("Audit_Status");
            entity.Property(e => e.ReviewedAt)
                .HasComment("審核時間")
                .HasColumnName("Reviewed_At");

            entity.HasOne(d => d.Admin).WithMany(p => p.SellerApplicationAudits)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Seller_Application_Audit_Admin_ID_FK");

            entity.HasOne(d => d.Application).WithMany(p => p.SellerApplicationAudits)
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Seller_Application_Audit_Application_ID_FK");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("Users_User_ID_PK");

            entity.ToTable(tb => tb.HasComment("使用者"));

            entity.HasIndex(e => e.Email, "Users_Email_UQ").IsUnique();

            entity.HasIndex(e => new { e.Provider, e.ProviderId }, "Users_Provider_ID_UQ")
                .IsUnique()
                .HasFilter("([Provider_ID] IS NOT NULL)");

            entity.HasIndex(e => e.Username, "Users_Username_UQ")
                .IsUnique()
                .HasFilter("([Username] IS NOT NULL)");

            entity.Property(e => e.UserId)
                .HasComment("使用者編號")
                .HasColumnName("User_ID");
            entity.Property(e => e.Avatar)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("頭像");
            entity.Property(e => e.Birthday).HasComment("生日");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(10)
                .HasComment("顯示名稱")
                .HasColumnName("Display_Name");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("電子郵件");
            entity.Property(e => e.LastLoginAt)
                .HasComment("最後登入時間")
                .HasColumnName("Last_Login_At");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("密碼雜湊值");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("電話");
            entity.Property(e => e.Provider)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("LOCAL")
                .HasComment("登入提供者");
            entity.Property(e => e.ProviderId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("第三方登入識別碼")
                .HasColumnName("Provider_ID");
            entity.Property(e => e.SellerVerificationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("NONE")
                .HasComment("賣家驗證狀態")
                .HasColumnName("Seller_Verification_Status");
            entity.Property(e => e.UserCreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("建立時間")
                .HasColumnName("UserCreated_At");
            entity.Property(e => e.UserStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE")
                .HasComment("使用者狀態")
                .HasColumnName("User_Status");
            entity.Property(e => e.UserUpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("更新時間")
                .HasColumnName("UserUpdated_At");
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("使用者名稱");
        });

        modelBuilder.Entity<UserBlacklist>(entity =>
        {
            entity.HasKey(e => e.BlockId).HasName("User_Blacklist_Block_ID_PK");

            entity.ToTable("User_Blacklist", tb => tb.HasComment("使用者黑名單"));

            entity.Property(e => e.BlockId)
                .HasComment("封鎖紀錄編號")
                .HasColumnName("Block_ID");
            entity.Property(e => e.AdminId)
                .HasComment("管理員編號")
                .HasColumnName("Admin_ID");
            entity.Property(e => e.BlockStatus)
                .HasMaxLength(20)
                .HasDefaultValue("BLOCKED")
                .HasComment("封鎖狀態")
                .HasColumnName("Block_Status");
            entity.Property(e => e.BlockedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("封鎖時間")
                .HasColumnName("Blocked_At");
            entity.Property(e => e.ReasonDetail)
                .HasMaxLength(500)
                .HasComment("封鎖原因")
                .HasColumnName("Reason_Detail");
            entity.Property(e => e.UnblockedAt)
                .HasComment("解除封鎖時間")
                .HasColumnName("Unblocked_At");
            entity.Property(e => e.UserId)
                .HasComment("使用者編號")
                .HasColumnName("User_ID");

            entity.HasOne(d => d.Admin).WithMany(p => p.UserBlacklists)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("User_Blacklist_Admin_ID_FK");

            entity.HasOne(d => d.User).WithMany(p => p.UserBlacklists)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("User_Blacklist_User_ID_FK");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
