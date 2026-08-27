using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;

namespace PokemonCard.Controllers;

public class SellerController(PicartchuContext context, IWebHostEnvironment environment, ILogger<SellerController> logger) : Controller
{
    private const int SellerId = 3;

    [HttpGet]
    public async Task<IActionResult> SellerHomepage(DateTime? startDate, DateTime? endDate)
    {
        var today = DateTime.Today;
        var year = today.Year;
        var orders = context.Orders.Where(order => order.SellerId == SellerId);

        if (startDate.HasValue)
            orders = orders.Where(order => order.OrderedAt >= startDate.Value.Date);

        if (endDate.HasValue)
        {
            var nextDay = endDate.Value.Date.AddDays(1);
            orders = orders.Where(order => order.OrderedAt < nextDay);
        }

        var totalOrders = await orders.CountAsync();
        var totalSales = await orders.SumAsync(order => (decimal?)order.OrderAmount) ?? 0;
        var monthlySales = await orders
            .Where(order => order.OrderedAt.Year == year)
            .GroupBy(order => order.OrderedAt.Month)
            .Select(group => new { group.Key, Total = group.Sum(order => (decimal)order.OrderAmount) })
            .ToDictionaryAsync(group => group.Key, group => group.Total);
        var chartData = Enumerable.Range(1, 12)
            .Select(month => monthlySales.GetValueOrDefault(month))
            .ToArray();

        ViewBag.ChartData = chartData;
        ViewBag.AnnualTotalSales = chartData.Sum();
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        var model = new SellerDashboardViewModel
        {
            TodayNewOrders = await orders.CountAsync(order => order.OrderedAt.Date == today),
            TotalSales = totalSales,
            TotalOrders = totalOrders,
            AvgOrderValue = totalOrders == 0 ? 0 : totalSales / totalOrders,
            MonthlyCreditedAmount = await context.MoneyReconciliations
                .Where(item => item.Order.SellerId == SellerId
                    && (item.RemitResult == "撥款成功" || item.RemitResult == "匯款完成")
                    && item.RemitDate.HasValue
                    && item.RemitDate.Value.Year == year
                    && item.RemitDate.Value.Month == today.Month)
                .SumAsync(item => (decimal?)item.SellerPayout) ?? 0,
            PendingShipments = await orders.CountAsync(order => order.OrderStatus == "Processing"),
            TotalRevenueDisplay = chartData.Sum().ToString("N0"),
            GrowthRate = "18.5",
            RecentOrders = await orders
                .OrderByDescending(order => order.OrderedAt)
                .Take(5)
                .Select(order => new RecentOrderViewModel
                {
                    OrderId = order.OrderId,
                    OrderNo = order.OrderNo,
                    BuyerName = order.Buyer == null ? "未知買家" : order.Buyer.DisplayName ?? "未知買家",
                    FirstProductName = order.OrderItems
                        .Select(item => item.Product == null ? null : item.Product.ProductName)
                        .FirstOrDefault() ?? "無商品名稱",
                    OrderAmount = order.OrderAmount,
                    OrderStatus = order.OrderStatus
                })
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult OrderManage() => View();

    [HttpGet]
    public async Task<IActionResult> ProductManage(string? productType, string? status, string? search, int page = 1)
    {
        var products = context.Products.AsNoTracking().Where(product => product.UserId == SellerId);
        if (!string.IsNullOrWhiteSpace(productType) && productType != "全部") products = products.Where(product => product.ProductType == productType);
        if (status is "PUBLISHED" or "DRAFT") products = products.Where(product => product.ProductStatus == status);
        if (!string.IsNullOrWhiteSpace(search)) products = products.Where(product => EF.Functions.Like(product.ProductName, $"%{search.Trim()}%"));

        const int pageSize = 10;
        var totalCount = await products.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)));
        var model = new ProductManageViewModel
        {
            ProductType = productType,
            Status = status,
            Search = search,
            Page = page, TotalCount = totalCount, PageSize = pageSize,
            Items = await products.OrderByDescending(product => product.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(product => new ProductListItem
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ProductType = product.ProductType ?? "未分類",
                ProductStatus = product.ProductStatus ?? "DRAFT",
                IsScheduled = product.ProductStatus == "PUBLISHED" && product.PublishedAt > DateTime.Now,
                ImageUrl = product.ProductImages.OrderBy(image => image.ImageOrder).Select(image => image.ImageUrl).FirstOrDefault(),
                PreSale = product.ProductSpecs.Select(spec => (bool?)spec.PreSale).FirstOrDefault() ?? false,
                Price = product.ProductSpecs.Select(spec => (int?)spec.SpecsPrice).Min() ?? 0,
                Stock = product.ProductSpecs.Select(spec => (int?)spec.Stock).Sum() ?? 0,
                Sales = product.OrderItems.Select(item => (int?)item.Quantity).Sum() ?? 0
            }).ToListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductStatus(int[] productIds, string status)
    {
        if (status is not ("PUBLISHED" or "DRAFT") || productIds.Length == 0) return RedirectToAction(nameof(ProductManage));
        var products = await context.Products.Where(product => product.UserId == SellerId && productIds.Contains(product.ProductId)).ToListAsync();
        foreach (var product in products) { product.ProductStatus = status; product.PublishedAt = status == "PUBLISHED" ? DateTime.Now : null; product.UpdatedAt = DateTime.Now; }
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"已更新 {products.Count} 件商品狀態。";
        return RedirectToAction(nameof(ProductManage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        var product = await context.Products.Include(item => item.ProductImages).Include(item => item.ProductSpecs).SingleOrDefaultAsync(item => item.ProductId == productId && item.UserId == SellerId);
        if (product == null) return NotFound();
        if (await context.OrderItems.AnyAsync(item => item.ProductId == productId)) { TempData["ErrorMessage"] = "已有訂單紀錄的商品不可刪除。"; return RedirectToAction(nameof(ProductManage)); }
        context.ProductImages.RemoveRange(product.ProductImages);
        context.ProductSpecs.RemoveRange(product.ProductSpecs);
        context.Products.Remove(product);
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "商品已刪除。";
        return RedirectToAction(nameof(ProductManage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelectedProducts(int[] productIds)
    {
        var products = await context.Products.Include(product => product.ProductImages).Include(product => product.ProductSpecs).Where(product => product.UserId == SellerId && productIds.Contains(product.ProductId)).ToListAsync();
        var deletable = products.Where(product => !context.OrderItems.Any(item => item.ProductId == product.ProductId)).ToList();
        context.ProductImages.RemoveRange(deletable.SelectMany(product => product.ProductImages));
        context.ProductSpecs.RemoveRange(deletable.SelectMany(product => product.ProductSpecs));
        context.Products.RemoveRange(deletable);
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"已刪除 {deletable.Count} 件商品。";
        return RedirectToAction(nameof(ProductManage));
    }

    [HttpGet]
    public async Task<IActionResult> AddProduct(int? id)
    {
        if (!id.HasValue) return View(new AddProductInput());
        var product = await context.Products.Include(item => item.ProductImages).Include(item => item.ProductSpecs).SingleOrDefaultAsync(item => item.ProductId == id && item.UserId == SellerId);
        if (product == null) return NotFound();
        return View(new AddProductInput { ProductId = product.ProductId, ProductName = product.ProductName, Description = product.Description, Location = product.Location, ProductType = product.ProductType, ProductStatus = product.ProductStatus ?? "DRAFT", SaleType = product.ProductSpecs.FirstOrDefault()?.PreSale == true ? "preorder" : "instock", ExistingImages = product.ProductImages.OrderBy(image => image.ImageOrder).Select(image => image.ImageUrl).ToList(), Specs = product.ProductSpecs.Select(spec => new ProductSpecInput { Category = spec.SpecsCategory2 ?? "", Option = spec.SpecsCategory1, Price = spec.SpecsPrice, Stock = spec.Stock }).ToList() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(AddProductInput input)
    {
        input.ProductId = 0;
        return await SaveProduct(input, isUpdate: false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduct(AddProductInput input)
    {
        if (input.ProductId <= 0)
            return BadRequest("缺少要修改的商品編號。");

        return await SaveProduct(input, isUpdate: true);
    }

    private async Task<IActionResult> SaveProduct(AddProductInput input, bool isUpdate)
    {
        var specs = input.Specs;
        ValidateProduct(input, specs);

        if (!ModelState.IsValid)
        {
            await RestoreExistingImagesAsync(input);
            return View("AddProduct", input);
        }

        var now = DateTime.Now;
        var imageFiles = input.Images?.Take(9).ToList() ?? [];
        var savedFiles = new List<string>();

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var isDraft = input.ProductStatus == "DRAFT";
            var product = isUpdate
                ? await context.Products
                    .Include(item => item.ProductSpecs)
                    .Include(item => item.ProductImages)
                    .SingleOrDefaultAsync(item => item.ProductId == input.ProductId && item.UserId == SellerId)
                : null;
            if (isUpdate && product == null) return NotFound();
            var existingPublishedAt = product?.PublishedAt;
            product ??= new Product { UserId = SellerId, CreatedAt = now };
            product.ProductName = input.ProductName.Trim();
            product.Description = input.Description?.Trim();
            product.Location = input.Location;
            product.ProductType = input.ProductType;
            product.ProductStatus = input.ProductStatus;
            product.PublishedAt = isDraft
                ? null
                : input.ScheduledAt.HasValue
                    ? new DateTime(input.ScheduledAt.Value.Year, input.ScheduledAt.Value.Month, input.ScheduledAt.Value.Day, input.ScheduledAt.Value.Hour, input.ScheduledAt.Value.Minute, 0)
                    : existingPublishedAt ?? now;
            product.UpdatedAt = now;
            if (!isUpdate) context.Products.Add(product);
            await context.SaveChangesAsync();
            var imageOrderStart = product.ProductImages.Select(image => image.ImageOrder).DefaultIfEmpty(0).Max();

            if (isUpdate)
            {
                context.ProductSpecs.RemoveRange(product.ProductSpecs);
                await context.SaveChangesAsync();

                var removedImages = product.ProductImages
                    .Where(image => input.RemoveImageUrls.Contains(image.ImageUrl))
                    .ToList();
                context.ProductImages.RemoveRange(removedImages);
                foreach (var image in removedImages)
                    product.ProductImages.Remove(image);
            }

            foreach (var (file, index) in imageFiles.Select((file, index) => (file, index + 1)))
            {
                var imageUrl = await SaveImageAsync(file, savedFiles);
                context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.ProductId,
                    ImageUrl = imageUrl,
                    ImageOrder = imageOrderStart + index,
                    ImgCreatedAt = now
                });
            }

            context.ProductSpecs.AddRange(specs.Select(spec => new ProductSpec
            {
                ProductId = product.ProductId,
                PreSale = input.SaleType == "preorder",
                SpecsCategory1 = spec.Option.Trim(),
                SpecsCategory2 = string.IsNullOrWhiteSpace(spec.Category) ? null : spec.Category.Trim(),
                SpecsPrice = spec.Price,
                Stock = spec.Stock,
                SpecsCreatedAt = now,
                SpecsUpdatedAt = now
            }));

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = isUpdate ? "商品變更已儲存。" : "商品已新增。";
            return RedirectToAction(nameof(ProductManage));
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            foreach (var path in savedFiles.Where(System.IO.File.Exists))
                System.IO.File.Delete(path);

            logger.LogError(exception, "新增商品失敗");
            var message = "商品儲存失敗，請稍後再試。";
            if (environment.IsDevelopment())
                message += $" 原因：{exception.GetBaseException().Message}";
            ModelState.AddModelError(string.Empty, message);
            await RestoreExistingImagesAsync(input);
            return View("AddProduct", input);
        }
    }

    private async Task RestoreExistingImagesAsync(AddProductInput input)
    {
        if (input.ProductId <= 0) return;

        input.ExistingImages = await context.ProductImages
            .Where(image => image.ProductId == input.ProductId)
            .OrderBy(image => image.ImageOrder)
            .Select(image => image.ImageUrl)
            .ToListAsync();
    }

    private async Task<string> SaveImageAsync(IFormFile file, List<string> savedFiles)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"/images/products/{fileName}";
        var directory = Path.Combine(environment.WebRootPath, "images", "products");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);
        savedFiles.Add(filePath);
        return relativePath;
    }

    private void ValidateProduct(AddProductInput input, List<ProductSpecInput> specs)
    {
        if (string.IsNullOrWhiteSpace(input.ProductName))
            ModelState.AddModelError(nameof(input.ProductName), "請輸入商品名稱。");
        if (input.ProductName?.Length > 50)
            ModelState.AddModelError(nameof(input.ProductName), "商品名稱最多 50 字。");
        if (input.Description?.Length > 500)
            ModelState.AddModelError(nameof(input.Description), "商品描述最多 500 字。");
        if (string.IsNullOrWhiteSpace(input.Location))
            ModelState.AddModelError(nameof(input.Location), "請選擇地區。");
        if (string.IsNullOrWhiteSpace(input.ProductType))
            ModelState.AddModelError(nameof(input.ProductType), "請選擇商品分類。");
        if (input.ProductStatus is not ("PUBLISHED" or "DRAFT"))
            ModelState.AddModelError(nameof(input.ProductStatus), "請選擇商品狀態。");
        if (input.Images?.Count > 9)
            ModelState.AddModelError(nameof(input.Images), "商品照片最多 9 張。");
        if (input.Images?.Any(file => file.Length == 0 || file.Length > 5 * 1024 * 1024 || !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(file.FileName).ToLowerInvariant())) == true)
            ModelState.AddModelError(nameof(input.Images), "照片僅限 JPG、PNG、WEBP，且每張不得超過 5 MB。");
        if (specs.Count == 0)
            ModelState.AddModelError(nameof(input.Specs), "請至少新增一組商品規格。");
        if (specs.Any(spec => string.IsNullOrWhiteSpace(spec.Option) || spec.Price < 0 || spec.Stock < 0))
            ModelState.AddModelError(nameof(input.Specs), "請完整填寫規格選項、價格與庫存。");
        if (specs.GroupBy(spec => $"{(spec.Category ?? string.Empty).Trim()}\u001f{(spec.Option ?? string.Empty).Trim()}", StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            ModelState.AddModelError(nameof(input.Specs), "同一規格類別不可有重複的商品選項。");
    }

    [HttpGet]
    public async Task<IActionResult> MyRevenue(DateTime? startDate, DateTime? endDate, string status = "全部")
    {
        var reconciliations = context.MoneyReconciliations
            .Where(item => item.Order.SellerId == SellerId)
            .AsQueryable();

        if (startDate.HasValue)
            reconciliations = reconciliations.Where(item => item.RemitDate >= startDate.Value.Date);

        if (endDate.HasValue)
        {
            var nextDay = endDate.Value.Date.AddDays(1);
            reconciliations = reconciliations.Where(item => item.RemitDate < nextDay);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "全部")
            reconciliations = reconciliations.Where(item => item.RemitResult == status);

        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentStatus = status;

        var model = await reconciliations
            .Select(item => new RevenueViewModel
            {
                OrderNo = item.Order != null ? item.Order.OrderNo : "-",
                RemitResult = item.RemitResult,
                OrderAmount = item.OrderAmount,
                PlatformRevenue = item.PlatformRevenue,
                AdjustAmount = item.AdjustAmount,
                SellerPayout = item.SellerPayout,
                RemitDate = item.RemitDate
            })
            .ToListAsync();

        return View(model);
    }
}

public class AddProductInput
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? ProductType { get; set; }
    public string SaleType { get; set; } = "preorder";
    public DateTime? ScheduledAt { get; set; }
    public string ProductStatus { get; set; } = "PUBLISHED";
    public List<IFormFile>? Images { get; set; }
    public List<string> ExistingImages { get; set; } = [];
    public List<string> RemoveImageUrls { get; set; } = [];
    public List<ProductSpecInput> Specs { get; set; } = [];
}

public class ProductManageViewModel
{
    public string? ProductType { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public List<ProductListItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public class ProductListItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string ProductStatus { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool PreSale { get; set; }
    public int Price { get; set; }
    public int Stock { get; set; }
    public int Sales { get; set; }
    public bool IsScheduled { get; set; }
}

public class ProductSpecInput
{
    public string? Category { get; set; }
    public string Option { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Stock { get; set; }
}
