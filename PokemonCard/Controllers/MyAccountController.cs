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
        public IActionResult MemberCenter( )
        {
            // 取得目前登入者的 UserId
            var userIdString = User. FindFirstValue(ClaimTypes. NameIdentifier);

            // 如果沒有登入，導向登入頁
            if(string. IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "UserLogin");
            }

            int userId = int. Parse(userIdString);

            // 判斷這個 User 有沒有 Seller 資料
            bool isSeller = _context. Sellers
                . Any(s => s. UserId == userId);

            // 建立 ViewModel
            var vm = new MemberCenterViewModel
            {
                UserId = userId,
                IsSeller = isSeller
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
                 . Where(p => p. BuyerId == userId)
                    . ToList( );

            return View(data);


        }
    }
}
