using System;
using System.Linq;
using System.Threading.Tasks;
using CourseWiki.Misc;
using CourseWiki.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseWiki.Controllers
{
    /// <summary>
    /// Get Information for classes.
    /// </summary>
    [Produces("application/json")]
    [ApiController]
    [Route("[controller]")]
    public class ClassesController : ControllerBase
    {
        private readonly ApiDbContext _context;

        /// <summary>
        /// Load Dbcontext.
        /// </summary>
        /// <param name="context"></param>
        public ClassesController(ApiDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get Class object by its uuid.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>a Class object</returns>
        /// <response code="200">Returns a class object</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="404">Can't find the class from the uuid in response.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
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