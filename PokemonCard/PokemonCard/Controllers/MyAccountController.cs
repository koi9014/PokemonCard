using Microsoft. AspNetCore. Mvc;

namespace PokemonCard. Controllers
{
    public class MyAccountController: Controller
    {
        public IActionResult MemberCenter( )
        {
            return View( );
        }
    }
}
