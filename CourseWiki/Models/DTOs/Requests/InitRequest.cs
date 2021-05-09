using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CourseWiki.Models.DTOs.Requests
{
    public class InitSubject
    {
        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("startYear")]
        public int StartYear { get; set; }

        [JsonPropertyName("endYear")]
        public int EndYear { get; set; }
    }

    public class InitRequest
    {
        [JsonPropertyName("initSubjects")]
        public List<InitSubject> InitSubjects { get; set; }
    }
}