using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class GanttDataModel : PageModel
{
    public IActionResult OnGet()
    {
        var props = System.IO.File.ReadAllLines("db.properties")
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
        var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
        var client = new MongoClient(mongoUri);
        var db = client.GetDatabase("scrummaster");
        var collection = db.GetCollection<TaskList>("taskList");
        var tasks = collection.Find(_ => true).ToList();
        var ganttTasks = new List<object>();
        foreach (var t in tasks)
        {
            string start = string.Empty;
            string end = string.Empty;
            if (!string.IsNullOrWhiteSpace(t.startDate)) 
                start = t.startDate;
            if (!string.IsNullOrWhiteSpace(t.endDate)) 
                end = t.endDate;
            ganttTasks.Add(new {
                id = t.Id.ToString(),
                name = string.IsNullOrWhiteSpace(t.assignedTo) ? t.taskName : $"{t.taskName} ({t.assignedTo})",
                start = start,
                end = end,
                type = "task",
                progress = t.progress,
                dependencies = "",
                assignedTo = t.assignedTo
            });
        }
        return new JsonResult(ganttTasks);
    }

    public class TaskList
    {
        public ObjectId Id { get; set; }
        public string? taskName { get; set; }
        public int? duration { get; set; }
        public string? assignedTo { get; set; }
        public int sequenceNumber { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public string? projectName { get; set; } // Added projectName field
        public int progress { get; set; } = 0; // Default progress to 0
    }
}
