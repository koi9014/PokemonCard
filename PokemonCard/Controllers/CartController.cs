using Microsoft. AspNetCore. Mvc;
using Microsoft. EntityFrameworkCore;
using System. Text. Json;
using PokemonCard. Models;
using PokemonCard. ViewModels;

namespace PokemonCard. Controllers
{
    public class CartController : Controller
    {
        private const string CartCookieName = "ShoppingCart";

        private readonly PicartchuContext _context;


        // ==========================================
        // 建構子
        // ==========================================

        public CartController( PicartchuContext context )
        {
            _context = context;
        }


        // ==========================================
        // 購物車頁面
        // ==========================================

        public async Task<IActionResult> Index( )
        {
            var cart = GetCart( );

            var viewModel = new List<CartItemViewModel>( );


            foreach(var item in cart)
            {
                var product = await _context. Products
                    . Include(p => p. ProductImages)
                    . Include(p => p. ProductSpecs)
                    . FirstOrDefaultAsync(p =>
                        p. ProductId == item. ProductId);


                // 商品不存在
                if(product == null)
                {
                    continue;
                }


                // 找目前選擇的規格
                var specification =
                    product. ProductSpecs
                        . FirstOrDefault(s =>
                            s. SpecificationId ==
                            item. SpecificationId);


                // 規格不存在
                if(specification == null)
                {
                    continue;
                }


                // 找第一張商品圖片
                var image =
                    product. ProductImages
                        . OrderBy(i => i. ImageOrder)
                        . FirstOrDefault( );


                // 建立購物車畫面資料
                viewModel. Add(new CartItemViewModel
                {
                    ProductId =
                        product. ProductId,

                    SpecificationId =
                        specification. SpecificationId,

                    Quantity =
                        item. Quantity,

                    ProductName =
                        product. ProductName,

                    ProductImage =
                        image?.ImageUrl,

                    SpecificationName =
                        specification. SpecsCategory1,

                    Price =
                        specification. SpecsPrice,

                    Stock =
                        specification. Stock
                });
            }


            return View(viewModel);
        }


        // ==========================================
        // 加入購物車
        // ==========================================

        [HttpPost]
        public IActionResult Add(
            int productId,
            int specificationId,
            int quantity = 1 )
        {
            // 數量防呆
            if(quantity < 1)
            {
                quantity = 1;
            }


            var cart = GetCart( );


            // 找相同商品 + 相同規格
            var item = cart. FirstOrDefault(
                x => x. ProductId == productId &&
                     x. SpecificationId == specificationId
            );


            if(item == null)
            {
                // 新商品
                cart. Add(new CartItemViewModel
                {
                    ProductId = productId,

                    SpecificationId =
                        specificationId,

                    Quantity = quantity
                });
            }
            else
            {
                // 已存在 → 增加數量
                item. Quantity += quantity;
            }


            SaveCart(cart);


            // 計算購物車總數量
            int cartCount =
                cart. Sum(x => x. Quantity);


            // 回傳 JSON
            return Json(new
            {
                success = true,

                message = "已加入購物車",

                cartCount = cartCount
            });
        }


        // ==========================================
        // 取得購物車數量
        // ==========================================

        [HttpGet]
        public IActionResult GetCartCount( )
        {
            var cart = GetCart( );


            int cartCount =
                cart. Sum(x => x. Quantity);


            return Json(new
            {
                success = true,

                cartCount = cartCount
            });
        }


        // ==========================================
        // 讀取 Cookie
        // ==========================================

        private List<CartItemViewModel> GetCart( )
        {
            var cookie =
                Request. Cookies[CartCookieName];


            if(string. IsNullOrEmpty(cookie))
            {
                return new List<CartItemViewModel>( );
            }


            try
            {
                return JsonSerializer. Deserialize<
                    List<CartItemViewModel>
                >(cookie)
                ?? new List<CartItemViewModel>( );
            }
            catch
            {
                return new List<CartItemViewModel>( );
            }
        }


        // ==========================================
        // 儲存 Cookie
        // ==========================================

        private void SaveCart(
            List<CartItemViewModel> cart )
        {
            var json =
                JsonSerializer. Serialize(cart);


            Response. Cookies. Append(
                CartCookieName,
                json,
                new CookieOptions
                {
                    HttpOnly = true,

                    IsEssential = true,

                    Expires =
                        DateTimeOffset. Now. AddDays(7)
                }
            );
        }
        // ==========================================
        // 從購物車刪除商品
        // ==========================================

        [HttpPost]
        public IActionResult RemoveFromCart(
            int productId,
            int specificationId )
        {
            // 取得目前購物車
            var cart = GetCart( );

            // 找到商品 + 規格
            var item = cart. FirstOrDefault(x =>
                x. ProductId == productId &&
                x. SpecificationId == specificationId
            );

            // 找不到商品
            if(item == null)
            {
                return Json(new
                {
                    success = false,
                    message = "找不到此商品"
                });
            }

            // 從購物車移除
            cart. Remove(item);

            // 儲存回 ShoppingCart Cookie
            SaveCart(cart);

            // 計算目前購物車總數量
            int cartCount =
                cart. Sum(x => x. Quantity);

            return Json(new
            {
                success = true,
                message = "商品已刪除",
                cartCount = cartCount
            });
        }


    }
}
