using Microsoft. AspNetCore. Mvc;
using Microsoft. EntityFrameworkCore;
using PokemonCard. Models;
using static Microsoft. Extensions. Logging. EventSource. LoggingEventSource;

namespace PokemonCard. Controllers
{
    public class ProductController : Controller
    {
        private readonly PicartchuContext _context;
        public ProductController( PicartchuContext context )
        {
            _context = context;
        }
        public async Task<IActionResult> Details( int id )
        {
            var data = await _context. Products
                . Include(p => p. ProductImages
                    . OrderBy(x => x. ImageOrder))
                . Include(p => p. ProductSpecs)
        . Include(p => p. Seller)
            . ThenInclude(s => s. User)
                . FirstOrDefaultAsync(p => p. ProductId == id);

            return View(data);
        }
    }
}
