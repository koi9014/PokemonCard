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

            // 查詢目前使用者的訂單
            var orders = _context. Orders
                . Include(o => o. Buyer)
                . Include(o => o. Seller)
                . Include(o => o. OrderItems)
                    . ThenInclude(oi => oi. Product)
                        . ThenInclude(p => p. ProductImages)
                . Where(o => o. BuyerId == userId)
                . ToList( );

            // 建立 ViewModel
            var vm = new MemberCenterViewModel
            {
                UserId = userId,
                IsSeller = isSeller,
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

            var order = await _context. Orders
                . Include(o => o. Seller)
                . Include(o => o. OrderItems)
                    . ThenInclude(oi => oi. Product)
                        . ThenInclude(p => p. ProductImages)
                . FirstOrDefaultAsync(o =>
                    o. OrderId == id &&
                    o. BuyerId == userId);

            if(order == null)
            {
                return NotFound( );
            }

            return View(order);
        }



    }
}
