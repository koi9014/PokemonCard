using Microsoft. AspNetCore. Mvc;
using Microsoft. EntityFrameworkCore;
using PokemonCard. Models;
using System. Security. Claims;
using PokemonCard. ViewModels;
using static Microsoft. Extensions. Logging. EventSource. LoggingEventSource;

namespace PokemonCard. Controllers
{
    public class MyAccountController: Controller
    {
        private readonly PicartchuContext _context;
        public MyAccountController( PicartchuContext context )
        {
            _context = context;
        }
        public IActionResult Order( )
        {
            // 取得目前登入者的 UserId
            var userIdString = User. FindFirstValue(ClaimTypes. NameIdentifier);

            // 沒有登入
            if(string. IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "UserLogin");
            }

            int userId = int. Parse(userIdString);

            // 判斷是不是賣家
            bool isSeller = _context. Sellers
                . Any(s => s. UserId == userId);
            bool canCreateStore = !isSeller && _context.Users
                .Any(user => user.UserId == userId && user.SellerVerificationStatus == "APPROVED");

            // 查詢目前使用者的訂單
            var orders = _context. Orders
                . Include(o => o. Buyer)
                . Include(o => o. Seller)
                . Include(o => o. OrderItems)
                    . ThenInclude(oi => oi. Product)
                        . ThenInclude(p => p. ProductImages)
                . Where(o => o. BuyerId == userId)
                        . OrderByDescending(o => o. OrderedAt)  // 最新訂單在最上面

                . ToList( );

            // 建立 ViewModel
            var vm = new MemberCenterViewModel
            {
                UserId = userId,
                IsSeller = isSeller,
                CanCreateStore = canCreateStore,
                Orders = orders
            };

            return View(vm);
        }
        
        public async Task<IActionResult> Index( string? status )
        {
            var userIdString =
                User. FindFirstValue(ClaimTypes. NameIdentifier);

            if(string. IsNullOrEmpty(userIdString))
            {
                return Unauthorized( );
            }

            var userId = int. Parse(userIdString);

            var memberState = await _context.Users
                .Where(user => user.UserId == userId)
                .Select(user => new
                {
                    IsSeller = user.Seller != null,
                    CanCreateStore = user.Seller == null && user.SellerVerificationStatus == "APPROVED"
                })
                .SingleAsync();

            var ordersQuery = _context. Orders
                . Include(o => o. Seller)
                . Include(o => o. OrderItems)
                    . ThenInclude(oi => oi. Product)
                        . ThenInclude(p => p. ProductImages)
                . Where(o => o. BuyerId == userId);

            if(!string. IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery
                    . Where(o => o. OrderStatus == status);
            }

            var orders = await ordersQuery
                . OrderByDescending(o => o. OrderCreatedAt)
                . ToListAsync( );

            var viewModel = new MemberCenterViewModel
            {
                UserId = userId,
                IsSeller = memberState.IsSeller,
                CanCreateStore = memberState.CanCreateStore,
                Orders = orders
            };

            return View("Order", viewModel);

        }
        public async Task<IActionResult> OrderDetail( int id )
        {
            var userIdString = User. FindFirstValue(ClaimTypes. NameIdentifier);

            if(string. IsNullOrEmpty(userIdString))
            {
                return Unauthorized( );
            }

            var userId = int. Parse(userIdString);
            var sellerId = await _context. Sellers
                .Where(seller => seller.UserId == userId)
                .Select(seller => (int?)seller.UserId)
                .FirstOrDefaultAsync();

            var order = await _context. Orders
                . Include(o => o. Seller)
                . Include(o => o.OrderHistories)
                . Include(o => o. OrderItems)
                    . ThenInclude(oi => oi. Product)
                        . ThenInclude(p => p. ProductImages)
                . FirstOrDefaultAsync(o =>
                    o. OrderId == id &&
                    (o. BuyerId == userId ||
                     (sellerId.HasValue && o.SellerId == sellerId.Value)));

            if(order == null)
            {
                return NotFound( );
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceipt(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
                return RedirectToAction("Login", "UserLogin");

            var order = await _context.Orders
                .Include(item => item.MoneyReconciliation)
                .SingleOrDefaultAsync(item => item.OrderId == id && item.BuyerId == userId);
            if (order is null) return NotFound();

            if (!string.Equals(order.OrderStatus, "SHIPPED", StringComparison.OrdinalIgnoreCase))
            {
                TempData["OrderErrorMessage"] = "只有運送中的訂單可以確認收貨。";
                return RedirectToAction(nameof(OrderDetail), new { id });
            }

            var now = DateTime.Now;
            order.OrderStatus = "COMPLETED";
            order.OrderUpdatedAt = now;
            _context.OrderHistories.Add(new OrderHistory
            {
                OrderNo = order.OrderNo,
                OrderStatus = "COMPLETED",
                ChangeTime = now,
                ChangeReason = "買家確認收貨",
                ChangedByUserId = userId
            });

            if (order.MoneyReconciliation is null)
            {
                var platformRevenue = (int)Math.Round(order.OrderAmount * 0.05m, MidpointRounding.AwayFromZero);
                _context.MoneyReconciliations.Add(new MoneyReconciliation
                {
                    OrderId = order.OrderId,
                    OrderAmount = order.OrderAmount,
                    PlatformRevenue = platformRevenue,
                    SellerPayout = order.OrderAmount - platformRevenue,
                    AdjustAmount = 0,
                    IsManual = false,
                    AdminId = null,
                    CreatedAt = now,
                    RemitStatus = "SUCCESS",
                    RemitResult = "已撥款",
                    RemitDate = now
                });
            }
            else
            {
                order.MoneyReconciliation.RemitStatus = "SUCCESS";
                order.MoneyReconciliation.RemitResult = "已撥款";
                order.MoneyReconciliation.RemitDate = now;
            }

            await _context.SaveChangesAsync();
            TempData["OrderSuccessMessage"] = "已確認收貨，訂單已完成。";
            return RedirectToAction(nameof(OrderDetail), new { id });
        }



    }
}
