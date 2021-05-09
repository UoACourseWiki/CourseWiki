using CourseWiki.Models.DTOs.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CourseWiki.Controllers
{
    /// <summary>
    /// Api for fetch course and class information from UoA API.
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class InitController : ControllerBase
    {
        private readonly ILogger<InitController> _logger;
        private readonly IConfiguration m_config;

        /// <summary>
        /// load config and logger settings.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        public InitController(ILogger<InitController> logger, IConfiguration config)
        {
            _logger = logger;
            m_config = config;
        }

        /// <summary>
        /// Fetch course and class information from UoA API
        /// </summary>
        /// <param name="initRequest"></param>
        /// <returns>Import result</returns>
        /// <response code="200">Import Succeed.</response>
        /// <response code="400">Request is invalid.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [HttpPost("courseInit")]
        public async Task<ActionResult<InitRequest>> InitDB(InitRequest initRequest)
        {
            string connectionString = m_config.GetConnectionString("rmfDatabase");
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            foreach (var initSubject in initRequest.InitSubjects)
            {
                for (int i = initSubject.StartYear; i <= initSubject.EndYear; i++)
                {
                    /* UoA 4 digit term code: first digit must be 1, second and third digit for year, e.g. 2020 become 20; 2018 become 18,
                     * the last digit for sememster, 0 for summer school, 3 for semester 1, 5 for semester 2.
                     * overall example: term code 1193 is semester 1, 2019; 1210 is summer school, 2021. */
                    var termCodes = new[]
                    {
                        Convert.ToInt32("1" + i.ToString().Substring(2, 2) + "0"),
                        Convert.ToInt32("1" + i.ToString().Substring(2, 2) + "3"),
                        Convert.ToInt32("1" + i.ToString().Substring(2, 2) + "5")
                    };
                    HttpClient httpclient = new HttpClient();
                    HttpResponseMessage coursesResponse = await httpclient
                        .GetAsync(
                            $"https://api.auckland.ac.nz/service/courses/v2/courses?subject={initSubject.Subject}&year={i}&size=500"); // Fetch course information from UoA courses API.
                    coursesResponse.EnsureSuccessStatusCode();
                    dynamic coursesResponseBody =
                        JObject.Parse(await coursesResponse.Content.ReadAsStringAsync()); // Response string to JSON.
                    await using var coursesSqlCmd = new NpgsqlCommand(
                        "INSERT INTO \"Courses\" (\"Id\",\"CrseId\",\"CatalogNbr\",\"Description\",\"Subject\", \"Title\", \"RqrmntDescr\") SELECT gen_random_uuid(), \"crseId\", \"catalogNbr\", \"description\", subject, title,\"rqrmntDescr\" FROM json_to_recordset(@courses::json) x (\"crseId\" text, \"catalogNbr\" text, \"description\" text, subject text, title text,\"rqrmntDescr\" text) ON CONFLICT(\"CrseId\") DO NOTHING;; ",
                        conn); //Insert to database with information we need.
                    coursesSqlCmd.Parameters.AddWithValue("@courses",
                        coursesResponseBody["data"].ToString()); // Get rid of "total" in response.
                    await coursesSqlCmd.ExecuteNonQueryAsync();
                    Parallel.ForEach(termCodes, async (termcode) =>
                    {
                        await using (var conn = new NpgsqlConnection(connectionString))
                        {
                            await conn.OpenAsync();
                            HttpResponseMessage classesResponse = await httpclient
                                .GetAsync(
                                    $"https://api.auckland.ac.nz/service/classes/v1/classes?term={termcode}&subject={initSubject.Subject}&size=500"); // Fetch class information from UoA classes API.
                            classesResponse.EnsureSuccessStatusCode();
                            dynamic classesResponseBody =
                                JObject.Parse(await classesResponse.Content
                                    .ReadAsStringAsync()); // Response string to JSON.
                            await using (var classesSqlCmd = new NpgsqlCommand(
                                "WITH citId AS(INSERT INTO \"CoursesInTerms\" (\"Id\", \"CrseId\", \"Term\", \"CourseUUID\")"
                                + "SELECT gen_random_uuid(), \"crseId\", \"term\", (SELECT \"Id\" FROM \"Courses\" WHERE \"CrseId\"=\"crseId\")"
                                + "FROM json_to_recordset(@cls::json) x (\"crseId\" text, \"term\" text)"
                                + "ON CONFLICT ON CONSTRAINT \"AK_CoursesInTerms_CrseId_Term\" DO NOTHING RETURNING \"Id\" as \"classId\", \"CrseId\" as \"CrId\", \"Term\" as \"classTerm\")"
                                + "INSERT INTO \"Clses\" (\"Id\", \"CrseId\", \"Term\", \"ClassSection\", \"Component\", \"Consent\", \"DropConsent\", \"Cituuid\",\"MeetingPatterns\")"
                                + "SELECT gen_random_uuid(),\"crseId\",term,\"classSection\",\"component\",consent,\"dropConsent\","
                                + "(SELECT \"classId\" FROM citId WHERE \"CrId\"=\"crseId\" and \"classTerm\"=term),\"meetingPatterns\""
                                + "FROM json_to_recordset(@cls::json)"
                                + "x (\"crseId\" text, \"term\" text, \"classSection\" text, \"component\" text, \"consent\" text, \"dropConsent\" text,\"meetingPatterns\" json)"
                                + "ON CONFLICT ON CONSTRAINT \"AK_Clses_CrseId_Term_ClassSection\" DO NOTHING;;",
                                conn))
                            {
                                classesSqlCmd.Parameters.AddWithValue("@cls",
                                    classesResponseBody["data"].ToString()); // Get rid of "total" in response.
                                await classesSqlCmd.ExecuteNonQueryAsync();
                            }
                        }
                    });
                }
            }

            return Content("success!");
        }
    }
}