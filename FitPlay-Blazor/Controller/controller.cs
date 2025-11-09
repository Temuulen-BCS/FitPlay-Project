using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitPlay-Blazor.Server.Data;
using FitPlay-Blazor.Shared;

namespace FitPlay-Blazor.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProdutosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IEnumerable<ApplicationDbContext>> Get()
        {
            return await _context.Produtos.ToListAsync();
        }
    }
}
