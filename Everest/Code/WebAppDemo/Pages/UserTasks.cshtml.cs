using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class UserTasksModel : PageModel
{
    public List<UserTask> Tasks { get; set; } = new List<UserTask>();

    public void OnGet()
        // // Determine user role for tile/page protection
        // var roleCollection = db.GetCollection<BsonDocument>("userStore");
        // var roleDoc = roleCollection.Find(new BsonDocument("username", username)).FirstOrDefault();
        // string userRole = "";
        // if (roleDoc != null && roleDoc.Contains("role"))
        // {
        //     userRole = roleDoc["role"].AsString;
        // }
        // bool isPrivileged = username == "admin" || userRole == "SuperAdmin" || userRole == "ScrumMaster";
        // ViewData["ShowGanttAndTaskEntryTiles"] = isPrivileged;
        // ViewData["UserRole"] = userRole;
    {
        Console.WriteLine("UserTasksModel OnGet called");
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username)) return;
        // Read MongoDB connection string from db.properties
        var props = System.IO.File.ReadAllLines("db.properties")
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
        var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
        var client = new MongoClient(mongoUri);
        var db = client.GetDatabase("scrummaster");
        var userCollection = db.GetCollection<User>("userStore");
        var projection = Builders<User>.Projection
            .Include(u => u.username)
            .Include(u => u.firstname)
            .Include(u => u.lastname);
        var userDoc = userCollection.Find(u => u.username == username).Project(projection).FirstOrDefault();
        if (userDoc == null)
        {
            Console.WriteLine($"User not found: {username}");
            return;
        }
        var user = new User
        {
            username = userDoc.GetValue("username", "").AsString,
            firstname = userDoc.GetValue("firstname", "").AsString,
            lastname = userDoc.GetValue("lastname", "").AsString
        };
        var fullName = $"{user.firstname} {user.lastname}".Trim();
        var taskCollection = db.GetCollection<UserTask>("taskList");
        var taskProjection = Builders<UserTask>.Projection
            .Include(t => t.taskName)
            .Include(t => t.projectName)
            .Include(t => t.startDate)
            .Include(t => t.endDate)
            .Include(t => t.progress)
            .Include(t => t.assignedTo);
        var taskDocs = taskCollection.Find(t => t.assignedTo == username).Project(taskProjection).ToList();
        Tasks = new List<UserTask>();
        foreach (var doc in taskDocs)
        {
            Tasks.Add(new UserTask
            {
                taskName = doc.GetValue("taskName", "").AsString,
                projectName = doc.GetValue("projectName", "").AsString,
                startDate = doc.GetValue("startDate", "").AsString,
                endDate = doc.GetValue("endDate", "").AsString,
                progress = doc.GetValue("progress", 0).ToInt32(),
                assignedTo = doc.GetValue("assignedTo", "").AsString
            });
        }
        Console.WriteLine($"Retrieved {Tasks.Count} tasks for user: {fullName}");
    }

    public class User
    {
        public ObjectId Id { get; set; }
        public string? username { get; set; }
        public string? firstname { get; set; }
        public string? lastname { get; set; }
    }

    public class UserTask
    {
        public ObjectId Id { get; set; }
        public string? taskName { get; set; }
        public string? projectName { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public int progress { get; set; }
        
        public string? assignedTo { get; set; }
    }
}
