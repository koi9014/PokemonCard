namespace PokemonCard. Helpers
{
    public static class OrderStatus
    {
        public static string GetText( string status )
        {
            return status switch
            {
                "Pending" => "待付款",
                "Paid" => "已付款",
                "Processing" => "待出貨",
                "Shipped" => "運送中",
                "Completed" => "已完成",
                _ => "未知狀態"
            };
        }
    }
}
