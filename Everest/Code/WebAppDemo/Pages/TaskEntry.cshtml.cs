using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.IO;
using System.Linq;


public class TaskEntryModel : PageModel
{
    [BindProperty]
    public string? Requirement { get; set; }
    public string? OpenAIResponse { get; set; }

    // Store the parsed task list for display
    public List<TaskList>? ParsedTaskList { get; set; }
    
    [BindProperty]
    public List<string>? SelectedUsers { get; set; }

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
        var roleCollection = db.GetCollection<MongoDB.Bson.BsonDocument>("userStore");
        var roleDoc = roleCollection.Find(new MongoDB.Bson.BsonDocument("username", username)).FirstOrDefault();
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
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Use only selected users from the form
        var selectedUsers = SelectedUsers ?? new List<string>();
   
        if (!string.IsNullOrWhiteSpace(Requirement))
        {
            var apiKey = "<APIKEY>"; // TODO: Replace with your actual OpenAI API key
            // Only include selected users in the teamMembers list
            var allMembers = GetTeamMembers();
            var teamMembers = allMembers.Where(u => selectedUsers.Contains(u.Id.ToString())).ToList();
            var teamJson = JsonSerializer.Serialize(teamMembers);
            var prompt = $"Act as Scrum master with the following team members: {teamJson}. Generate a list of sub-tasks with task name, duration, and sequence for: {Requirement} and assign it to the team members based on their role. Send the output as a json format which has taskname, duration (in days), assignedTo (username), startDate (format dd/mm/yyyy), endDate (format dd/mm/yyyy) and sequenceNumber. Consider startDate as today for the first task in the sequence and use duration to increment for subsequenct task";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[] {
                    new { role = "developer", content = prompt }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                OpenAIResponse = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            }
            else
            {
                OpenAIResponse = $"Error: {response.StatusCode}";
            }
        }
        Console.WriteLine("OpenAI response: " + OpenAIResponse); // This line is for debugging purposes, you can remove it later.

        // Parse OpenAIResponse and store in MongoDB
        if (!string.IsNullOrWhiteSpace(OpenAIResponse))
        {
            try
            {
                // Find the first '[' and last ']' to extract the JSON array
                int start = OpenAIResponse.IndexOf('[');
                int end = OpenAIResponse.LastIndexOf(']');
                if (start != -1 && end != -1 && end > start)
                {
                    string jsonArray = OpenAIResponse.Substring(start, end - start + 1);
                    var taskList = JsonSerializer.Deserialize<List<TaskList>>(jsonArray);
                    if (taskList != null && taskList.Count > 0)
                    {
                        var props = System.IO.File.ReadAllLines("db.properties")
                            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
                            .Select(line => line.Split('=', 2))
                            .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
                        var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
                        var client = new MongoClient(mongoUri);
                        var db = client.GetDatabase("scrummaster");
                        var collection = db.GetCollection<TaskList>("taskList");
                        // Add projectName to each task before saving
                        var selectedProjectId = Request.Form["ProjectName"].ToString();
                        var selectedProject = GetProjects().FirstOrDefault(p => p.Id.ToString() == selectedProjectId);
                        string projectName = selectedProject?.Name ?? "Unknown";
                        foreach (var task in taskList)
                        {
                            task.projectName = projectName;
                        }
                        collection.InsertMany(taskList);
                        ParsedTaskList = taskList; // Set for display
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing or saving task list: " + ex.Message);
            }
        }
        return Page();
    }

    public class TaskList
    {
        public string? taskName { get; set; }
        public int? duration { get; set; }
        public string? assignedTo { get; set; }
        public int sequenceNumber { get; set; }
        public string? startDate { get; set; }
        public string? endDate { get; set; }
        public string? projectName { get; set; } // Added projectName field
        public int progress { get; set; } = 0; // Default progress to 0
    }

    public new class User
    {
        public ObjectId Id { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("username")]
        public string? Username { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("status")]
        public string? Status { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("role")]
        public string? Role { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("firstname")]
        public string? FirstName { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonElement("lastname")]
        public string? LastName { get; set; }
    }

    public List<User> GetTeamMembers()
    {
        var props = System.IO.File.ReadAllLines("db.properties")
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
        var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
        var client = new MongoClient(mongoUri);
        var db = client.GetDatabase("scrummaster");
        var collection = db.GetCollection<User>("userStore");
        var projection = Builders<User>.Projection
            .Include(u => u.Id)
            .Include(u => u.Username)
            .Include(u => u.Status)
            .Include(u => u.Role)
            .Include(u => u.FirstName)
            .Include(u => u.LastName);
        var users = collection.Find(_ => true).Project<User>(projection).ToList();
        return users;
    }

    public class Project
    {
        public ObjectId Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public List<Project> GetProjects()
    {
        var props = System.IO.File.ReadAllLines("db.properties")
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
        var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
        var client = new MongoClient(mongoUri);
        var db = client.GetDatabase("scrummaster");
        var collection = db.GetCollection<Project>("projectStore");
        var projects = collection.Find(_ => true).ToList();
        return projects;
    }
}
