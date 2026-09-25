namespace PokemonCard.ViewModels.Seller
{
    public class ToBuyList{
        public List<ToBuyListGroup> Groups { get; set; } = [];
        public List<ToBuyOrderProgress> Orders { get; set; } = [];
        public int TotalQuantity => Groups.Sum(group => group.TotalQuantity);
        public int TotalOrderCount => Groups.SelectMany(group => group.OrderIds).Distinct().Count();
    }

    public class ToBuyListGroup
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSpec { get; set; }
        public string? ProductSpec2 { get; set; }
        public string? ImageUrl { get; set; }
        public int TotalQuantity { get; set; }
        public List<int> OrderItemIds { get; set; } = [];
        public List<ToBuyPurchaseItem> Items { get; set; } = [];
        public List<int> OrderIds { get; set; } = [];
        public List<string> OrderNumbers { get; set; } = [];
    }

    public class ToBuyOrderProgress
    {
        public int OrderId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public DateTime OrderedAt { get; set; }
        public List<ToBuyPurchaseItem> Items { get; set; } = [];
    }

    public class ToBuyPurchaseItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int Quantity { get; set; }
    }
}
