using System;
using System.Collections.Generic;

namespace CourseWiki.Models.DTOs.Requests
{
    public class CourseRequest
    {
        public string Subject { get; }
        public string CatalogNbr { get; }
        public string Description { get; }
        public string CrseId { get; }
        public string Title { get; }
    }
}