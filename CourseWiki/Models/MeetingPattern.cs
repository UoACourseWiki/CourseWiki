using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CourseWiki.Models
{
    
    public class MeetingPattern
    {
        [JsonPropertyName("startDate")]public string StartDate { get; set; }
        [JsonPropertyName("endDate")]public string EndDate { get; set; }
        [JsonPropertyName("startTime")]public string StartTime { get; set; }
        [JsonPropertyName("endTime")] public string EndTime { get; set; }
        [JsonPropertyName("location")]public string Location { get; set; }
        [JsonPropertyName("daysOfWeek")]public string DateOfWeek { get; set; }
        [JsonPropertyName("meetingNumber")]public int MeetingNumber { get; set; }
    }
}