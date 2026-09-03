using PokemonCard. Models;

namespace PokemonCard. ViewModels
{
    public class MemberCenterViewModel
    {
        public int UserId { get; set; }
        public bool IsSeller { get; set; }
        public bool CanCreateStore { get; set; }
        public List<Order> Orders { get; set; } = new List<Order>( );

    }
}
