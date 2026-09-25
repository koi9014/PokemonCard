
namespace PokemonCard.ViewModels.Seller
{
    public class ProductManage : PagedViewModel<ProductListItem>
    {
        public string? ProductType { get; set; }

        public string? Status { get; set; }

        public string? Search { get; set; }
    }
    public class ProductListItem
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string ProductType { get; set; } = string.Empty;

        public string ProductStatus { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool PreSale { get; set; }

        public int Price { get; set; }

        public int Stock { get; set; }

        public int Sales { get; set; }

        public bool IsScheduled { get; set; }
    }
}
