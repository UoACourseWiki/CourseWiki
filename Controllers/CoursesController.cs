using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseWiki.Data;
using CourseWiki.Models;
using CourseWiki.Models.DTOs.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
            var course = await _context.Courses.FindAsync(guid);

            if (course == null)
            {
                return NotFound();
            }

            course.CitUUIDs = await _context.CoursesInTerms
                .Where(courseInTerm => courseInTerm.CourseUUID == course.Id)
                .Select(courseInTermSelected => courseInTermSelected.Id).ToListAsync();
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
    }
}