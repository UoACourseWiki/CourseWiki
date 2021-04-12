using System;
using System.Linq;
using System.Threading.Tasks;
using CourseWiki.Data;
using CourseWiki.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CourseWiki.Controllers
{
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly ApiDbContext _context;
        // private readonly ILogger<CoursesController> _logger;

        public CoursesController(ApiDbContext context)
        {
            _context = context;
        }

        // public CoursesController(ILogger<CoursesController> logger)
        // {
        //     _logger = logger;
        // }

        [HttpGet("course/")]
        public async Task<ActionResult<Course>> GetCourse(Guid guid)
        {
            var courses = await _context.Courses.FindAsync(guid);

            if (courses == null)
            {
                return NotFound();
            }

            return courses;
        }

        [HttpGet("")]
        public async Task<ActionResult<Course>> SearchCourses(string? search, string? subject, string? catalogNbr,
            int? level)
        {
            var courses = await _context.Courses.Where(a => a.SearchVector.Matches(search) || search == null)
                .Where(a => a.Subject == subject || subject == null)
                .Where(a => a.CatalogNbr == catalogNbr || catalogNbr == null)
                .Where(a => (a.CatalogNbr.CompareTo((level * 100).ToString()) > 0 &&
                             a.CatalogNbr.CompareTo(((level + 1) * 100).ToString()) < 0) || level == null)
                .OrderBy(a => a.CatalogNbr)
                .ToArrayAsync();

            if (courses.Length == 0)
            {
                return NotFound();
            }

            return Ok(courses);
        }
    }
}