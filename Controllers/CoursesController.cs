using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseWiki.Misc;
using CourseWiki.Models;
using CourseWiki.Models.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace CourseWiki.Controllers
{
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

        [HttpGet("")]
        public async Task<ActionResult<List<CourseSearchByTxtResponse>>> SearchCourses(string? search, string? subject,
            string? catalogNbr,
            int? level)
        {
            var courses = await _context.Courses.Where(a => a.SearchVector.Matches(search) || search == null)
                .Where(a => a.Subject == subject || subject == null)
                .Where(a => a.CatalogNbr == catalogNbr || catalogNbr == null)
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

        [HttpGet("{subject}")]
        public async Task<ActionResult<List<Course>>> GetCoursesBySubject(string subject)
        {
            var courses = await _context.Courses.Where(a => a.Subject == subject).OrderBy(a => a.CatalogNbr)
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

        [HttpGet("{subject}/{catalogNbr}")]
        public async Task<ActionResult<Course>> GetCourseByCatalogNbr(string subject, string catalogNbr)
        {
            var course = await _context.Courses.Where(a => a.Subject == subject).Where(a => a.CatalogNbr == catalogNbr)
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

        [HttpGet("{subject}/{catalogNbr}/{term}")]
        public async Task<ActionResult<CourseInTerm>> GetCourseInTermByTerm(string subject, string catalogNbr,
            string term)
        {
            var course = await _context.Courses.Where(a => a.Subject == subject).Where(a => a.CatalogNbr == catalogNbr)
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

        [HttpGet("{subject}/{catalogNbr}/{term}/{section}")]
        public async Task<ActionResult<Cls>> GetClassBySection(string subject, string catalogNbr, string term,
            string section)
        {
            var course = await _context.Courses.Where(a => a.Subject == subject).Where(a => a.CatalogNbr == catalogNbr)
                .OrderBy(a => a.CatalogNbr).FirstOrDefaultAsync();
            var courseInTerm = await _context.CoursesInTerms.Where(b => b.CourseUUID == course.Id)
                .Where(b => b.Term == term).FirstOrDefaultAsync();
            var cls = await _context.Clses.Where(a => a.Cituuid == courseInTerm.Id)
                .Where(a => a.ClassSection == section).FirstOrDefaultAsync();
            if (cls == null)
            {
                return NotFound();
            }

            return Ok(cls);
        }
    }
}