using Microsoft. AspNetCore. Mvc;

namespace PokemonCard. Controllers
{
    public class CheckoutController : Controller
    {
        public IActionResult Index( )
        {
            return View( );
        }
    }
}
