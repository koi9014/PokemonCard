using PokemonCard.Models;

namespace PokemonCard.ViewModels.Seller
{
    public class SellerStore
    {
        public PokemonCard.Models.Seller Seller { get; set; } = null!;
        public List<Product> Products { get; set; } = [];
        public List<string> Categories { get; set; } = [];
        public string? ProductType { get; set; }
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public List<SellerStoreReviewItem> Reviews { get; set; } = [];
    }

    public class SellerStoreReviewItem
    {
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StoreSettingInput
    {
        public bool IsCreate { get; set; }
        public string? StoreName { get; set; }
        public string? StoreDescription { get; set; }
        public IFormFile? Avatar { get; set; }
        public string? AvatarUrl { get; set; }
    }

}
