using System;
using System.Collections.Generic;

namespace CourseWiki.Models.DTOs.Responses
{
    public class CourseSearchByTxtResponse
    {
        public Guid Id { get; set; }
        
        public string CatalogNbr { get; set; }
        public string Description { get; set; } 
        public string Subject { get; set; }

        public string Title { get; set; }
    }
}