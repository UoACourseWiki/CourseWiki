using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CourseWiki.Models
{
    public class Cls
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("crseId")]
        public string CrseId { get; set; }
        [JsonPropertyName("term")]
        public int Term { get; set; }
        [JsonPropertyName("syllabusLink")]
        public string SyllabusLink { get; set; }
        [JsonPropertyName("canvasLink")]
        public string CanvasLink { get; set; }
        [JsonPropertyName("examLink")]
        public string ExamLink { get; set; }
        [JsonPropertyName("lecturerUUIDs")]
        public List<Guid> LecturerUUIDs { get; set; }
    }
}