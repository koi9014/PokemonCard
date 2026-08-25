using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;

namespace PokemonCard.Controllers
{
    // ===== [管理員登入系統新增] 管理員後台需使用 AdminCookie 登入後才能進入 =====
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminController : Controller
    {
        private readonly PicartchuContext _context;


        public AdminController(PicartchuContext context)
        {
            _context = context;
        }



        public IActionResult AdminCenter()
        {
            return View();
        }
    }
}
