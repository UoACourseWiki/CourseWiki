using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace CourseWiki.Models
{
    public class Course
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("crseId")] public string CrseId { get; set; }
        [JsonPropertyName("catalogNbr")] public string CatalogNbr { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("subject")] public string Subject { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("rqrmntDescr")] public string RqrmntDescr { get; set; }
        [NotMapped] public List<Guid> CitUUIDs { get; set; }
        [JsonIgnore] public NpgsqlTsVector SearchVector { get; set; }
    }
}