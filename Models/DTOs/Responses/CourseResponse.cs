using System;
using System.Collections.Generic;

namespace CourseWiki.Models.DTOs.Responses
{
    public class CourseResponse
    {
        public Guid Id { get; }
        public string Subject { get; }
        public string CatalogNbr { get; }
        public string Description { get; }
        public string CrseId { get; }
        public string Title { get; }
        public List<Guid> ClassUUIDs { get; }
    }
}