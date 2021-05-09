using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CourseWiki.Misc;
using CourseWiki.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CourseWiki.Controllers
{
    /// <summary>
    /// Get Information for courses in each term.
    /// </summary>
    [Produces("application/json")]
    [ApiController]
    [Route("[controller]")]
    public class CourseInTermsController : ControllerBase
    {
        private readonly ApiDbContext _context;

        /// <summary>
        /// Load Dbcontext.
        /// </summary>
        /// <param name="context"></param>
        public CourseInTermsController(ApiDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get CourseInTerm by its uuid.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>a courseInTerm object</returns>
        /// <response code="200">Returns the courseInTerm object</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="404">Can't find the courseInTerm from the uuid in response.</response>
        [HttpGet("courseInTerm/")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
        public async Task<ActionResult<CourseInTerm>> GetCourseInTerm(Guid guid)
        {
            var courseInTerm = await _context.CoursesInTerms.FindAsync(guid);
            if (courseInTerm == null)
            {
                return NotFound();
            }

            courseInTerm.Sections = await _context.Clses.Where(cls => cls.Cituuid == courseInTerm.Id)
                .Select(clsSelected => clsSelected.ClassSection).ToListAsync();
            return Ok(courseInTerm);
        }
    }
}