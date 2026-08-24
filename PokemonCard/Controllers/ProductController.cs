using Microsoft. AspNetCore. Mvc;

namespace PokemonCard. Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index( )
        {
            return View( );
        }
    }
}
