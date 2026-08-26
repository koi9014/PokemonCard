using Microsoft. AspNetCore. Mvc;
using PokemonCard. Models;

namespace PokemonCard. Controllers
{
    public class MyAccountController: Controller
    {
        private readonly PicartchuContext _context;
        public MyAccountController( PicartchuContext context )
        {
            _context = context;
        }
        public IActionResult MemberCenter( )
        {

            var product = _context. Products. FirstOrDefault( );
            var StoreName = _context. Sellers. FirstOrDefault( );
            var image = _context. ProductImages. FirstOrDefault( );

            ViewBag. Product = product;
            ViewBag. StoreName = StoreName;
            ViewBag. image = image;


            return View();
        }
        public IActionResult OrderDetail( )
        {
            return View( );
        }
    }
}
