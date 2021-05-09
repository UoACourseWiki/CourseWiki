using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseWiki.Misc;
using CourseWiki.Models;
using CourseWiki.Models.DTOs.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace CourseWiki.Controllers
{
    /// <summary>
    /// APIs for courses information.
    /// </summary>
    [Produces("application/json")]
    [ApiController]
    [Route("[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly ApiDbContext _context;
        // private readonly ILogger<CoursesController> _logger;

        /// <summary>
        /// Load Dbcontext.
        /// </summary>
        /// <param name="context"></param>
        public CoursesController(ApiDbContext context)
        {
            _context = context;
        }

        // public CoursesController(ILogger<CoursesController> logger)
        // {
        //     _logger = logger;
        // }

        /// <summary>
        /// Get Course object by its uuid.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>a Course object</returns>
        /// <response code="200">Returns a course object</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="404">Can't find the class from the uuid in response.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
        [HttpGet("course/")]
        public async Task<ActionResult<Course>> GetCourse(Guid guid)
        {
            var course = await _context.Courses.FindAsync(guid);

            if (course == null)
            {
                return NotFound();
            }

            course.Terms = await _context.CoursesInTerms
                .Where(courseInTerm => courseInTerm.CourseUUID == course.Id)
                .Select(courseInTermSelected => courseInTermSelected.Term).ToListAsync();
            return Ok(course);
        }

        /// <summary>
        /// Find courses by keyword search, subject, course code and stages of courses.
        /// </summary>
        /// <param name="search"></param>
        /// <param name="subject"></param>
        /// <param name="catalogNbr"></param>
        /// <param name="level"></param>
        /// <returns>a List of Course object</returns>
        /// <response code="200">Returns a list of course object according to parameters</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="404">Can't find the courses according parameters in response.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
        [HttpGet("")]
        public async Task<ActionResult<List<CourseSearchByTxtResponse>>> SearchCourses(string? search, string? subject,
            string? catalogNbr,
            int? level)
        {
            var courses = await _context.Courses.Where(a => a.SearchVector.Matches(search) || search == null)
                .Where(a => EF.Functions.ILike(a.Subject, subject) || subject == null)
                .Where(a => EF.Functions.ILike(a.CatalogNbr, catalogNbr) || catalogNbr == null)
                .Where(a => (a.CatalogNbr.CompareTo((level * 100).ToString()) > 0 &&
                             a.CatalogNbr.CompareTo(((level + 1) * 100).ToString()) < 0) || level == null)
                .OrderBy(a => a.CatalogNbr)
                .ToListAsync();

            if (courses.Count == 0)
            {
                return NotFound();
            }

            var coursesNoCit = (from course in courses
                select new CourseSearchByTxtResponse()
                {
                    Id = course.Id, Subject = course.Subject, CatalogNbr = course.CatalogNbr,
                    Description = course.Description, Title = course.Title
                }).ToList();
            return Ok(coursesNoCit);
        }

        /// <summary>
        /// Find courses by subject.
        /// </summary>
        /// <param name="subject"></param>
        /// <returns>a List of Course objects</returns>
        /// <response code="200">Returns a list of course objects according to subject</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="404">Can't find the courses according to subject in response.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
        [HttpGet("{subject}")]
        public async Task<ActionResult<List<Course>>> GetCoursesBySubject(string subject)
        {
            var courses = await _context.Courses.Where(a => EF.Functions.ILike(a.Subject, subject))
                .OrderBy(a => a.CatalogNbr)
                .ToListAsync();
            if (courses.Count == 0)
            {
                return NotFound();
            }

            foreach (var course in courses)
            {
                course.Terms = await _context.CoursesInTerms
                    .Where(courseInTerm => courseInTerm.CourseUUID == course.Id)
                    .Select(courseInTermSelected => courseInTermSelected.Term).ToListAsync();
            }

            return Ok(courses);
        }

        /// <summary>
        /// Find courses by subject and course code.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="catalogNbr"></param>
        /// <returns>a List of Course objects</returns>
        /// <response code="200">Returns a list of course objects according to subject and course code.</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="404">Can't find the courses according to subject and course code in response.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
        [HttpGet("{subject}/{catalogNbr}")]
        public async Task<ActionResult<Course>> GetCourseByCatalogNbr(string subject, string catalogNbr)
        {
            var course = await _context.Courses.Where(a => EF.Functions.ILike(a.Subject, subject))
                .Where(a => EF.Functions.ILike(a.CatalogNbr, catalogNbr))
                .OrderBy(a => a.CatalogNbr).FirstOrDefaultAsync();
            if (course == null)
            {
                return NotFound();
            }

            course.Terms = await _context.CoursesInTerms
                .Where(courseInTerm => courseInTerm.CourseUUID == course.Id).OrderBy(courseInTerm => courseInTerm.Term)
                .Select(courseInTermSelected => courseInTermSelected.Term).ToListAsync();

            return Ok(course);
        }

        /// <summary>
        /// Find information of course in term by subject, course code and term code.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="catalogNbr"></param>
        /// <param name="term"></param>
        /// <returns>a List of CourseInTerm objects</returns>
        /// <response code="200">Returns a list of CourseInTerm objects according to subject</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="404">Can't find information of course in term according to parameters in response.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
        [HttpGet("{subject}/{catalogNbr}/{term}")]
        public async Task<ActionResult<CourseInTerm>> GetCourseInTermByTerm(string subject, string catalogNbr,
            string term)
        {
            var course = await _context.Courses.Where(a => EF.Functions.ILike(a.Subject, subject))
                .Where(a => EF.Functions.ILike(a.CatalogNbr, catalogNbr))
                .OrderBy(a => a.CatalogNbr).FirstOrDefaultAsync();
            var courseInTerm = await _context.CoursesInTerms.Where(b => b.CourseUUID == course.Id)
                .Where(b => b.Term == term).FirstOrDefaultAsync();
            if (courseInTerm == null)
            {
                return NotFound();
            }

            courseInTerm.Sections = await _context.Clses.Where(a => a.Cituuid == courseInTerm.Id)
                .OrderBy(a => a.ClassSection)
                .Select(b => b.ClassSection).ToListAsync();
            return Ok(courseInTerm);
        }

        /// <summary>
        /// Find class information by subject, course code, term code and section.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="catalogNbr"></param>
        /// <param name="term"></param>
        /// <param name="section"></param>
        /// <returns>a List of CourseInTerm objects</returns>
        /// <response code="200">Returns a list of class objects according to subject</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="404">Can't find information of class according to parameters in response.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
        [HttpGet("{subject}/{catalogNbr}/{term}/{section}")]
        public async Task<ActionResult<Cls>> GetClassBySection(string subject, string catalogNbr, string term,
            string section)
        {
            var course = await _context.Courses.Where(a => EF.Functions.ILike(a.Subject, subject))
                .Where(a => EF.Functions.ILike(a.CatalogNbr, catalogNbr))
                .OrderBy(a => a.CatalogNbr).FirstOrDefaultAsync();
            var courseInTerm = await _context.CoursesInTerms.Where(b => b.CourseUUID == course.Id)
                .Where(b => b.Term == term).FirstOrDefaultAsync();
            var cls = await _context.Clses.Where(a => a.Cituuid == courseInTerm.Id)
                .Where(a => EF.Functions.ILike(a.ClassSection, section)).FirstOrDefaultAsync();
            if (cls == null)
            {
                return NotFound();
            }

            return Ok(cls);
        }
    }
}