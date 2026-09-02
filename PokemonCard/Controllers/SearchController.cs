using Microsoft. AspNetCore. Mvc;
using Microsoft. EntityFrameworkCore;
using PokemonCard. Models;

namespace PokemonCard. Controllers
{
    public class SearchController : Controller
    {

        private readonly PicartchuContext _context;
        public SearchController( PicartchuContext context )
        {
            _context = context;
        }
        public IActionResult Index( )
        {
            var data = _context. Products
                . Include(p => p. ProductImages)
                . Include(p => p. ProductSpecs)
                . ToList( );

            return View(data);
        }


        [HttpPost]
        public async Task<IActionResult> Index( string? Keyword )
        {
            var data = await _context. Products
                . Include(p => p. ProductImages)
                . Include(p => p. ProductSpecs)
                . Where(p => p. ProductStatus == "PUBLISHED")
                . Where(p => string. IsNullOrEmpty(Keyword) || p. ProductName. Contains(Keyword))
                . ToListAsync( );

            return View(data);
        }
    }
}
