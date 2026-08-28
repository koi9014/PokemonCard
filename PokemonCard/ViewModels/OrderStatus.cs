namespace PokemonCard. Helpers
{
    public static class OrderStatus
    {
        public static string GetText( string status )
        {
            return status switch
            {
                "PENDING" => "待付款",
                "PAID" => "已付款",
                "Processing" => "待出貨",
                "SHIPPED" => "運送中",
                "COMPLETED" => "已完成",
                _ => "未知狀態"
            };
        }
    }
}
