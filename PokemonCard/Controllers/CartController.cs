using Microsoft. AspNetCore. Authorization;
using Microsoft. AspNetCore. Mvc;
using Microsoft. EntityFrameworkCore;
using PokemonCard. Models;
using PokemonCard. ViewModels;
using System. Security. Claims;
using System. Text. Json;

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
        public async Task<IActionResult> Add(
    int productId,
    int specificationId,
    int quantity = 1 )
        {
            // 數量防呆
            if(quantity < 1)
            {
                quantity = 1;
            }

            // ==========================================
            // 1. 查詢商品
            // ==========================================

            var product = await _context. Products
                . FirstOrDefaultAsync(p =>
                    p. ProductId == productId);

            if(product == null)
            {
                return Json(new
                {
                    success = false,
                    message = "找不到商品"
                });
            }


            // ==========================================
            // 2. 取得目前購物車
            // ==========================================

            var cart = GetCart( );


            // ==========================================
            // 3. 檢查購物車是否已有商品
            // ==========================================

            if(cart. Any( ))
            {
                // 取得購物車第一個商品
                var firstCartItem = cart. First( );

                var firstProduct = await _context. Products
                    . FirstOrDefaultAsync(p =>
                        p. ProductId == firstCartItem. ProductId);

                if(firstProduct == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "購物車商品不存在"
                    });
                }


                // ==========================================
                // 4. 檢查是否為同一個賣家
                // ==========================================

                if(firstProduct. UserId != product. UserId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "購物車只能放置同一位賣家的商品"
                    });
                }
            }


            // ==========================================
            // 5. 找相同商品 + 相同規格
            // ==========================================

            var item = cart. FirstOrDefault(x =>
                x. ProductId == productId &&
                x. SpecificationId == specificationId
            );


            if(item == null)
            {
                // 新商品
                cart. Add(new CartItemViewModel
                {
                    ProductId = productId,
                    SpecificationId = specificationId,
                    Quantity = quantity
                });
            }
            else
            {
                // 已存在 → 增加數量
                item. Quantity += quantity;
            }


            // ==========================================
            // 6. 儲存購物車
            // ==========================================

            SaveCart(cart);


            // ==========================================
            // 7. 計算購物車總數量
            // ==========================================

            int cartCount =
                cart. Sum(x => x. Quantity);


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
        public async Task<IActionResult> Test( )
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

                if(product == null)
                {
                    continue;
                }

                var specification =
                    product. ProductSpecs
                        . FirstOrDefault(s =>
                            s. SpecificationId ==
                            item. SpecificationId);

                if(specification == null)
                {
                    continue;
                }

                var image =
                    product. ProductImages
                        . OrderBy(i => i. ImageOrder)
                        . FirstOrDefault( );

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


            // ==========================================
            // 計算購物車商品小計
            // ==========================================

            int cartSubtotal = 0;

            foreach(var item in viewModel)
            {
                cartSubtotal +=
                    item. Price * item. Quantity;
            }


            // ==========================================
            // 運費
            // ==========================================

            int shipAmount = 60;


            // ==========================================
            // 優惠折扣
            // ==========================================

            int discount = 0;


            // ==========================================
            // 應付總額
            // ==========================================

            int cartTotal =
                cartSubtotal +
                shipAmount -
                discount;


            // ==========================================
            // 傳給 View
            // ==========================================

            ViewBag. CartSubtotal = cartSubtotal;

            ViewBag. ShipAmount = shipAmount;

            ViewBag. Discount = discount;

            ViewBag. CartTotal = cartTotal;


            return View(viewModel);
        }


        private async Task<string> GenerateOrderNo( )
        {
            string today = DateTime. Now. ToString("yyyyMMdd");

            string prefix = "PK" + today;

            var lastOrder = await _context. Orders
                . Where(o => o. OrderNo. StartsWith(prefix))
                . OrderByDescending(o => o. OrderNo)
                . FirstOrDefaultAsync( );

            int nextNumber = 1;

            if(lastOrder != null)
            {
                string lastNumber =
                    lastOrder. OrderNo. Substring(prefix. Length);

                if(int. TryParse(lastNumber, out int number))
                {
                    nextNumber = number + 1;
                }
            }

            return prefix + nextNumber. ToString("D4");
        }

        // =====================================================
        // POST：購物車建立訂單
        // =====================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCartOrder(
    string customerName,
    string phone,
    string email,
    string shippingMethod,
    string postalCode,
    string city,
    string district,
    string address,
    string paymentMethod,
    string note )
        {
            // ==========================================
            // 1. 取得登入會員
            // ==========================================

            var userIdString =
                User. FindFirstValue(ClaimTypes. NameIdentifier);

            if(string. IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "UserLogin");
            }

            int buyerId =
                int. Parse(userIdString);


            // ==========================================
            // 2. 基本資料檢查
            // ==========================================

            if(string. IsNullOrWhiteSpace(customerName))
            {
                return Content("請輸入姓名");
            }

            if(string. IsNullOrWhiteSpace(phone))
            {
                return Content("請輸入手機號碼");
            }


            if(string. IsNullOrWhiteSpace(city))
            {
                return Content("請選擇縣市");
            }

            if(string. IsNullOrWhiteSpace(district))
            {
                return Content("請選擇區域");
            }

            if(string. IsNullOrWhiteSpace(address))
            {
                return Content("請輸入地址");
            }


            // ==========================================
            // 3. 取得購物車
            // ==========================================

            var cart = GetCart( );

            if(cart == null || !cart. Any( ))
            {
                return Content("購物車是空的");
            }


            // ==========================================
            // 4. 取得購物車所有商品
            // ==========================================

            var productIds =
                cart. Select(x => x. ProductId)
                    . Distinct( )
                    . ToList( );


            var products =
                await _context. Products
                    . Include(p => p. ProductSpecs)
                    . Where(p =>
                        productIds. Contains(p. ProductId))
                    . ToListAsync( );


            if(products. Count != productIds. Count)
            {
                return Content("購物車中有商品不存在");
            }


            // ==========================================
            // 5. 確認全部商品都是同一個賣家
            // ==========================================

            var sellerIds =
                products
                    . Select(p => p. UserId)
                    . Distinct( )
                    . ToList( );


            if(sellerIds. Count != 1)
            {
                return Content(
                    "購物車商品必須來自同一位賣家"
                );
            }


            int sellerId =
                sellerIds. First( );


            // ==========================================
            // 6. 建立訂單編號
            // ==========================================

            string orderNo =
                await GenerateOrderNo( );


            // ==========================================
            // 7. 計算商品金額
            // ==========================================

            int orderAmount = 0;


            foreach(var cartItem in cart)
            {
                var product =
                    products. FirstOrDefault(p =>
                        p. ProductId ==
                        cartItem. ProductId);


                if(product == null)
                {
                    return Content("找不到商品");
                }


                var spec =
                    product. ProductSpecs
                        . FirstOrDefault(s =>
                            s. SpecificationId ==
                            cartItem. SpecificationId);


                if(spec == null)
                {
                    return Content(
                        $"找不到商品規格：{product. ProductName}"
                    );
                }


                // ==========================================
                // 檢查庫存
                // ==========================================

                if(cartItem. Quantity > spec. Stock)
                {
                    return Content(
                        $"{product. ProductName} 庫存不足"
                    );
                }


                // ==========================================
                // 小計
                // ==========================================

                orderAmount +=
                    spec. SpecsPrice *
                    cartItem. Quantity;
            }


            // ==========================================
            // 8. 運費
            // ==========================================

            int shipAmount = 60;


            // ==========================================
            // 9. 訂金
            // ==========================================

            int orderDeposit = 0;


            // ==========================================
            // 10. 建立 Order
            // ==========================================

            var order = new Order
            {
                OrderNo = orderNo,

                BuyerId = buyerId,

                SellerId = sellerId,

                OrderedAt = DateTime. Now,

                OrderDeposit = orderDeposit,

                OrderAmount = orderAmount,

                ShipAmount = shipAmount,

                OrderStatus = "PAID",

                ReceiverName = customerName,

                ReceiverPhone = phone,

                ShippingAddress =
                    $"{postalCode} {city}{district}{address}",

                OrderCreatedAt = DateTime. Now,

                OrderUpdatedAt = DateTime. Now
            };


            // ==========================================
            // 11. 建立多筆 OrderItem
            // ==========================================

            foreach(var cartItem in cart)
            {
                var product =
                    products. First(p =>
                        p. ProductId ==
                        cartItem. ProductId);


                var spec =
                    product. ProductSpecs
                        . First(s =>
                            s. SpecificationId ==
                            cartItem. SpecificationId);


                var orderItem = new OrderItem
                {
                    ProductId =
                        product. ProductId,

                    ProductName =
                        product. ProductName,

                    ProductSpec =
                        spec. SpecsCategory1,

                    ProductSpec2 = null,

                    PreSale = false,

                    Quantity =
                        cartItem. Quantity,

                    UnitPrice =
                        spec. SpecsPrice
                };


                order. OrderItems. Add(orderItem);
            }

            order.OrderHistories.Add(new OrderHistory
            {
                OrderNo = orderNo,
                OrderStatus = "PAID",
                ChangeTime = DateTime.Now,
                ChangeReason = "訂單成立，預扣額度成功",
                ChangedByUserId = buyerId
            });


            // ==========================================
            // 12. 加入資料庫
            // ==========================================

            _context. Orders. Add(order);


            // ==========================================
            // 13. 儲存
            // ==========================================

            try
            {
                await _context. SaveChangesAsync( );
            }
            catch(Exception ex)
            {
                return Content(
                    "建立訂單失敗：" +
                    "<br><br>" +
                    ex. Message +
                    "<br><br>" +
                    ex. InnerException?.Message
                );
            }


            // ==========================================
            // 14. 清空購物車
            // ==========================================

            Response. Cookies. Delete(
                CartCookieName
            );


            // ==========================================
            // 15. 前往成功頁面
            // ==========================================

            return RedirectToAction(
                "Success",
                new
                {
                    id = order. OrderId
                }
            );
        }
        // =====================================================
        // 訂單完成頁面
        // =====================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Success( int id )
        {
            // 取得目前登入會員
            var userIdString =
                User. FindFirstValue(ClaimTypes. NameIdentifier);

            if(string. IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "UserLogin");
            }

            int buyerId = int. Parse(userIdString);


            // 查詢這一筆訂單
            var order = await _context. Orders
                . Include(o => o. OrderItems)
                . FirstOrDefaultAsync(o =>
                    o. OrderId == id &&
                    o. BuyerId == buyerId
                );


            // 找不到訂單
            if(order == null)
            {
                return NotFound( );
            }


            // 使用 Checkout 原本的成功頁面
            return View("~/Views/Cart/Success.cshtml", order);
        }



    }
}
