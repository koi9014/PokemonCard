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
        private const string ActiveUserStatus = "ACTIVE";
        private const string BannedUserStatus = "BANNED";
        private const string BlockedStatus = "BLOCKED";
        private const string UnblockedStatus = "UNBLOCKED";
        private const int UserPageSize = 10;

        private readonly PicartchuContext _context;
        private readonly BannedWordReviewService _bannedWordReviewService;


        public AdminController(PicartchuContext context, BannedWordReviewService bannedWordReviewService)
        {
            _context = context;
            _bannedWordReviewService = bannedWordReviewService;
        }



        public async Task<IActionResult> AdminCenter(
            string? userKeyword,
            string userStatus = "all",
            int page = 1,
            string analysisType = "revenue",
            string revenueRange = "month",
            DateTime? revenueStart = null,
            DateTime? revenueEnd = null)
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);
            var normalizedKeyword = userKeyword?.Trim() ?? string.Empty;
            var normalizedStatus = userStatus is "active" or "blocked" ? userStatus : "all";
            var normalizedAnalysisType = analysisType == "members" ? "members" : "revenue";
            var normalizedRevenueRange = revenueRange is "7days" or "30days" or "year" or "custom"
                ? revenueRange
                : "month";

            DateTime revenuePeriodStart;
            DateTime revenuePeriodEnd;

            switch (normalizedRevenueRange)
            {
                case "7days":
                    revenuePeriodStart = today.AddDays(-6);
                    revenuePeriodEnd = today.AddDays(1);
                    break;
                case "30days":
                    revenuePeriodStart = today.AddDays(-29);
                    revenuePeriodEnd = today.AddDays(1);
                    break;
                case "year":
                    revenuePeriodStart = new DateTime(today.Year, 1, 1);
                    revenuePeriodEnd = revenuePeriodStart.AddYears(1);
                    break;
                case "custom" when revenueStart.HasValue
                    && revenueEnd.HasValue
                    && revenueEnd.Value.Date >= revenueStart.Value.Date:
                    revenuePeriodStart = revenueStart.Value.Date;
                    revenuePeriodEnd = revenueEnd.Value.Date.AddDays(1);
                    break;
                default:
                    normalizedRevenueRange = "month";
                    revenuePeriodStart = monthStart;
                    revenuePeriodEnd = nextMonthStart;
                    break;
            }

            var previousPeriodEnd = revenuePeriodStart;
            var previousPeriodStart = normalizedRevenueRange switch
            {
                "month" => revenuePeriodStart.AddMonths(-1),
                "year" => revenuePeriodStart.AddYears(-1),
                _ => revenuePeriodStart - (revenuePeriodEnd - revenuePeriodStart)
            };

            var revenueRecords = await _context.MoneyReconciliations
                .AsNoTracking()
                .Where(reconciliation => reconciliation.RemitDate.HasValue
                    && reconciliation.RemitDate >= revenuePeriodStart
                    && reconciliation.RemitDate < revenuePeriodEnd)
                .Select(reconciliation => new
                {
                    reconciliation.RemitDate,
                    reconciliation.PlatformRevenue
                })
                .ToListAsync();

            var previousPeriodRevenue = await _context.MoneyReconciliations
                .AsNoTracking()
                .Where(reconciliation => reconciliation.RemitDate.HasValue
                    && reconciliation.RemitDate >= previousPeriodStart
                    && reconciliation.RemitDate < previousPeriodEnd)
                .SumAsync(reconciliation => (long?)reconciliation.PlatformRevenue) ?? 0;

            var revenueChartLabels = new List<string>();
            var revenueChartValues = new List<long>();
            var useMonthlyRevenueChart = (revenuePeriodEnd - revenuePeriodStart).TotalDays > 62;

            if (useMonthlyRevenueChart)
            {
                for (var cursor = new DateTime(revenuePeriodStart.Year, revenuePeriodStart.Month, 1);
                    cursor < revenuePeriodEnd;
                    cursor = cursor.AddMonths(1))
                {
                    revenueChartLabels.Add(cursor.ToString("yyyy/MM"));
                    revenueChartValues.Add(revenueRecords
                        .Where(record => record.RemitDate!.Value.Year == cursor.Year
                            && record.RemitDate.Value.Month == cursor.Month)
                        .Sum(record => (long)record.PlatformRevenue));
                }
            }
            else
            {
                for (var cursor = revenuePeriodStart.Date;
                    cursor < revenuePeriodEnd;
                    cursor = cursor.AddDays(1))
                {
                    revenueChartLabels.Add(cursor.ToString("MM/dd"));
                    revenueChartValues.Add(revenueRecords
                        .Where(record => record.RemitDate!.Value.Date == cursor)
                        .Sum(record => (long)record.PlatformRevenue));
                }
            }

            var revenuePeriodTotal = revenueRecords.Sum(record => (long)record.PlatformRevenue);
            decimal? revenueGrowthRate = previousPeriodRevenue == 0
                ? null
                : Math.Round(
                    (revenuePeriodTotal - previousPeriodRevenue) * 100m / previousPeriodRevenue,
                    1);

            var memberCreatedDates = await _context.Users
                .AsNoTracking()
                .Where(user => user.UserCreatedAt >= revenuePeriodStart
                    && user.UserCreatedAt < revenuePeriodEnd)
                .Select(user => user.UserCreatedAt)
                .ToListAsync();

            var previousPeriodMemberCount = await _context.Users
                .AsNoTracking()
                .CountAsync(user => user.UserCreatedAt >= previousPeriodStart
                    && user.UserCreatedAt < previousPeriodEnd);

            var memberChartLabels = new List<string>();
            var memberChartValues = new List<int>();

            if (useMonthlyRevenueChart)
            {
                for (var cursor = new DateTime(revenuePeriodStart.Year, revenuePeriodStart.Month, 1);
                    cursor < revenuePeriodEnd;
                    cursor = cursor.AddMonths(1))
                {
                    memberChartLabels.Add(cursor.ToString("yyyy/MM"));
                    memberChartValues.Add(memberCreatedDates.Count(createdAt =>
                        createdAt.Year == cursor.Year && createdAt.Month == cursor.Month));
                }
            }
            else
            {
                for (var cursor = revenuePeriodStart.Date;
                    cursor < revenuePeriodEnd;
                    cursor = cursor.AddDays(1))
                {
                    memberChartLabels.Add(cursor.ToString("MM/dd"));
                    memberChartValues.Add(memberCreatedDates.Count(createdAt => createdAt.Date == cursor));
                }
            }

            var memberPeriodNewCount = memberCreatedDates.Count;
            decimal? memberGrowthRate = previousPeriodMemberCount == 0
                ? null
                : Math.Round(
                    (memberPeriodNewCount - previousPeriodMemberCount) * 100m / previousPeriodMemberCount,
                    1);

            var userQuery = _context.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                var isUserId = int.TryParse(normalizedKeyword, out var userId);
                userQuery = userQuery.Where(user =>
                    (isUserId && user.UserId == userId) ||
                    user.Email.Contains(normalizedKeyword) ||
                    (user.Username != null && user.Username.Contains(normalizedKeyword)) ||
                    (user.DisplayName != null && user.DisplayName.Contains(normalizedKeyword)));
            }

            userQuery = normalizedStatus switch
            {
                "active" => userQuery.Where(user =>
                    !user.UserBlacklists.Any(block => block.UnblockedAt == null)),
                "blocked" => userQuery.Where(user =>
                    user.UserBlacklists.Any(block => block.UnblockedAt == null)),
                _ => userQuery
            };

            var filteredUserCount = await userQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(filteredUserCount / (double)UserPageSize));
            var currentPage = Math.Clamp(page, 1, totalPages);

            var users = await userQuery
                .OrderByDescending(user => user.UserCreatedAt)
                .ThenByDescending(user => user.UserId)
                .Skip((currentPage - 1) * UserPageSize)
                .Take(UserPageSize)
                .Select(user => new AdminUserListItemViewModel
                {
                    UserId = user.UserId,
                    DisplayName = user.DisplayName,
                    Username = user.Username,
                    Email = user.Email,
                    UserStatus = user.UserStatus,
                    SellerVerificationStatus = user.SellerVerificationStatus,
                    UserCreatedAt = user.UserCreatedAt,
                    IsBlacklisted = user.UserBlacklists.Any(block => block.UnblockedAt == null),
                    BlockReason = user.UserBlacklists
                        .Where(block => block.UnblockedAt == null)
                        .OrderByDescending(block => block.BlockedAt)
                        .Select(block => block.ReasonDetail)
                        .FirstOrDefault(),
                    BlockedAt = user.UserBlacklists
                        .Where(block => block.UnblockedAt == null)
                        .OrderByDescending(block => block.BlockedAt)
                        .Select(block => (DateTime?)block.BlockedAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalUserCount = await _context.Users
                    .AsNoTracking()
                    .CountAsync(),
                BlacklistedUserCount = await _context.UserBlacklists
                    .AsNoTracking()
                    .Where(block => block.UnblockedAt == null)
                    .Select(block => block.UserId)
                    .Distinct()
                    .CountAsync(),
                TotalPlatformRevenue = await _context.MoneyReconciliations
                    .AsNoTracking()
                    .Where(reconciliation => reconciliation.RemitDate.HasValue)
                    .SumAsync(reconciliation => (long?)reconciliation.PlatformRevenue) ?? 0,
                RevenueMonth = monthStart,
                AnalysisType = normalizedAnalysisType,
                RevenueRange = normalizedRevenueRange,
                RevenuePeriodStart = revenuePeriodStart,
                RevenuePeriodEnd = revenuePeriodEnd.AddDays(-1),
                RevenuePeriodTotal = revenuePeriodTotal,
                RevenueRecordCount = revenueRecords.Count,
                RevenueGrowthRate = revenueGrowthRate,
                RevenueChartLabels = revenueChartLabels,
                RevenueChartValues = revenueChartValues,
                MemberPeriodNewCount = memberPeriodNewCount,
                MemberGrowthRate = memberGrowthRate,
                MemberChartLabels = memberChartLabels,
                MemberChartValues = memberChartValues,
                Users = users,
                UserKeyword = normalizedKeyword,
                UserStatusFilter = normalizedStatus,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                FilteredUserCount = filteredUserCount
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(
            int userId,
            string? reason,
            string? userKeyword,
            string userStatus = "all",
            int page = 1)
        {
            var normalizedReason = reason?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                TempData["UserManagementError"] = "封鎖使用者時必須填寫原因。";
                return RedirectToAdminCenter(userKeyword, userStatus, page);
            }

            if (normalizedReason.Length > 500)
            {
                TempData["UserManagementError"] = "封鎖原因不可超過 500 個字。";
                return RedirectToAdminCenter(userKeyword, userStatus, page);
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["UserManagementError"] = "找不到指定的使用者。";
                return RedirectToAdminCenter(userKeyword, userStatus, page);
            }

            var isAlreadyBlocked = await _context.UserBlacklists
                .AnyAsync(block => block.UserId == userId && block.UnblockedAt == null);

            if (isAlreadyBlocked)
            {
                TempData["UserManagementError"] = "此使用者目前已在黑名單中。";
                return RedirectToAdminCenter(userKeyword, userStatus, page);
            }

            var now = DateTime.Now;
            user.UserStatus = BannedUserStatus;
            user.UserUpdatedAt = now;

            _context.UserBlacklists.Add(new UserBlacklist
            {
                UserId = userId,
                ReasonDetail = normalizedReason,
                BlockStatus = BlockedStatus,
                AdminId = GetCurrentAdminId(),
                BlockedAt = now
            });

            await _context.SaveChangesAsync();
            TempData["UserManagementMessage"] = $"使用者 #{userId} 已加入黑名單。";

            return RedirectToAdminCenter(userKeyword, userStatus, page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(
            int userId,
            string? userKeyword,
            string userStatus = "all",
            int page = 1)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["UserManagementError"] = "找不到指定的使用者。";
                return RedirectToAdminCenter(userKeyword, userStatus, page);
            }

            var activeBlocks = await _context.UserBlacklists
                .Where(block => block.UserId == userId && block.UnblockedAt == null)
                .ToListAsync();

            if (activeBlocks.Count == 0)
            {
                TempData["UserManagementError"] = "此使用者目前不在黑名單中。";
                return RedirectToAdminCenter(userKeyword, userStatus, page);
            }

            var now = DateTime.Now;
            foreach (var block in activeBlocks)
            {
                block.BlockStatus = UnblockedStatus;
                block.UnblockedAt = now;
            }

            user.UserStatus = ActiveUserStatus;
            user.UserUpdatedAt = now;

            await _context.SaveChangesAsync();
            TempData["UserManagementMessage"] = $"使用者 #{userId} 已解除封鎖。";

            return RedirectToAdminCenter(userKeyword, userStatus, page);
        }

        public async Task<IActionResult> SellerAudit(
            string? keyword,
            string status = "all",
            string chartRange = "month",
            DateTime? chartStart = null,
            DateTime? chartEnd = null)
        {
            var query = _context.SellerApplications
                .AsNoTracking()
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

            // ===== [賣家資格統計圖表新增開始] 計算指定期間的審核通過人數與柱狀圖資料 =====
            var today = DateTime.Today;
            var rangeStart = new DateTime(today.Year, today.Month, 1);
            var rangeEnd = rangeStart.AddMonths(1);

            if (string.Equals(chartRange, "year", StringComparison.OrdinalIgnoreCase))
            {
                rangeStart = new DateTime(today.Year, 1, 1);
                rangeEnd = rangeStart.AddYears(1);
                chartRange = "year";
            }
            else if (string.Equals(chartRange, "custom", StringComparison.OrdinalIgnoreCase)
                && chartStart.HasValue
                && chartEnd.HasValue
                && chartEnd.Value.Date >= chartStart.Value.Date)
            {
                rangeStart = chartStart.Value.Date;
                rangeEnd = chartEnd.Value.Date.AddDays(1);
                chartRange = "custom";
            }
            else
            {
                chartRange = "month";
            }

            var approvedReviewDates = await _context.SellerApplicationAudits
                .AsNoTracking()
                .Where(audit => audit.AuditStatus == "APPROVED"
                    && audit.ReviewedAt >= rangeStart
                    && audit.ReviewedAt < rangeEnd)
                .Select(audit => audit.ReviewedAt)
                .ToListAsync();

            var chartLabels = new List<string>();
            var chartValues = new List<int>();
            var useMonthlyChart = (rangeEnd - rangeStart).TotalDays > 62;

            if (useMonthlyChart)
            {
                for (var month = new DateTime(rangeStart.Year, rangeStart.Month, 1); month < rangeEnd; month = month.AddMonths(1))
                {
                    chartLabels.Add(month.ToString("yyyy-MM"));
                    chartValues.Add(approvedReviewDates.Count(reviewedAt =>
                        reviewedAt.Year == month.Year && reviewedAt.Month == month.Month));
                }
            }
            else
            {
                for (var day = rangeStart.Date; day < rangeEnd; day = day.AddDays(1))
                {
                    chartLabels.Add(day.ToString("MM/dd"));
                    chartValues.Add(approvedReviewDates.Count(reviewedAt => reviewedAt.Date == day));
                }
            }

            ViewBag.ChartRange = chartRange;
            ViewBag.ChartStart = rangeStart.ToString("yyyy-MM-dd");
            ViewBag.ChartEnd = rangeEnd.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.ApprovedSellerCount = approvedReviewDates.Count;
            ViewBag.ApprovalChartLabels = chartLabels;
            ViewBag.ApprovalChartValues = chartValues;
            ViewBag.ApprovalChartUnit = useMonthlyChart ? "月份" : "日期";
            // ===== [賣家資格統計圖表新增結束] =====

            return View(applications);
        }

        // ===== [賣家審核新增開始] 通過賣家申請 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSellerApplication(int applicationId, string? auditNote)
        {
            var application = await _context.SellerApplications
                .Include(sellerApplication => sellerApplication.User)
                .FirstOrDefaultAsync(sellerApplication => sellerApplication.ApplicationId == applicationId);

            if (application == null)
            {
                TempData["SellerAuditError"] = "找不到指定的賣家申請";
                return RedirectToAction(nameof(SellerAudit));
            }

            if (application.SellerStatus != "PENDING")
            {
                TempData["SellerAuditError"] = "此申請已完成審核，請重新整理頁面後再操作";
                return RedirectToAction(nameof(SellerAudit));
            }

            application.SellerStatus = "APPROVED";
            application.User.SellerVerificationStatus = "APPROVED";

            _context.SellerApplicationAudits.Add(new SellerApplicationAudit
            {
                ApplicationId = application.ApplicationId,
                AdminId = GetCurrentAdminId(),
                AuditStatus = "APPROVED",
                AuditNote = string.IsNullOrWhiteSpace(auditNote) ? null : auditNote.Trim(),
                ReviewedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SellerAuditMessage"] = "賣家申請已通過審核";

            return RedirectToAction(nameof(SellerAudit));
        }
        // ===== [賣家審核新增結束] =====

        // ===== [賣家審核新增開始] 退回賣家申請補件 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSellerApplication(int applicationId, string? auditNote)
        {
            if (string.IsNullOrWhiteSpace(auditNote))
            {
                TempData["SellerAuditError"] = "退回補件時必須填寫退回理由";
                return RedirectToAction(nameof(SellerAudit));
            }

            var application = await _context.SellerApplications
                .FirstOrDefaultAsync(sellerApplication => sellerApplication.ApplicationId == applicationId);

            if (application == null)
            {
                TempData["SellerAuditError"] = "找不到指定的賣家申請";
                return RedirectToAction(nameof(SellerAudit));
            }

            if (application.SellerStatus != "PENDING")
            {
                TempData["SellerAuditError"] = "此申請已完成審核，請重新整理頁面後再操作";
                return RedirectToAction(nameof(SellerAudit));
            }

            application.SellerStatus = "REJECTED";

            _context.SellerApplicationAudits.Add(new SellerApplicationAudit
            {
                ApplicationId = application.ApplicationId,
                AdminId = GetCurrentAdminId(),
                AuditStatus = "REJECTED",
                AuditNote = auditNote.Trim(),
                ReviewedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SellerAuditMessage"] = "賣家申請已退回補件";

            return RedirectToAction(nameof(SellerAudit));
        }
        // ===== [賣家審核新增結束] =====

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

        private IActionResult RedirectToAdminCenter(
            string? userKeyword,
            string? userStatus,
            int page)
        {
            var url = Url.Action(nameof(AdminCenter), new
            {
                userKeyword,
                userStatus,
                page
            }) ?? Url.Action(nameof(AdminCenter)) ?? "/Admin/AdminCenter";

            return Redirect($"{url}#user-management");
        }

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

namespace PokemonCard.Models
{
    /// <summary>
    /// 管理員後台首頁的即時統計摘要。
    /// </summary>
    public sealed class AdminDashboardViewModel
    {
        public int TotalUserCount { get; init; }

        public int BlacklistedUserCount { get; init; }

        public long TotalPlatformRevenue { get; init; }

        public DateTime RevenueMonth { get; init; }

        public string AnalysisType { get; init; } = "revenue";

        public string RevenueRange { get; init; } = "month";

        public DateTime RevenuePeriodStart { get; init; }

        public DateTime RevenuePeriodEnd { get; init; }

        public long RevenuePeriodTotal { get; init; }

        public int RevenueRecordCount { get; init; }

        public decimal? RevenueGrowthRate { get; init; }

        public IReadOnlyList<string> RevenueChartLabels { get; init; }
            = Array.Empty<string>();

        public IReadOnlyList<long> RevenueChartValues { get; init; }
            = Array.Empty<long>();

        public int MemberPeriodNewCount { get; init; }

        public decimal? MemberGrowthRate { get; init; }

        public IReadOnlyList<string> MemberChartLabels { get; init; }
            = Array.Empty<string>();

        public IReadOnlyList<int> MemberChartValues { get; init; }
            = Array.Empty<int>();

        public IReadOnlyList<AdminUserListItemViewModel> Users { get; init; }
            = Array.Empty<AdminUserListItemViewModel>();

        public string UserKeyword { get; init; } = string.Empty;

        public string UserStatusFilter { get; init; } = "all";

        public int CurrentPage { get; init; }

        public int TotalPages { get; init; }

        public int FilteredUserCount { get; init; }
    }

    public sealed class AdminUserListItemViewModel
    {
        public int UserId { get; init; }

        public string? DisplayName { get; init; }

        public string? Username { get; init; }

        public string Email { get; init; } = string.Empty;

        public string UserStatus { get; init; } = string.Empty;

        public string SellerVerificationStatus { get; init; } = string.Empty;

        public DateTime UserCreatedAt { get; init; }

        public bool IsBlacklisted { get; init; }

        public string? BlockReason { get; init; }

        public DateTime? BlockedAt { get; init; }
    }
}
