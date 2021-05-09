using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CourseWiki.Controllers;
using CourseWiki.Misc;
using CourseWiki.Models;
using CourseWiki.Models.DTOs.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CourseWiki.Test
{
    public class InitControllerTest
    {
        [Fact]
        public async Task IsImportSucceed()
        {
            List<InitSubject> _initSubjects = new List<InitSubject>();
            _initSubjects.Add(new InitSubject() {Subject = "COMPSCI", StartYear = 2018, EndYear = 2021});
            _initSubjects.Add(new InitSubject() {Subject = "MATHS", StartYear = 2017, EndYear = 2021});
            InitRequest _initRequest = new InitRequest() {InitSubjects = _initSubjects};
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();

            var logger = factory.CreateLogger<InitController>();
            var _controller = new InitController(logger, configuration);
            var _actionResult = await _controller.InitDB(_initRequest);
            var result = _actionResult.Result as OkObjectResult;
            Assert.Equal("success!", result.Value);
        }

        [Fact]
        public async Task CourseInfoTest()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var connectionString = configuration.GetConnectionString("rmfDatabase");
            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>().UseNpgsql(connectionString);
            ApiDbContext apiDbContext = new ApiDbContext(optionsBuilder.Options);
            var _controller = new CoursesController(apiDbContext);
            var _actionResult = await _controller.GetCourseByCatalogNbr("COMPSCI", "732");
            var result = _actionResult.Result as OkObjectResult;
            Assert.IsType<Course>(result.Value);
        }
    }
}