using Microsoft.AspNetCore.Mvc;
using oyasumi.Database;

namespace oyasumi.API.Controllers
{
    // Uncatigorized stuff
    public class GeneralController : Controller
    {
        private readonly OyasumiDbContext _context;

        public GeneralController(OyasumiDbContext context) =>
            _context = context;
        
    }
}