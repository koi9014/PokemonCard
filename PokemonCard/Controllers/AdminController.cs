using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;

namespace PokemonCard.Controllers
{
    public class AdminController : Controller
    {
        private readonly PicartchuContext _context;


        public AdminController(PicartchuContext context)
        {
            _context = context;
        }



        public IActionResult AdminCenter()
        {
            return View();
        }
    }
}
