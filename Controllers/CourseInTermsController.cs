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
    public class CourseInTermsController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public CourseInTermsController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet("courseInTerm/")]
        public async Task<ActionResult<CourseInTerm>> GetCourseInTerm(Guid guid)
        {
            var courseInTerm = await _context.CoursesInTerms.FindAsync(guid);
            if (courseInTerm == null)
            {
                return NotFound();
            }

            courseInTerm.ClassUUIDs = await _context.Clses.Where(cls => cls.Cituuid == courseInTerm.Id)
                .Select(clsSelected => clsSelected.Id).ToListAsync();
            return Ok(courseInTerm);
        }
    }
}