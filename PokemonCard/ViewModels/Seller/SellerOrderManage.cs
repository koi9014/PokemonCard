
namespace PokemonCard.ViewModels.Seller
{
    public class SellerOrderManage : PagedViewModel<SellerOrderListItem>
    {
        public string? Search { get; set; }

        public string? Status { get; set; }

        public int ShipAmount { get; set; }
    }
    public class SellerOrderListItem
    {
        public int OrderId { get; set; }

        public string OrderNo { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public int ShipAmount { get; set; }

        public int OrderAmount { get; set; }

        public string OrderStatus { get; set; } = string.Empty;

        public DateTime OrderedAt { get; set; }
    }
}
