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
        public IActionResult OrderDetail( )
        {
            var userIdString = User. FindFirstValue(ClaimTypes. NameIdentifier);


            if(string. IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "UserLogin");
            }

            int userId = int. Parse(userIdString);

            var data = _context. Orders
                . Include(p => p. Buyer)
                . Include(p => p. Seller)
                 . Include(p => p. OrderItems)
                     . Include(p => p. OrderItems)
                      . ThenInclude(p => p. Product)
                        . ThenInclude(p => p. ProductImages)
                 . Where(p => p. BuyerId == userId)
                    . ToList( );
            Console. WriteLine($"UserId = {userId}");
            Console. WriteLine($"Order Count = {data. Count}");
            return View(data);


        }
    }
}
