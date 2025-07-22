using Microsoft.AspNetCore.Mvc.RazorPages;

using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GanttChartModel : PageModel
{
    public List<TaskList> Tasks { get; set; } = new List<TaskList>();

    public void OnGet()
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username)) { Response.Redirect("/Login"); return; }
        var props = System.IO.File.ReadAllLines("db.properties")
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
        var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
        var client = new MongoClient(mongoUri);
        var db = client.GetDatabase("scrummaster");
        var roleCollection = db.GetCollection<BsonDocument>("userStore");
        var roleDoc = roleCollection.Find(new BsonDocument("username", username)).FirstOrDefault();
        string userRole = "";
        if (roleDoc != null && roleDoc.Contains("role"))
        {
            userRole = roleDoc["role"].AsString;
        }
        bool isPrivileged = username == "admin" || userRole == "SuperAdmin" || userRole == "ScrumMaster";
        if (!isPrivileged)
        {
            Response.Redirect("/UserTasks");
            return;
        }
        var collection = db.GetCollection<TaskList>("taskList");
        Tasks = collection.Find(_ => true).SortBy(t => t.sequenceNumber).ToList();
    }

    public List<Project> GetProjects()
    {
        var client = new MongoClient("mongodb://test:test@localhost:27017/?authSource=admin");
        var db = client.GetDatabase("scrummaster");
        var collection = db.GetCollection<Project>("projectStore");
        var projects = collection.Find(_ => true).ToList();
        return projects;
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
        public int progress { get; set; } = 0; // Add progress property for Gantt chart, default to 0
    }

    public class Project
    {
        public ObjectId Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
