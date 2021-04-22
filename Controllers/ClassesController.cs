using System;
using System.Linq;
using System.Threading.Tasks;
using CourseWiki.Data;
using CourseWiki.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseWiki.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClassesController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public ClassesController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet("class/")]
        public async Task<ActionResult<Cls>> GetClass(Guid guid)
        {
            var cls = await _context.Clses.FindAsync(guid);
            if (cls == null)
            {
                return NotFound();
            }

            return Ok(cls);
        }
    }
}