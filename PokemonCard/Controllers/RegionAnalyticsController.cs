using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;

namespace PokemonCard.Controllers;

/// <summary>
/// 管理員專用的日本九大地區訂單分析。
/// </summary>
[Authorize(AuthenticationSchemes = "AdminCookie")]
public class RegionAnalyticsController : Controller
{
    private static readonly string[] RegionNames =
    [
        "北海道", "東北", "關東", "中部", "近畿", "中國", "四國", "九州", "沖繩"
    ];

    private readonly PicartchuContext _context;

    public RegionAnalyticsController(PicartchuContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 日本九大地區訂單熱力地圖頁面。
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// 回傳地區訂購件數、銷售額、訂單數與按月趨勢。
    /// 目前保留所有訂單狀態，讓管理者可先完整檢視資料；後續可再加上狀態篩選。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRegionOrderAnalytics()
    {
        var orderItems = await (
            from item in _context.OrderItems.AsNoTracking()
            join product in _context.Products.AsNoTracking() on item.ProductId equals product.ProductId
            join order in _context.Orders.AsNoTracking() on item.OrderId equals order.OrderId
            select new
            {
                product.Location,
                item.OrderId,
                item.Quantity,
                item.UnitPrice,
                order.OrderedAt
            })
            .ToListAsync();

        var classifiedItems = orderItems
            .Select(item => new
            {
                Region = GetRegionFromLocation(item.Location),
                Location = string.IsNullOrWhiteSpace(item.Location) ? "（空白）" : item.Location.Trim(),
                item.OrderId,
                item.Quantity,
                item.UnitPrice,
                item.OrderedAt
            })
            .ToList();

        var regionStats = RegionNames
            .Select(region =>
            {
                var items = classifiedItems.Where(item => item.Region == region).ToList();

                return new
                {
                    region,
                    orderQuantity = items.Sum(item => (long)item.Quantity),
                    salesAmount = items.Sum(item => (long)item.Quantity * item.UnitPrice),
                    orderCount = items.Select(item => item.OrderId).Distinct().LongCount()
                };
            })
            .ToList();

        var monthlyTrend = classifiedItems
            .Where(item => item.Region is not null)
            .GroupBy(item => new
            {
                Region = item.Region!,
                Month = new DateTime(item.OrderedAt.Year, item.OrderedAt.Month, 1)
            })
            .OrderBy(group => group.Key.Month)
            .ThenBy(group => Array.IndexOf(RegionNames, group.Key.Region))
            .Select(group => new
            {
                region = group.Key.Region,
                month = group.Key.Month.ToString("yyyy-MM"),
                orderQuantity = group.Sum(item => (long)item.Quantity),
                salesAmount = group.Sum(item => (long)item.Quantity * item.UnitPrice),
                orderCount = group.Select(item => item.OrderId).Distinct().LongCount()
            })
            .ToList();

        var unclassifiedLocations = classifiedItems
            .Where(item => item.Region is null)
            .GroupBy(item => item.Location)
            .OrderByDescending(group => group.Sum(item => item.Quantity))
            .Select(group => new
            {
                location = group.Key,
                orderQuantity = group.Sum(item => (long)item.Quantity),
                salesAmount = group.Sum(item => (long)item.Quantity * item.UnitPrice),
                orderCount = group.Select(item => item.OrderId).Distinct().LongCount()
            })
            .ToList();

        return Ok(new
        {
            regions = regionStats,
            monthlyTrend,
            unclassifiedLocations
        });
    }

    private static string? GetRegionFromLocation(string? location)
    {
        var value = location?.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value switch
        {
            "北海道" => "北海道",

            "東北" or "東北地方" or "青森" or "青森縣" or "青森県" or "岩手" or "岩手縣" or "岩手県"
                or "宮城" or "宮城縣" or "宮城県" or "秋田" or "秋田縣" or "秋田県" or "山形" or "山形縣" or "山形県"
                or "福島" or "福島縣" or "福島県" => "東北",

            "關東" or "関東" or "關東地方" or "関東地方" or "茨城" or "茨城縣" or "茨城県" or "栃木" or "栃木縣" or "栃木県"
                or "群馬" or "群馬縣" or "群馬県" or "埼玉" or "埼玉縣" or "埼玉県" or "千葉" or "千葉縣" or "千葉県"
                or "東京" or "東京都" or "神奈川" or "神奈川縣" or "神奈川県" => "關東",

            "中部" or "中部地方" or "新潟" or "新潟縣" or "新潟県" or "富山" or "富山縣" or "富山県"
                or "石川" or "石川縣" or "石川県" or "福井" or "福井縣" or "福井県" or "山梨" or "山梨縣" or "山梨県"
                or "長野" or "長野縣" or "長野県" or "岐阜" or "岐阜縣" or "岐阜県" or "靜岡" or "静岡" or "靜岡縣"
                or "静岡県" or "愛知" or "愛知縣" or "愛知県" => "中部",

            "近畿" or "近畿地方" or "關西" or "関西" or "關西地方" or "関西地方" or "三重" or "三重縣" or "三重県"
                or "滋賀" or "滋賀縣" or "滋賀県" or "京都" or "京都府" or "大阪" or "大阪府" or "兵庫" or "兵庫縣"
                or "兵庫県" or "奈良" or "奈良縣" or "奈良県" or "和歌山" or "和歌山縣" or "和歌山県" => "近畿",

            "中國" or "中国" or "中國地方" or "中国地方" or "鳥取" or "鳥取縣" or "鳥取県" or "島根" or "島根縣" or "島根県"
                or "岡山" or "岡山縣" or "岡山県" or "廣島" or "広島" or "廣島縣" or "広島県" or "山口" or "山口縣"
                or "山口県" => "中國",

            "四國" or "四国" or "四國地方" or "四国地方" or "德島" or "徳島" or "德島縣" or "徳島県" or "香川" or "香川縣"
                or "香川県" or "愛媛" or "愛媛縣" or "愛媛県" or "高知" or "高知縣" or "高知県" => "四國",

            "九州" or "九州地方" or "福岡" or "福岡縣" or "福岡県"
                or "佐賀" or "佐賀縣" or "佐賀県" or "長崎" or "長崎縣" or "長崎県" or "熊本" or "熊本縣" or "熊本県"
                or "大分" or "大分縣" or "大分県" or "宮崎" or "宮崎縣" or "宮崎県" or "鹿兒島" or "鹿児島" or "鹿兒島縣"
                or "鹿児島県" => "九州",

            "沖繩" or "沖縄" or "沖繩縣" or "沖縄県" => "沖繩",

            _ => null
        };
    }
}
