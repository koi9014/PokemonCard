using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;
using System.Security.Claims;

namespace PokemonCard.Controllers
{
    public class AdminLoginController : Controller
    {
        private readonly PicartchuContext _context;
        private readonly PasswordHasher<AdminUser> _passwordHasher = new();

        public AdminLoginController(PicartchuContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(new AdminLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var admin = await _context.AdminUsers
                .Include(adminUser => adminUser.Role)
                .FirstOrDefaultAsync(adminUser => adminUser.Username == model.Username);

            if (admin == null)
            {
                ModelState.AddModelError(string.Empty, "帳號或密碼錯誤");
                return View("Index", model);
            }

            if (admin.IsLocked)
            {
                ModelState.AddModelError(string.Empty, "此管理員帳號已被鎖定，請聯絡系統管理者");
                return View("Index", model);
            }

            // ===== [管理員登入系統修改開始] 使用雜湊密碼驗證，避免明碼比對 =====
            var passwordResult = _passwordHasher.VerifyHashedPassword(
                admin,
                admin.PasswordHash,
                model.Password
            );

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "帳號或密碼錯誤");
                return View("Index", model);
            }
            // ===== [管理員登入系統修改結束] =====

            admin.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // ===== [管理員登入系統新增開始] 建立管理員登入 Cookie =====
            // Claims 會被寫入加密 Cookie，後續後台頁面可用 User.Identity / User.Claims 取得登入者資訊。
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim("AdminFullName", admin.FullName),
                new Claim(ClaimTypes.Role, admin.Role.RoleName)
            };

            var identity = new ClaimsIdentity(claims, "AdminCookie");
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync("AdminCookie", principal, authProperties);
            // ===== [管理員登入系統新增結束] =====

            return RedirectToAction("AdminCenter", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "AdminCookie")]
        public async Task<IActionResult> Logout()
        {
            // ===== [管理員登入系統新增] 清除管理員登入 Cookie =====
            await HttpContext.SignOutAsync("AdminCookie");
            return RedirectToAction("Index", "AdminLogin");
        }
    }
}
