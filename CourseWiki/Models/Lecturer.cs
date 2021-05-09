using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CourseWiki.Models
{
    public class Lecturer
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("jobTitle")]
        public string JobTitle { get; set; }
        [JsonPropertyName("upi")]
        public string Upi { get; set; }
        
    }
}