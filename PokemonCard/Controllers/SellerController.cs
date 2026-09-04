using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;
using System.Security.Claims;
using PokemonCard.ViewModels;

namespace PokemonCard.Controllers;

public class SellerController(PicartchuContext context, IWebHostEnvironment environment, ILogger<SellerController> logger, BannedWordReviewService bannedWordReviewService) : Controller
{
    public override void OnActionExecuting(ActionExecutingContext actionContext)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewData["IsSeller"] = int.TryParse(userIdText, out var userId)
            && context.Sellers.AsNoTracking().Any(seller => seller.UserId == userId);
        base.OnActionExecuting(actionContext);
    }

    private async Task<int?> GetCurrentSellerIdAsync()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdText, out var userId)) return null;

        return await context.Sellers
            .AsNoTracking()
            .Where(seller => seller.UserId == userId)
            .Select(seller => (int?)seller.UserId)
            .FirstOrDefaultAsync();
    }

    private int? GetCurrentUserId()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdText, out var userId) ? userId : null;
    }

    private static bool IsOrderActionable(string status) => status is not ("SHIPPED" or "COMPLETED" or "CANCELLED");

    private static string GetRemitStatusText(string? remitResult, string? remitStatus)
    {
        if (!string.IsNullOrWhiteSpace(remitResult))
        {
            return remitResult switch
            {
                "已撥款" or "撥款完成" or "撥款成功" or "匯款完成" or "SUCCESS" => "已撥款",
                "撥款失敗" or "撥款取消" or "CANCELLED" => "撥款取消",
                _ => remitResult
            };
        }

        return remitStatus switch
        {
            "PENDING" => "待撥款",
            "COMPLETED" or "SUCCESS" => "已撥款",
            "CANCELLED" or "FAILED" => "撥款取消",
            _ => "待撥款"
        };
    }

    [HttpGet]
    public async Task<IActionResult> SellerHomepage(DateTime? startDate, DateTime? endDate)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        var today = DateTime.Today;
        var year = today.Year;
        var orders = context.Orders.Where(order => order.SellerId == sellerId.Value);

        if (startDate.HasValue)
            orders = orders.Where(order => order.OrderedAt >= startDate.Value.Date);

        if (endDate.HasValue)
        {
            var nextDay = endDate.Value.Date.AddDays(1);
            orders = orders.Where(order => order.OrderedAt < nextDay);
        }

        var salesOrders = orders.Where(order => order.OrderStatus.ToUpper() != "CANCELLED");

        var totalOrders = await orders.CountAsync();
        var totalSales = await salesOrders.SumAsync(order => (decimal?)order.OrderAmount) ?? 0;
        var monthlySales = await salesOrders
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
                .Where(item => item.Order.SellerId == sellerId.Value
                    && (item.RemitStatus == "COMPLETED" || item.RemitStatus == "SUCCESS")
                    && item.RemitDate.HasValue
                    && item.RemitDate.Value.Year == year
                    && item.RemitDate.Value.Month == today.Month)
                .SumAsync(item => (decimal?)item.SellerPayout) ?? 0,
            PendingShipments = await orders.CountAsync(order => order.OrderStatus.ToUpper() == "PROCESSING"),
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

    [HttpGet]
    public async Task<IActionResult> OrderManage(string? search, string? status, int page = 1)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");

        var orders = context.Orders
            .AsNoTracking()
            .Where(order => order.SellerId == sellerId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            var pattern = $"%{keyword}%";
            orders = orders.Where(order =>
                EF.Functions.Like(order.OrderNo, pattern)
                || EF.Functions.Like(order.Buyer.Username ?? string.Empty, pattern)
                || EF.Functions.Like(order.Buyer.Phone ?? string.Empty, pattern)
                || EF.Functions.Like(order.Buyer.DisplayName ?? string.Empty, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            orders = status == "PROCESSING"
                ? orders.Where(order => order.OrderStatus == "PROCESSING" || order.OrderStatus == "Processing")
                : orders.Where(order => order.OrderStatus == status);
        }

        const int pageSize = 10;
        var totalCount = await orders.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)));

        var model = new SellerOrderManageViewModel
        {
            Search = search,
            Status = status,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = await orders
                .OrderByDescending(order => order.OrderedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(order => new SellerOrderListItem
                {
                    OrderId = order.OrderId,
                    OrderNo = order.OrderNo,
                    Username = order.Buyer.Username ?? "-",
                    OrderAmount = order.OrderAmount,
                    OrderStatus = order.OrderStatus,
                    OrderedAt = order.OrderedAt
                })
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ToBuyList()
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");

        var paidItems = await context.OrderItems
            .AsNoTracking()
            .Where(item => item.Order.SellerId == sellerId.Value
                && item.Order.OrderStatus.ToUpper() == "PAID")
            .Select(item => new
            {
                item.OrderItemsId,
                item.OrderId,
                item.Order.OrderNo,
                item.Order.OrderedAt,
                item.ProductId,
                item.ProductName,
                item.ProductSpec,
                item.ProductSpec2,
                item.Quantity,
                ImageUrl = item.Product.ProductImages
                    .OrderBy(image => image.ImageOrder)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var model = new ToBuyListViewModel
        {
            Groups = paidItems
                .GroupBy(item => new
                {
                    item.ProductId,
                    item.ProductName,
                    item.ProductSpec,
                    item.ProductSpec2,
                    item.ImageUrl
                })
                .Select(group => new ToBuyListGroup
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.ProductName,
                    ProductSpec = group.Key.ProductSpec,
                    ProductSpec2 = group.Key.ProductSpec2,
                    ImageUrl = group.Key.ImageUrl,
                    TotalQuantity = group.Sum(item => item.Quantity),
                    OrderItemIds = group.Select(item => item.OrderItemsId).ToList(),
                    Items = group
                        .OrderBy(item => item.OrderedAt)
                        .ThenBy(item => item.OrderItemsId)
                        .Select(item => new ToBuyPurchaseItem
                        {
                            OrderItemId = item.OrderItemsId,
                            OrderId = item.OrderId,
                            Quantity = item.Quantity
                        })
                        .ToList(),
                    OrderIds = group.Select(item => item.OrderId).Distinct().ToList(),
                    OrderNumbers = group.Select(item => item.OrderNo).Distinct().OrderBy(number => number).ToList()
                })
                .OrderBy(group => group.ProductName)
                .ThenBy(group => group.ProductSpec)
                .ThenBy(group => group.ProductSpec2)
                .ToList()
        };

        model.Orders = paidItems
            .GroupBy(item => new { item.OrderId, item.OrderNo, item.OrderedAt })
            .Select(group => new ToBuyOrderProgress
            {
                OrderId = group.Key.OrderId,
                OrderNo = group.Key.OrderNo,
                OrderedAt = group.Key.OrderedAt,
                Items = group.Select(item => new ToBuyPurchaseItem
                {
                    OrderItemId = item.OrderItemsId,
                    OrderId = item.OrderId,
                    Quantity = item.Quantity
                }).ToList()
            })
            .OrderBy(order => order.OrderedAt)
            .ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletePurchasing(List<int>? selectedOrderIds, string? returnTo = null)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        var redirectAction = returnTo == nameof(ToBuyList) ? nameof(ToBuyList) : nameof(OrderManage);
        if (selectedOrderIds is not { Count: > 0 })
        {
            TempData["ErrorMessage"] = "請先選擇訂單。";
            return RedirectToAction(redirectAction);
        }

        var orders = await context.Orders
            .Where(order => order.SellerId == sellerId.Value && selectedOrderIds.Contains(order.OrderId))
            .ToListAsync();
        if (orders.Count != selectedOrderIds.Distinct().Count()
            || orders.Any(order => order.OrderStatus.ToUpper() != "PAID"))
        {
            TempData["ErrorMessage"] = "只有已付款的訂單可以設為採購完成。";
            return RedirectToAction(redirectAction);
        }

        var now = DateTime.Now;
        foreach (var order in orders)
        {
            order.OrderStatus = "PROCESSING";
            order.OrderUpdatedAt = now;
            context.OrderHistories.Add(new OrderHistory
            {
                OrderNo = order.OrderNo,
                OrderStatus = "PROCESSING",
                ChangeTime = now,
                ChangeReason = "賣家已完成採購",
                ChangedByUserId = sellerId.Value
            });
        }

        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"已將 {orders.Count} 筆訂單設為採購完成，等待出貨。";
        return RedirectToAction(redirectAction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int orderId, string? cancelReason)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");

        var reason = cancelReason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "請填寫取消原因。";
            return RedirectToAction(nameof(OrderManage));
        }
        if (reason.Length > 50)
        {
            TempData["ErrorMessage"] = "取消原因最多 50 個字。";
            return RedirectToAction(nameof(OrderManage));
        }

        var order = await context.Orders
            .SingleOrDefaultAsync(item => item.OrderId == orderId && item.SellerId == sellerId.Value);
        if (order is null) return NotFound();
        var normalizedStatus = order.OrderStatus.Trim().ToUpperInvariant();
        if (normalizedStatus is not ("PENDING" or "PAID"))
        {
            TempData["ErrorMessage"] = "只有待付款或尚未完成採購的已付款訂單可以取消。";
            return RedirectToAction(nameof(OrderManage));
        }

        var now = DateTime.Now;
        order.OrderStatus = "CANCELLED";
        order.OrderUpdatedAt = now;
        context.OrderHistories.Add(new OrderHistory
        {
            OrderNo = order.OrderNo,
            OrderStatus = "CANCELLED",
            ChangeTime = now,
            ChangeReason = reason,
            ChangedByUserId = sellerId.Value
        });

        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"訂單 {order.OrderNo} 已取消。";
        return RedirectToAction(nameof(OrderManage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ShipOrders(List<int>? selectedOrderIds)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        if (selectedOrderIds is not { Count: > 0 })
        {
            TempData["ErrorMessage"] = "請至少勾選一筆訂單。";
            return RedirectToAction(nameof(OrderManage));
        }

        var orders = await context.Orders
            .Include(order => order.MoneyReconciliation)
            .Where(order => order.SellerId == sellerId.Value && selectedOrderIds.Contains(order.OrderId))
            .ToListAsync();
        if (orders.Count != selectedOrderIds.Distinct().Count()
            || orders.Any(order => order.OrderStatus.ToUpper() != "PROCESSING"))
        {
            TempData["ErrorMessage"] = "只有待出貨的訂單可以確認出貨。";
            return RedirectToAction(nameof(OrderManage));
        }
        var now = DateTime.Now;
        foreach (var order in orders)
        {
            order.OrderStatus = "SHIPPED";
            order.OrderUpdatedAt = now;
            context.OrderHistories.Add(new OrderHistory
            {
                OrderNo = order.OrderNo,
                OrderStatus = "SHIPPED",
                ChangeTime = order.OrderUpdatedAt.Value,
                ChangeReason = "賣家確認出貨",
                ChangedByUserId = sellerId.Value
            });

            if (order.MoneyReconciliation is null)
            {
                var platformRevenue = (int)Math.Round(order.OrderAmount * 0.05m, MidpointRounding.AwayFromZero);
                context.MoneyReconciliations.Add(new MoneyReconciliation
                {
                    OrderId = order.OrderId,
                    OrderAmount = order.OrderAmount,
                    PlatformRevenue = platformRevenue,
                    SellerPayout = order.OrderAmount - platformRevenue,
                    AdjustAmount = 0,
                    IsManual = false,
                    AdminId = null,
                    CreatedAt = now,
                    RemitStatus = "PENDING",
                    RemitResult = null,
                    RemitDate = null
                });
            }
        }

        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"已將 {orders.Count} 筆訂單更新為已出貨。";
        return RedirectToAction(nameof(OrderManage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MergeOrders(List<int>? selectedOrderIds)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        if (selectedOrderIds is not { Count: > 1 })
        {
            TempData["ErrorMessage"] = "請至少勾選兩筆訂單後再合併。";
            return RedirectToAction(nameof(OrderManage));
        }

        var orders = await context.Orders
            .Include(order => order.OrderItems)
            .Include(order => order.OrderHistories)
            .Include(order => order.Payments)
            .Include(order => order.Logistic)
            .Include(order => order.MoneyReconciliation)
            .Include(order => order.Reviews)
            .Where(order => order.SellerId == sellerId.Value && selectedOrderIds.Contains(order.OrderId))
            .OrderBy(order => order.OrderedAt)
            .ToListAsync();

        if (orders.Count != selectedOrderIds.Distinct().Count())
        {
            TempData["ErrorMessage"] = "選取的訂單資料不存在或不屬於目前賣家。";
            return RedirectToAction(nameof(OrderManage));
        }

        var target = orders[0];
        var sources = orders.Skip(1).ToList();
        if (orders.Any(order => !IsOrderActionable(order.OrderStatus)))
        {
            TempData["ErrorMessage"] = "已完成、已出貨或已取消的訂單不可合併。";
            return RedirectToAction(nameof(OrderManage));
        }
        if (orders.Any(order => order.BuyerId != target.BuyerId
            || order.ReceiverPhone != target.ReceiverPhone
            || order.ShippingAddress != target.ShippingAddress))
        {
            TempData["ErrorMessage"] = "僅能合併同一買家且收件資訊相同的訂單。";
            return RedirectToAction(nameof(OrderManage));
        }

        if (orders.Any(order => order.Logistic is not null || order.MoneyReconciliation is not null || order.Reviews.Any()))
        {
            TempData["ErrorMessage"] = "已建立物流、對帳或評價紀錄的訂單不可合併。";
            return RedirectToAction(nameof(OrderManage));
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        foreach (var source in sources)
        {
            foreach (var item in source.OrderItems) item.OrderId = target.OrderId;
            foreach (var history in source.OrderHistories) history.OrderNo = target.OrderNo;
            foreach (var payment in source.Payments) payment.OrderId = target.OrderId;
            target.OrderAmount += source.OrderAmount;
            target.OrderDeposit += source.OrderDeposit;
            context.Orders.Remove(source);
        }

        target.OrderUpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["SuccessMessage"] = $"訂單已合併至 {target.OrderNo}。";
        return RedirectToAction(nameof(OrderManage));
    }

    [HttpGet]
    public async Task<IActionResult> MyStore(int? sellerId, string? productType)
    {
        sellerId ??= await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return NotFound();

        var seller = await context.Sellers
            .AsNoTracking()
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.UserId == sellerId.Value);
        if (seller is null) return NotFound();

        var now = DateTime.Now;
        var publishedProducts = context.Products
            .AsNoTracking()
            .Include(product => product.ProductImages)
            .Include(product => product.ProductSpecs)
            .Where(product => product.UserId == sellerId.Value
                && product.ProductStatus == "PUBLISHED"
                && (!product.PublishedAt.HasValue || product.PublishedAt <= now));

        var categories = await publishedProducts
            .Where(product => !string.IsNullOrWhiteSpace(product.ProductType))
            .Select(product => product.ProductType!)
            .Distinct()
            .OrderBy(type => type)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(productType))
            publishedProducts = publishedProducts.Where(product => product.ProductType == productType);

        var products = await publishedProducts
            .OrderByDescending(product => product.PublishedAt ?? product.CreatedAt)
            .ToListAsync();

        var reviews = context.Reviews.AsNoTracking().Where(review => review.RevieweeId == sellerId.Value);
        var reviewCount = await reviews.CountAsync();
        var averageRating = reviewCount == 0 ? 0 : await reviews.AverageAsync(review => (double)review.Rating);
        var reviewItems = await reviews
            .OrderByDescending(review => review.ReviewCreatedAt)
            .Select(review => new SellerStoreReviewItem
            {
                ReviewerName = review.Reviewer.DisplayName ?? review.Reviewer.Username ?? "匿名買家",
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.ReviewCreatedAt
            })
            .ToListAsync();

        return View(new SellerStoreViewModel
        {
            Seller = seller,
            Products = products,
            Categories = categories,
            ProductType = productType,
            ReviewCount = reviewCount,
            AverageRating = averageRating,
            Reviews = reviewItems
        });
    }

    [HttpGet]
    public async Task<IActionResult> StoreSetting()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return RedirectToAction("Login", "UserLogin");

        var seller = await context.Sellers
            .AsNoTracking()
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.UserId == userId.Value);

        if (seller is not null)
        {
            return View(new StoreSettingInput
            {
                StoreName = seller.StoreName,
                StoreDescription = seller.StoreDescription,
                AvatarUrl = seller.User.Avatar
            });
        }

        var approvedUser = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId.Value
                && item.SellerVerificationStatus == "APPROVED");
        if (approvedUser is null) return Forbid();

        ViewData["IsSeller"] = false;
        return View(new StoreSettingInput
        {
            IsCreate = true,
            AvatarUrl = approvedUser.Avatar
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StoreSetting(StoreSettingInput input)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return RedirectToAction("Login", "UserLogin");

        var seller = await context.Sellers
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.UserId == userId.Value);
        var user = seller?.User ?? await context.Users
            .SingleOrDefaultAsync(item => item.UserId == userId.Value);
        if (user is null) return NotFound();

        var isCreate = seller is null;
        if (isCreate && user.SellerVerificationStatus != "APPROVED") return Forbid();

        input.IsCreate = isCreate;
        input.AvatarUrl = user.Avatar;
        var storeName = input.StoreName?.Trim();
        var storeDescription = input.StoreDescription?.Trim();
        if (string.IsNullOrWhiteSpace(storeName))
            ModelState.AddModelError(nameof(input.StoreName), "請輸入賣場名稱。");
        if (storeName?.Length > 50)
            ModelState.AddModelError(nameof(input.StoreName), "賣場名稱最多 50 個字。");
        if (storeDescription?.Length > 100)
            ModelState.AddModelError(nameof(input.StoreDescription), "賣場介紹最多 100 個字。");
        if (input.Avatar is { Length: > 0 }
            && (input.Avatar.Length > 5 * 1024 * 1024
                || !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(input.Avatar.FileName).ToLowerInvariant())))
            ModelState.AddModelError(nameof(input.Avatar), "請上傳 JPG、PNG 或 WEBP，檔案不得超過 5 MB。");

        if (!ModelState.IsValid)
        {
            ViewData["IsSeller"] = !isCreate;
            return View(input);
        }

        var now = DateTime.Now;
        var changed = isCreate;
        if (isCreate)
        {
            var fullName = await context.SellerApplications
                .AsNoTracking()
                .Where(application => application.UserId == userId.Value
                    && application.SellerStatus == "APPROVED")
                .OrderByDescending(application => application.ApplyAt)
                .Select(application => application.RealName)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(fullName)) return Forbid();

            seller = new Seller
            {
                UserId = userId.Value,
                FullName = fullName,
                StoreName = storeName!,
                StoreDescription = storeDescription,
                StoreStatus = "ACTIVE",
                StoreCreatedAt = now,
                StoreUpdatedAt = now,
                User = user
            };
            context.Sellers.Add(seller);
        }
        else
        {
            if (!string.Equals(seller!.StoreName, storeName, StringComparison.Ordinal))
            {
                seller.StoreName = storeName!;
                changed = true;
            }
            if (!string.Equals(seller.StoreDescription, storeDescription, StringComparison.Ordinal))
            {
                seller.StoreDescription = storeDescription;
                changed = true;
            }
        }

        if (input.Avatar is { Length: > 0 })
        {
            var extension = Path.GetExtension(input.Avatar.FileName).ToLowerInvariant();
            var fileName = $"seller_{userId.Value}_{Guid.NewGuid():N}{extension}";
            var directory = Path.Combine(environment.WebRootPath, "images", "avatar");
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, fileName);
            await using var stream = System.IO.File.Create(filePath);
            await input.Avatar.CopyToAsync(stream);
            user.Avatar = $"/images/avatar/{fileName}";
            changed = true;
        }

        if (changed)
        {
            seller!.StoreUpdatedAt = now;
            user.UserUpdatedAt = now;
            await context.SaveChangesAsync();
            TempData["StoreSettingSuccess"] = isCreate ? "賣場已建立。" : "賣場資料已更新。";
        }
        else
        {
            TempData["StoreSettingInfo"] = "沒有需要更新的資料。";
        }

        return RedirectToAction(nameof(StoreSetting));
    }

    [NonAction]
    public async Task<IActionResult> StoreSettingLegacyGet()
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");

        var seller = await context.Sellers
            .AsNoTracking()
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.UserId == sellerId.Value);
        if (seller is null) return NotFound();

        return View(new StoreSettingInput
        {
            StoreName = seller.StoreName,
            StoreDescription = seller.StoreDescription,
            AvatarUrl = seller.User.Avatar
        });
    }

    [NonAction]
    public async Task<IActionResult> StoreSettingLegacyPost(StoreSettingInput input)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");

        var seller = await context.Sellers
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.UserId == sellerId.Value);
        if (seller is null) return NotFound();

        input.AvatarUrl = seller.User.Avatar;
        var storeName = input.StoreName?.Trim();
        var storeDescription = input.StoreDescription?.Trim();
        if (string.IsNullOrWhiteSpace(storeName))
            ModelState.AddModelError(nameof(input.StoreName), "請輸入賣場名稱。 ");
        if (storeName?.Length > 50)
            ModelState.AddModelError(nameof(input.StoreName), "賣場名稱最多 50 字。 ");
        if (storeDescription?.Length > 100)
            ModelState.AddModelError(nameof(input.StoreDescription), "賣場介紹最多 100 字。 ");
        if (input.Avatar is { Length: > 0 }
            && (input.Avatar.Length > 5 * 1024 * 1024
                || !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(input.Avatar.FileName).ToLowerInvariant())))
            ModelState.AddModelError(nameof(input.Avatar), "頭像僅限 JPG、PNG、WEBP，且檔案不得超過 5 MB。 ");

        if (!ModelState.IsValid) return View(input);

        var changed = false;
        if (!string.Equals(seller.StoreName, storeName, StringComparison.Ordinal))
        {
            seller.StoreName = storeName!;
            changed = true;
        }
        if (!string.Equals(seller.StoreDescription, storeDescription, StringComparison.Ordinal))
        {
            seller.StoreDescription = storeDescription;
            changed = true;
        }
        if (input.Avatar is { Length: > 0 })
        {
            var extension = Path.GetExtension(input.Avatar.FileName).ToLowerInvariant();
            var fileName = $"seller_{sellerId.Value}_{Guid.NewGuid():N}{extension}";
            var directory = Path.Combine(environment.WebRootPath, "images", "avatar");
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, fileName);
            await using var stream = System.IO.File.Create(filePath);
            await input.Avatar.CopyToAsync(stream);
            seller.User.Avatar = $"/images/avatar/{fileName}";
            changed = true;
        }

        if (changed)
        {
            var now = DateTime.Now;
            seller.StoreUpdatedAt = now;
            seller.User.UserUpdatedAt = now;
            await context.SaveChangesAsync();
            TempData["StoreSettingSuccess"] = "賣場資料已更新。";
        }
        else
        {
            TempData["StoreSettingInfo"] = "沒有需要更新的資料。";
        }

        return RedirectToAction(nameof(StoreSetting));
    }

    [HttpGet]
    public async Task<IActionResult> ProductManage(string? productType, string? status, string? search, int page = 1)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        var products = context.Products.AsNoTracking().Where(product => product.UserId == sellerId.Value);
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
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        var products = await context.Products.Where(product => product.UserId == sellerId.Value && productIds.Contains(product.ProductId)).ToListAsync();
        foreach (var product in products) { product.ProductStatus = status; product.PublishedAt = status == "PUBLISHED" ? DateTime.Now : null; product.UpdatedAt = DateTime.Now; }
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"已更新 {products.Count} 件商品狀態。";
        return RedirectToAction(nameof(ProductManage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        var product = await context.Products.Include(item => item.ProductImages).Include(item => item.ProductSpecs).SingleOrDefaultAsync(item => item.ProductId == productId && item.UserId == sellerId.Value);
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
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        var products = await context.Products.Include(product => product.ProductImages).Include(product => product.ProductSpecs).Where(product => product.UserId == sellerId.Value && productIds.Contains(product.ProductId)).ToListAsync();
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
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        if (!id.HasValue) return View(new AddProductInput());
        var product = await context.Products.Include(item => item.ProductImages).Include(item => item.ProductSpecs).SingleOrDefaultAsync(item => item.ProductId == id && item.UserId == sellerId.Value);
        if (product == null) return NotFound();
        return View(new AddProductInput { ProductId = product.ProductId, ProductName = product.ProductName, Description = product.Description, Location = product.Location, ProductType = product.ProductType, ProductStatus = product.ProductStatus ?? "DRAFT", SaleType = product.ProductSpecs.FirstOrDefault()?.PreSale == true ? "preorder" : "instock", ExistingImages = product.ProductImages.OrderBy(image => image.ImageOrder).Select(image => image.ImageUrl).ToList(), Specs = product.ProductSpecs.Select(spec => new ProductSpecInput { Category = spec.SpecsCategory2 ?? "", Option = spec.SpecsCategory1, Price = spec.SpecsPrice, Stock = spec.Stock, Deposit = spec.Deposit }).ToList() });
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
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        var specs = input.Specs;
        ValidateProduct(input, specs);

        // ===== 新增：上架前檢查商品名稱與商品描述的違禁字 =====
        if (input.ProductStatus == "PUBLISHED")
        {
            var reviewResults = await bannedWordReviewService.ReviewAsync(
                new Dictionary<string, string?>
                {
                    ["商品名稱"] = input.ProductName,
                    ["商品描述"] = input.Description
                });
            void AddBannedWordError(string fieldDisplayName, string modelStateKey)
            {
                var words = reviewResults
                    .Where(result => result.FieldDisplayName == fieldDisplayName)
                    .Select(result => result.MatchedWord)
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                if (words.Count > 0)
                {
                    ModelState.AddModelError(
                        modelStateKey,
                        $"{fieldDisplayName}包含違禁字「{string.Join("」、「", words)}」，不符合審核。");
                }
            }

            AddBannedWordError("商品名稱", nameof(input.ProductName));
            AddBannedWordError("商品描述", nameof(input.Description));
        }
        // ===== 新增結束 =====


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
                    .SingleOrDefaultAsync(item => item.ProductId == input.ProductId && item.UserId == sellerId.Value)
                : null;
            if (isUpdate && product == null) return NotFound();
            var existingPublishedAt = product?.PublishedAt;
            product ??= new Product { UserId = sellerId.Value, CreatedAt = now };
            product.ProductName = input.ProductName?.Trim() ?? string.Empty;
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
                SpecsCategory1 = spec.Option!.Trim(), // 加 ! 避開 null 警告
                SpecsCategory2 = string.IsNullOrWhiteSpace(spec.Category) ? null : spec.Category.Trim(),

                // 使用 ?? 0 將 decimal? 與 int? 轉回一般 decimal 與 int
                SpecsPrice = (int)(spec.Price ?? 0),
                Stock = (int)(spec.Stock ?? 0),
                Deposit = (int)(spec.Deposit ?? 0),

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
        if (input.Images?.Count > 9)
            ModelState.AddModelError(nameof(input.Images), "商品照片最多 9 張。");

        if (input.Images?.Any(file => file.Length == 0 || file.Length > 5 * 1024 * 1024 ||
            !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(file.FileName).ToLowerInvariant())) == true)
            ModelState.AddModelError(nameof(input.Images), "照片僅限 JPG、PNG、WEBP，且每張不得超過 5 MB。");

        // 規格組數檢查
        if (input.Specs == null || input.Specs.Count == 0)
            ModelState.AddModelError(nameof(input.Specs), "請至少新增一組商品規格。");

        // 規格重複檢查
        if (input.Specs != null && input.Specs
            .GroupBy(spec => $"{(spec.Category ?? string.Empty).Trim()}\u001f{(spec.Option ?? string.Empty).Trim()}", StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            ModelState.AddModelError(nameof(input.Specs), "同一規格類別不可有重複的商品選項。");
        }
    }

    [HttpGet]
    public async Task<IActionResult> MyRevenue(DateTime? startDate, DateTime? endDate, string status = "全部")
    {
        var sellerId = await GetCurrentSellerIdAsync();
        if (!sellerId.HasValue) return RedirectToAction("Login", "UserLogin");
        var reconciliations = context.MoneyReconciliations
            .Where(item => item.Order.SellerId == sellerId.Value)
            .AsQueryable();

        if (startDate.HasValue)
            reconciliations = reconciliations.Where(item => item.RemitDate >= startDate.Value.Date);

        if (endDate.HasValue)
        {
            var nextDay = endDate.Value.Date.AddDays(1);
            reconciliations = reconciliations.Where(item => item.RemitDate < nextDay);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "全部")
        {
            reconciliations = status switch
            {
                "待撥款" => reconciliations.Where(item => item.RemitStatus == "PENDING" || item.RemitResult == "待撥款"),
                "已撥款" => reconciliations.Where(item => item.RemitStatus == "COMPLETED" || item.RemitStatus == "SUCCESS" || item.RemitResult == "已撥款" || item.RemitResult == "撥款完成" || item.RemitResult == "撥款成功" || item.RemitResult == "匯款完成"),
                "撥款取消" => reconciliations.Where(item => item.RemitStatus == "CANCELLED" || item.RemitStatus == "FAILED" || item.RemitResult == "撥款取消" || item.RemitResult == "撥款失敗"),
                _ => reconciliations
            };
        }

        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentStatus = status;

        var model = await reconciliations
            .Select(item => new RevenueViewModel
            {
                OrderNo = item.Order != null ? item.Order.OrderNo : "-",
                RemitResult = item.RemitResult,
                RemitStatus = item.RemitStatus,
                OrderAmount = item.OrderAmount,
                PlatformRevenue = item.PlatformRevenue,
                AdjustAmount = item.AdjustAmount,
                SellerPayout = item.SellerPayout,
                RemitDate = item.RemitDate
            })
            .ToListAsync();

        foreach (var item in model)
            item.RemitResult = GetRemitStatusText(item.RemitResult, item.RemitStatus);

        return View(model);
    }
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

public class SellerOrderManageViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public List<SellerOrderListItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public class ToBuyListViewModel
{
    public List<ToBuyListGroup> Groups { get; set; } = [];
    public List<ToBuyOrderProgress> Orders { get; set; } = [];
    public int TotalQuantity => Groups.Sum(group => group.TotalQuantity);
    public int TotalOrderCount => Groups.SelectMany(group => group.OrderIds).Distinct().Count();
}

public class ToBuyListGroup
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSpec { get; set; }
    public string? ProductSpec2 { get; set; }
    public string? ImageUrl { get; set; }
    public int TotalQuantity { get; set; }
    public List<int> OrderItemIds { get; set; } = [];
    public List<ToBuyPurchaseItem> Items { get; set; } = [];
    public List<int> OrderIds { get; set; } = [];
    public List<string> OrderNumbers { get; set; } = [];
}

public class ToBuyOrderProgress
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }
    public List<ToBuyPurchaseItem> Items { get; set; } = [];
}

public class ToBuyPurchaseItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int Quantity { get; set; }
}

public class SellerOrderListItem
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int OrderAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }
}

public class SellerStoreViewModel
{
    public Seller Seller { get; set; } = null!;
    public List<Product> Products { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public string? ProductType { get; set; }
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }
    public List<SellerStoreReviewItem> Reviews { get; set; } = [];
}

public class SellerStoreReviewItem
{
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StoreSettingInput
{
    public bool IsCreate { get; set; }
    public string? StoreName { get; set; }
    public string? StoreDescription { get; set; }
    public IFormFile? Avatar { get; set; }
    public string? AvatarUrl { get; set; }
}
