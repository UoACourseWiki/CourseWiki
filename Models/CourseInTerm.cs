using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CourseWiki.Models
{
    public class CourseInTerm
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("crseId")] public string CrseId { get; set; }
        [JsonPropertyName("term")] public string Term { get; set; }
        [JsonPropertyName("syllabusLink")] public string SyllabusLink { get; set; }
        [JsonPropertyName("canvasLink")] public string CanvasLink { get; set; }
        [JsonPropertyName("examLink")] public string ExamLink { get; set; }
        [JsonPropertyName("courseUUID")] public Guid? CourseUUID { get; set; }
        [NotMapped] public List<Guid> ClassUUIDs { get; set; }
    }
}