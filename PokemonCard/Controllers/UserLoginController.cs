using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonCard.Models;
using System;
using System.Linq;
using System.Security.Claims;

namespace PokemonCard.Controllers
{
    public class UserLoginController : Controller
    {
        private readonly PicartchuContext _context;
                private readonly IWebHostEnvironment _env;

                public UserLoginController(PicartchuContext context, IWebHostEnvironment env)
                {
                    _context = context;
                    _env = env;
                }

        #region 會員註冊
// 1. 載入頁面 (GET) - 絕對不要加 [ValidateAntiForgeryToken]
[HttpGet]
public IActionResult Register()
{
    return View();
}

// 2. 表單送出 (POST) - Token 驗證放在這裡
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Register(string realName, string email, string password, string confirmPassword, string phone)
{
    if (password != confirmPassword)
        {
            TempData["RegisterFailed"] = true;
            return RedirectToAction("Register");
        }

    string safeDisplayName = string.IsNullOrEmpty(realName) ? "會員" : realName;
    if (safeDisplayName.Length > 10)
    {
        safeDisplayName = safeDisplayName.Substring(0, 10);
    }

    var user = new User
    {
        DisplayName = safeDisplayName,
        Username = string.IsNullOrEmpty(email) ? "user_" + DateTime.Now.Ticks : email.Split('@')[0],
        Email = email ?? "",
        PasswordHash = password,
        Phone = phone,
        Provider = "LOCAL",
        UserStatus = "ACTIVE",
        SellerVerificationStatus = "NONE",
        UserCreatedAt = DateTime.Now,
        UserUpdatedAt = DateTime.Now
    };

    try
    {
        _context.Users.Add(user);
        _context.SaveChanges();

        return RedirectToAction("Login");
    }
    catch (Exception ex)
        {
            TempData["RegisterFailed"] = true;
            return RedirectToAction("Register");
        }
}
#endregion

        #region 會員登入 / 登出
                [HttpGet]
                public IActionResult Login()
                {
                    // 已登入的話直接回首頁
                    if (User.Identity!.IsAuthenticated)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    return View();
                }

                [HttpPost]
                [ValidateAntiForgeryToken]
                public async Task<IActionResult> Login(string email, string password)
                {
                    var user = _context.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == password);

                    if (user != null)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                            new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
                            new Claim(ClaimTypes.Email, user.Email)
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                        return RedirectToAction("Index", "Home");
                    }

                    ViewBag.ErrorMessage = "帳號或密碼錯誤！";
                    return View();
                }

                [HttpPost]
                [ValidateAntiForgeryToken]
                public async Task<IActionResult> Logout()
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Index", "Home");
                }
                #endregion


        #region 編輯個人資料（需登入）
                [Authorize]
                [HttpGet]
                public IActionResult EditProfile()
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    var user = _context.Users.Find(userId);
                    if (user == null)
                    {
                        return NotFound();
                    }
                    return View(user);
                }

                [Authorize]
                                [HttpPost]
                                [ValidateAntiForgeryToken]
                                public async Task<IActionResult> EditProfile(string displayName, string phone, string birthday, string email, IFormFile avatar)
                                {
                                    // 從登入 Cookie 取得會員編號（一定以 Claims 為準，避免偽造）
                                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                                    var user = _context.Users.Find(userId);
                                    if (user == null)
                                    {
                                        return NotFound();
                                    }

                                    // 只更新表單實際送出的欄位，不動 Username / PasswordHash / Provider 等沒送的欄位
                                    if (!string.IsNullOrWhiteSpace(displayName))
                                    {
                                        user.DisplayName = displayName.Length > 10 ? displayName.Substring(0, 10) : displayName;
                                    }
                                    if (!string.IsNullOrWhiteSpace(phone))
                                    {
                                        user.Phone = phone;
                                    }
                                    if (DateOnly.TryParse(birthday, out var parsedBirthday))
                                    {
                                        user.Birthday = parsedBirthday;
                                    }
                                    if (!string.IsNullOrWhiteSpace(email))
                                    {
                                        user.Email = email;
                                    }

                                    // 大頭貼：存到 wwwroot/images/avatar 並把路徑寫入資料庫
                                    if (avatar != null && avatar.Length > 0)
                                    {
                                        var ext = Path.GetExtension(avatar.FileName);
                                        var fileName = $"user_{userId}_{DateTime.Now.Ticks}{ext}";
                                        var uploadDir = Path.Combine(_env.WebRootPath, "images", "avatar");
                                        Directory.CreateDirectory(uploadDir);
                                        var filePath = Path.Combine(uploadDir, fileName);
                                        using (var stream = new FileStream(filePath, FileMode.Create))
                                        {
                                            await avatar.CopyToAsync(stream);
                                        }
                                        user.Avatar = $"/images/avatar/{fileName}";
                                    }

                                    user.UserUpdatedAt = DateTime.Now;

                                    try
                                    {
                                        _context.SaveChanges();
                                        TempData["ProfileUpdated"] = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        TempData["ProfileFailed"] = true;
                                    }

                                    return RedirectToAction("EditProfile");
                                }
        #endregion

        #region 賣家申請（需登入）
        [Authorize]
        [HttpGet]
        public IActionResult SellerApplication()
        {
            // ===== [賣家申請補件新增開始] 依目前申請狀態導向審核頁或帶回補件資料 =====
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Challenge();
            }

            var application = _context.SellerApplications
                .FirstOrDefault(s => s.UserId == userId);

            if (application?.SellerStatus == "PENDING"
                || application?.SellerStatus == "APPROVED")
            {
                return RedirectToAction(nameof(SellerApplicationPending));
            }

            if (application != null)
            {
                ViewBag.ApplicationStatus = application.SellerStatus;
                ViewBag.RealName = application.RealName;
                ViewBag.IdNumber = application.IdNumber;
                ViewBag.ContactPhone = application.ContactPhone;
                ViewBag.BankCode = application.BankCode;
                ViewBag.BankAccount = application.BankAccount;

                if (application.SellerStatus == "REJECTED")
                {
                    ViewBag.RejectReason = _context.SellerApplicationAudits
                        .Where(a => a.ApplicationId == application.ApplicationId
                                 && a.AuditStatus == "REJECTED")
                        .OrderByDescending(a => a.ReviewedAt)
                        .Select(a => a.AuditNote)
                        .FirstOrDefault();
                }
            }
            // ===== [賣家申請補件新增結束] =====

            return View();
        }

        // ===== [賣家審核狀態頁新增開始] 待審核與已通過共用同一個狀態頁 =====
        [Authorize]
        [HttpGet]
        public IActionResult SellerApplicationPending()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Challenge();
            }

            var application = _context.SellerApplications
                .FirstOrDefault(s => s.UserId == userId);

            if (application == null
                || (application.SellerStatus != "PENDING"
                    && application.SellerStatus != "APPROVED"))
            {
                return RedirectToAction(nameof(SellerApplication));
            }

            ViewBag.ApplicationStatus = application.SellerStatus;
            return View();
        }
        // ===== [賣家審核狀態頁新增結束] =====

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SellerApplication(string realName, string idNumber, string contactPhone, string bankCode, string bankAccount, IFormFile idFront, IFormFile idBack)
        {
            // ===== [賣家申請補件新增開始] 讀取原申請、保留退回原因並阻止重複申請 =====
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Challenge();
            }

            var application = _context.SellerApplications
                .FirstOrDefault(s => s.UserId == userId);

            ViewBag.RealName = realName;
            ViewBag.IdNumber = idNumber;
            ViewBag.ContactPhone = contactPhone;
            ViewBag.BankCode = bankCode;
            ViewBag.BankAccount = bankAccount;
            ViewBag.ApplicationStatus = application?.SellerStatus;

            if (application?.SellerStatus == "REJECTED")
            {
                ViewBag.RejectReason = _context.SellerApplicationAudits
                    .Where(a => a.ApplicationId == application.ApplicationId
                             && a.AuditStatus == "REJECTED")
                    .OrderByDescending(a => a.ReviewedAt)
                    .Select(a => a.AuditNote)
                    .FirstOrDefault();
            }

            if (application?.SellerStatus == "PENDING"
                || application?.SellerStatus == "APPROVED")
            {
                return RedirectToAction(nameof(SellerApplicationPending));
            }

            if (application != null && application.SellerStatus != "REJECTED")
            {
                ViewBag.ApplicationStatus = application.SellerStatus;
                ViewBag.ErrorMessage = "目前的申請狀態無法重新提交！";
                return View();
            }
            // ===== [賣家申請補件新增結束] =====

            // 基本必填檢查
            if (string.IsNullOrWhiteSpace(realName) || string.IsNullOrWhiteSpace(idNumber) ||
                string.IsNullOrWhiteSpace(contactPhone) || string.IsNullOrWhiteSpace(bankCode) ||
                string.IsNullOrWhiteSpace(bankAccount))
            {
                ViewBag.ErrorMessage = "請填寫完整的申請資料！";
                return View();
            }

            // 身分證正反面皆必須上傳
            if (idFront == null || idFront.Length == 0 || idBack == null || idBack.Length == 0)
            {
                ViewBag.ErrorMessage = "請上傳身分證正反面照片！";
                return View();
            }

            // ===== [賣家申請補件新增開始] 無紀錄才新增，被退回則更新原有紀錄 =====
            if (application == null)
            {
                application = new SellerApplication
                {
                    UserId = userId
                };
                _context.SellerApplications.Add(application);
            }

            application.RealName = realName;
            application.IdNumber = idNumber;
            application.ContactPhone = contactPhone;
            application.BankCode = bankCode;
            application.BankAccount = bankAccount;
            application.SellerStatus = "PENDING";
            application.ApplyAt = DateTime.Now;
            // ===== [賣家申請補件新增結束] =====

            // 身分證照片：存到 wwwroot/images/idcard 並把路徑寫入資料庫
            var uploadDir = Path.Combine(_env.WebRootPath, "images", "idcard");
            Directory.CreateDirectory(uploadDir);

            var frontExt = Path.GetExtension(idFront.FileName);
            var frontName = $"id_{userId}_front_{DateTime.Now.Ticks}{frontExt}";
            using (var stream = new FileStream(Path.Combine(uploadDir, frontName), FileMode.Create))
            {
                await idFront.CopyToAsync(stream);
            }
            application.IdcardFront = $"/images/idcard/{frontName}";

            var backExt = Path.GetExtension(idBack.FileName);
            var backName = $"id_{userId}_back_{DateTime.Now.Ticks}{backExt}";
            using (var stream = new FileStream(Path.Combine(uploadDir, backName), FileMode.Create))
            {
                await idBack.CopyToAsync(stream);
            }
            application.IdcardBack = $"/images/idcard/{backName}";

            try
            {
                _context.SaveChanges();
                TempData["SellerApplied"] = true;
                // ===== [賣家審核狀態頁新增] 送出成功後進入平台審核狀態頁 =====
                return RedirectToAction(nameof(SellerApplicationPending));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "申請送出失敗，請稍後再試！";
                return View();
            }
        }
        #endregion
    }
}