using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PokemonCard.ViewModels
{
    public class AddProductInput
    {
        // 編輯時使用的商品編號 (新增時為 0)
        public int ProductId { get; set; }

        [Required(ErrorMessage = "請輸入商品名稱。")]
        [StringLength(50, ErrorMessage = "商品名稱最多 50 字。")]
        public string? ProductName { get; set; }

        [StringLength(500, ErrorMessage = "商品描述最多 500 字。")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "請選擇地區。")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "請選擇商品分類。")]
        public string? ProductType { get; set; }

        // 預設為預購 ("preorder" / "instock")
        public string SaleType { get; set; } = "preorder";

        // 排程上架時間
        public DateTime? ScheduledAt { get; set; }

        // 預設狀態為上架 ("PUBLISHED" / "DRAFT")
        [Required(ErrorMessage = "請選擇商品狀態。")]
        public string ProductStatus { get; set; } = "PUBLISHED";

        // 新上傳的照片檔案
        public List<IFormFile>? Images { get; set; }

        // 已存在於資料庫的舊照片 URL 列表 (編輯時顯示用)
        public List<string> ExistingImages { get; set; } = new();

        // 編輯時勾選要刪除的舊照片 URL 列表
        public List<string> RemoveImageUrls { get; set; } = new();

        // 商品規格列表
        public List<ProductSpecInput> Specs { get; set; } = new();
    }

    /// <summary>
    /// 商品規格子表單 ViewModel
    /// </summary>
    public class ProductSpecInput
    {
        public string? Category { get; set; }

        [Required(ErrorMessage = "請填寫規格選項。")]
        public string? Option { get; set; }

        [Required(ErrorMessage = "請輸入價格。")]
        [Range(0, double.MaxValue, ErrorMessage = "價格不可為負數。")]
        public decimal? Price { get; set; }

        // 庫存設定為 int?
        [Required(ErrorMessage = "請輸入庫存。")]
        [Range(0, int.MaxValue, ErrorMessage = "庫存不可為負數。")]
        public int? Stock { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "訂金不可為負數。")]
        public decimal? Deposit { get; set; }
    }
}
