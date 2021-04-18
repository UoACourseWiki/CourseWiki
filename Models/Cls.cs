using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CourseWiki.Models
{
    public class Cls
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("crseId")] public string CrseId { get; set; }
        [JsonPropertyName("term")] public string Term { get; set; }
        [JsonPropertyName("classSection")] public string ClassSection { get; set; }
        [JsonPropertyName("component")] public string Component { get; set; }
        [JsonPropertyName("consent")] public string Consent { get; set; }
        [JsonPropertyName("dropConsent")] public string DropConsent { get; set; }
        [JsonPropertyName("cituuid")] public Guid? Cituuid { get; set; }

        [Column(TypeName = "jsonb")]
        [JsonPropertyName("meetingPatterns")]
        public MeetingPattern[] MeetingPatterns { get; set; }
    }
}