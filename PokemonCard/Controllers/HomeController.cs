using Microsoft. AspNetCore. Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonCard. Models;
using System. Diagnostics;

namespace PokemonCard. Controllers
{
    public class HomeController : Controller
    {
        private readonly PicartchuContext context;

        public HomeController(PicartchuContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index(string? location, string? productType, string? search)
        {
            var now = DateTime.Now;
            var query = context.Products
                .AsNoTracking()
                .Where(product => product.ProductStatus == "PUBLISHED" &&
                    (!product.PublishedAt.HasValue || product.PublishedAt <= now));

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(product => product.Location == location);
            if (!string.IsNullOrWhiteSpace(productType))
                query = query.Where(product => product.ProductType == productType);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(product => EF.Functions.Like(product.ProductName, $"%{search.Trim()}%"));

            var products = await query
                .OrderByDescending(product => product.CreatedAt)
                .Select(product => new HomeProductCard
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ProductType = product.ProductType ?? "未分類",
                    Location = product.Location,
                    LowestPrice = product.ProductSpecs
                        .Select(spec => (int?)spec.SpecsPrice)
                        .Min(),
                    SpecCount = product.ProductSpecs.Count(),
                    AvailableSpecCount = product.ProductSpecs.Count(spec => spec.Stock > 0),
                    DefaultSpecificationId = product.ProductSpecs
                        .Where(spec => spec.Stock > 0)
                        .OrderBy(spec => spec.SpecsPrice)
                        .Select(spec => (int?)spec.SpecificationId)
                        .FirstOrDefault(),
                    ImageUrl = product.ProductImages
                        .OrderBy(image => image.ImageOrder)
                        .Select(image => image.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ViewData["Search"] = search;
            return View(new HomeIndexViewModel { Products = products, Location = location, ProductType = productType, Search = search });
        }

        public IActionResult Privacy( )
        {
            return View( );
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation. None, NoStore = true)]
        public IActionResult Error( )
        {
            return View(new ErrorViewModel { RequestId = Activity. Current?.Id ?? HttpContext. TraceIdentifier });
        }
    }

    public class HomeIndexViewModel
    {
        public List<HomeProductCard> Products { get; set; } = [];
        public string? Location { get; set; }
        public string? ProductType { get; set; }
        public string? Search { get; set; }
    }

    public class HomeProductCard
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int? LowestPrice { get; set; }
        public int SpecCount { get; set; }
        public int AvailableSpecCount { get; set; }
        public int? DefaultSpecificationId { get; set; }
        public string? ImageUrl { get; set; }
    }
}
