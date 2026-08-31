using Microsoft. AspNetCore. Mvc;
using Microsoft. EntityFrameworkCore;
using PokemonCard. Models;

namespace PokemonCard. Controllers
{
    public class CheckoutController : Controller
    {
        private readonly PicartchuContext _context;

        public CheckoutController( PicartchuContext context )
        {
            _context = context;
        }
        public async Task<IActionResult> Index(
            int productId,
            int specificationId,
            int quantity )
        {
            var product = await _context. Products
                . Include(p => p. ProductImages
                    . OrderBy(x => x. ImageOrder))
                . Include(p => p. ProductSpecs)
                . Include(p => p. Seller)
                    . ThenInclude(s => s. User)
                . FirstOrDefaultAsync(p => p. ProductId == productId);

            if(product == null)
            {
                return NotFound( );
            }

            var spec = product. ProductSpecs
                . FirstOrDefault(x => x. SpecificationId == specificationId);

            if(spec == null)
            {
                return NotFound( );
            }

            ViewBag. Spec = spec;
            ViewBag. Quantity = quantity;

            return View(product);
        }


    }
}
