namespace PokemonCard. ViewModels
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }

        public int SpecificationId { get; set; }

        public int Quantity { get; set; }

        // 顯示用
        public string ProductName { get; set; } = "";

        public string? ProductImage { get; set; }

        public string SpecificationName { get; set; } = "";

        public int Price { get; set; }

        public int Stock { get; set; }

        public decimal SubTotal =>
            Price * Quantity;
    }
}
