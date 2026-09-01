using Microsoft. AspNetCore. Authorization;
using Microsoft. AspNetCore. Mvc;
using Microsoft. EntityFrameworkCore;
using PokemonCard. Models;
using System. Security. Claims;

namespace PokemonCard. Controllers
{
    public class CheckoutController : Controller
    {
        private readonly PicartchuContext _context;

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

        public CheckoutController( PicartchuContext context )
        {
            _context = context;
        }


        // =====================================================
        // GET：結帳頁面
        // =====================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index(
            int productId,
            int specificationId,
            int quantity )
        {
            var product = await _context. Products
                . Include(p => p. ProductImages
                    . OrderBy(x => x. ImageOrder))
                . Include(p => p. ProductSpecs)
                . Include(p => p. Seller)
                    . ThenInclude(s => s. User)
                . FirstOrDefaultAsync(p =>
                    p. ProductId == productId);

            if(product == null)
            {
                return NotFound( );
            }


            var spec = product. ProductSpecs
                . FirstOrDefault(x =>
                    x. SpecificationId == specificationId);

            if(spec == null)
            {
                return NotFound( );
            }


            // 數量不能小於 1
            if(quantity < 1)
            {
                quantity = 1;
            }


            ViewBag. Spec = spec;
            ViewBag. Quantity = quantity;


            return View(product);
        }



        // =====================================================
        // POST：建立訂單
        // =====================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(
            int productId,
            int specificationId,
            int quantity,
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

            // =================================================
            // 1. 取得目前登入會員的 UserId
            // =================================================

            var userIdString =
                User. FindFirstValue(ClaimTypes. NameIdentifier);


            if(string. IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "UserLogin");
            }


            int buyerId =
                int. Parse(userIdString);



            // =================================================
            // 2. 基本資料檢查
            // =================================================

            if(string. IsNullOrWhiteSpace(customerName))
            {
                return Content("請輸入姓名");
            }


            if(string. IsNullOrWhiteSpace(phone))
            {
                return Content("請輸入手機號碼");
            }


            if(string. IsNullOrWhiteSpace(email))
            {
                return Content("請輸入 Email");
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


            if(quantity < 1)
            {
                return Content("商品數量錯誤");
            }



            // =================================================
            // 3. 查詢商品
            // =================================================

            var product = await _context. Products
                . Include(p => p. ProductSpecs)
                . Include(p => p. Seller)
                . FirstOrDefaultAsync(p =>
                    p. ProductId == productId);


            if(product == null)
            {
                return NotFound("找不到商品");
            }



            // =================================================
            // 4. 查詢商品規格
            // =================================================

            var spec = product. ProductSpecs
                . FirstOrDefault(x =>
                    x. SpecificationId == specificationId);


            if(spec == null)
            {
                return NotFound("找不到商品規格");
            }



            // =================================================
            // 5. 取得賣家 UserId
            //
            // Product 沒有 SellerId
            // Product.UserId 就是賣家的 UserId
            // =================================================

            int sellerId = product. UserId;



            // =================================================
            // 6. 取得商品價格
            // =================================================

            int unitPrice = spec. SpecsPrice;


            // 商品小計
            int orderAmount =
                unitPrice * quantity;


            // 運費
            int shipAmount = 60;


            // 訂金
            int orderDeposit = 0;



            // =================================================
            // 7. 建立訂單編號
            // =================================================

            string orderNo = await GenerateOrderNo( );



            // =================================================
            // 8. 建立 Order
            // =================================================

            var order = new Order
            {
                OrderNo = orderNo,

                BuyerId = buyerId,

                SellerId = sellerId,

                OrderedAt = DateTime. Now,

                OrderDeposit = orderDeposit,

                OrderAmount = orderAmount,

                ShipAmount = shipAmount,

                OrderStatus = "Pending",

                ReceiverName = customerName,

                ReceiverPhone = phone,

                ShippingAddress =
                    $"{postalCode} {city}{district}{address}",

                OrderCreatedAt = DateTime. Now,

                OrderUpdatedAt = DateTime. Now
            };



            // =================================================
            // 9. 建立 OrderItem
            // =================================================

            var orderItem = new OrderItem
            {
                ProductId = product. ProductId,

                ProductName = product. ProductName,

                ProductSpec = spec. SpecsCategory1,

                ProductSpec2 = null,

                PreSale = false,

                Quantity = quantity,

                UnitPrice = unitPrice
            };


            // 建立 Order 與 OrderItem 關聯
            order. OrderItems. Add(orderItem);



            // =================================================
            // 10. 加入資料庫
            // =================================================

            _context. Orders. Add(order);


            // =================================================
            // 11. 儲存
            // =================================================

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



            // =================================================
            // 12. 成功
            // =================================================

            return RedirectToAction("Success", new { id = order. OrderId });

        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Success( int id )
        {
            var buyerId = int. Parse(
                User. FindFirstValue(ClaimTypes. NameIdentifier)
            );

            var order = await _context. Orders
                . Include(o => o. OrderItems)
                . FirstOrDefaultAsync(o =>
                    o. OrderId == id &&
                    o. BuyerId == buyerId
                );

            if(order == null)
            {
                return NotFound( );
            }

            return View(order);
        }
    }
}
