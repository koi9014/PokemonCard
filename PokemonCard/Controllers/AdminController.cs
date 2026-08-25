using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;
using System.Security.Claims;

namespace PokemonCard.Controllers
{
    // ===== [管理員登入系統新增] 管理員後台需使用 AdminCookie 登入後才能進入 =====
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminController : Controller
    {
        private readonly PicartchuContext _context;
        private readonly BannedWordReviewService _bannedWordReviewService;


        public AdminController(PicartchuContext context, BannedWordReviewService bannedWordReviewService)
        {
            _context = context;
            _bannedWordReviewService = bannedWordReviewService;
        }



        public IActionResult AdminCenter()
        {
            return View();
        }

        public async Task<IActionResult> SellerAudit(string? keyword, string status = "all")
        {
            var query = _context.SellerApplications
                .Include(application => application.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var keywordText = keyword.Trim();
                query = query.Where(application =>
                    application.RealName.Contains(keywordText) ||
                    application.User.Email.Contains(keywordText) ||
                    (application.User.Username != null && application.User.Username.Contains(keywordText)) ||
                    (application.User.DisplayName != null && application.User.DisplayName.Contains(keywordText)));
            }

            if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(application => application.SellerStatus == status);
            }

            ViewBag.Keyword = keyword ?? string.Empty;
            ViewBag.Status = status;
            ViewBag.AllCount = await _context.SellerApplications.CountAsync();
            ViewBag.PendingCount = await _context.SellerApplications.CountAsync(application => application.SellerStatus == "PENDING");
            ViewBag.RejectedCount = await _context.SellerApplications.CountAsync(application => application.SellerStatus == "REJECTED");
            ViewBag.ApprovedCount = await _context.SellerApplications.CountAsync(application => application.SellerStatus == "APPROVED");

            var applications = await query
                .OrderBy(application => application.SellerStatus == "PENDING" ? 0 : application.SellerStatus == "REJECTED" ? 1 : 2)
                .ThenByDescending(application => application.ApplyAt)
                .ToListAsync();

            return View(applications);
        }

        // ===== [違禁字庫管理新增開始] 違禁字列表與搜尋 =====
        public async Task<IActionResult> BannedWords(string? keyword, string status = "all")
        {
            var query = _context.BannedWords
                .Include(bannedWord => bannedWord.Admin)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(bannedWord => bannedWord.BannedWords.Contains(keyword));
            }

            query = status switch
            {
                "enabled" => query.Where(bannedWord => bannedWord.IsEnabled),
                "disabled" => query.Where(bannedWord => !bannedWord.IsEnabled),
                _ => query
            };

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.EnabledCount = await _context.BannedWords.CountAsync(bannedWord => bannedWord.IsEnabled);
            ViewBag.DisabledCount = await _context.BannedWords.CountAsync(bannedWord => !bannedWord.IsEnabled);

            var bannedWords = await query
                .OrderByDescending(bannedWord => bannedWord.IsEnabled)
                .ThenByDescending(bannedWord => bannedWord.BanCreatedAt)
                .ToListAsync();

            return View(bannedWords);
        }
        // ===== [違禁字庫管理新增結束] =====

        // ===== [違禁字庫管理新增開始] 新增違禁字 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBannedWord(string bannedWords)
        {
            var word = bannedWords?.Trim();

            if (string.IsNullOrWhiteSpace(word))
            {
                TempData["BannedWordError"] = "請輸入違禁字";
                return RedirectToAction(nameof(BannedWords));
            }

            var exists = await _context.BannedWords
                .AnyAsync(bannedWord => bannedWord.BannedWords == word);

            if (exists)
            {
                TempData["BannedWordError"] = "此違禁字已存在";
                return RedirectToAction(nameof(BannedWords));
            }

            var adminId = GetCurrentAdminId();

            _context.BannedWords.Add(new BannedWord
            {
                BannedWords = word,
                IsEnabled = true,
                AdminId = adminId,
                BanCreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["BannedWordMessage"] = "違禁字新增成功";

            return RedirectToAction(nameof(BannedWords));
        }
        // ===== [違禁字庫管理新增結束] =====

        // ===== [違禁字庫管理新增開始] 編輯違禁字文字 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBannedWord(int id, string bannedWords)
        {
            var word = bannedWords?.Trim();

            if (string.IsNullOrWhiteSpace(word))
            {
                TempData["BannedWordError"] = "請輸入違禁字";
                return RedirectToAction(nameof(BannedWords));
            }

            var bannedWord = await _context.BannedWords.FindAsync(id);

            if (bannedWord == null)
            {
                TempData["BannedWordError"] = "找不到指定的違禁字";
                return RedirectToAction(nameof(BannedWords));
            }

            var exists = await _context.BannedWords
                .AnyAsync(otherWord => otherWord.BannedWordsId != id && otherWord.BannedWords == word);

            if (exists)
            {
                TempData["BannedWordError"] = "此違禁字已存在";
                return RedirectToAction(nameof(BannedWords));
            }

            bannedWord.BannedWords = word;
            await _context.SaveChangesAsync();
            TempData["BannedWordMessage"] = "違禁字更新成功";

            return RedirectToAction(nameof(BannedWords));
        }
        // ===== [違禁字庫管理新增結束] =====

        // ===== [違禁字庫管理新增開始] 啟用或停用違禁字，不做刪除以保留審查規則紀錄 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBannedWord(int id)
        {
            var bannedWord = await _context.BannedWords.FindAsync(id);

            if (bannedWord == null)
            {
                TempData["BannedWordError"] = "找不到指定的違禁字";
                return RedirectToAction(nameof(BannedWords));
            }

            bannedWord.IsEnabled = !bannedWord.IsEnabled;
            await _context.SaveChangesAsync();
            TempData["BannedWordMessage"] = bannedWord.IsEnabled ? "違禁字已啟用" : "違禁字已停用";

            return RedirectToAction(nameof(BannedWords));
        }
        // ===== [違禁字庫管理新增結束] =====

        // ===== [違禁字庫管理新增開始] 後台文字審查測試工具 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewBannedWordText(string reviewText)
        {
            if (string.IsNullOrWhiteSpace(reviewText))
            {
                TempData["BannedWordReviewError"] = "請輸入要審查的文字";
                return RedirectToAction(nameof(BannedWords));
            }

            var reviewResults = await _bannedWordReviewService.ReviewAsync(new Dictionary<string, string?>
            {
                ["測試文字"] = reviewText
            });

            TempData["BannedWordReviewText"] = reviewText;
            TempData["BannedWordReviewMatches"] = string.Join("、", reviewResults.Select(result => result.MatchedWord).Distinct());
            TempData["BannedWordReviewPassed"] = (!reviewResults.Any()).ToString();

            return RedirectToAction(nameof(BannedWords));
        }
        // ===== [違禁字庫管理新增結束] =====

        private int GetCurrentAdminId()
        {
            var adminIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(adminIdValue, out var adminId))
            {
                return adminId;
            }

            throw new InvalidOperationException("無法取得目前登入的管理員編號");
        }
    }
}
