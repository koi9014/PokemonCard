using Microsoft.AspNetCore.Mvc;

namespace PokemonCard.Controllers
{
    public class UserLoginController : Controller
    {
                // 會員註冊頁
        public IActionResult Register()
        {
            return View();
        }

        // 會員登入頁
        public IActionResult Login()
        {
            return View();
        }

        // 編輯個人資料頁
        public IActionResult EditProfile()
        {
            return View();
        }

        // 賣家入駐申請頁
        public IActionResult SellerApplication()
        {
            return View();
        }
    }
}