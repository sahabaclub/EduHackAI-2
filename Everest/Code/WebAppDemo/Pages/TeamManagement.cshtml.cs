using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebAppDemo.Models;
namespace WebAppDemo.Pages
{
    [IgnoreAntiforgeryToken]
    public class TeamManagementModel : PageModel
    {
        public List<TeamUser> Users { get; set; } = new();
        public string? Search { get; set; }

        [BindProperty]
        public string? NewUsername { get; set; }
        [BindProperty]
        public string? NewFirstName { get; set; }
        [BindProperty]
        public string? NewLastName { get; set; }
        [BindProperty]
        public string? NewPassword { get; set; }
        [BindProperty]
        public string? NewEmail { get; set; }
        [BindProperty]
        public string? NewTeamName { get; set; }
        [BindProperty]
        public string? NewRole { get; set; }
        [BindProperty]
        public string? NewStatus { get; set; }
        [BindProperty]
        public string? EditId { get; set; }

        public void OnGet(string? search)
        {
            // Only allow users/roles from page_roles.properties
            var username = HttpContext.Session.GetString("Username");
            var allowedRoles = new List<string>();
            var rolesPath = Path.Combine(Directory.GetCurrentDirectory(), "page_roles.properties");
            if (System.IO.File.Exists(rolesPath))
            {
                var lines = System.IO.File.ReadAllLines(rolesPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('=');
                    if (parts.Length == 2 && parts[0].Trim() == "TeamManagement")
                    {
                        allowedRoles = parts[1].Split(',').Select(r => r.Trim()).ToList();
                    }
                }
            }
            if (!string.IsNullOrEmpty(username))
            {
                if (!string.IsNullOrEmpty(username) && allowedRoles.Contains(username))
                {
                    // allowed by username
                }
                else
                {
                    var client = new MongoClient("mongodb://test:test@localhost:27017/?authSource=admin");
                    var db = client.GetDatabase("scrummaster");
                    var collection = db.GetCollection<TeamUser>("userStore");
                    var user = collection.Find(u => u.Username == username).FirstOrDefault();
                    if (user == null || string.IsNullOrEmpty(user.Role) || !allowedRoles.Contains(user.Role))
                    {
                        Response.Redirect("/Index");
                        return;
                    }
                }
            }
            else
            {
                Response.Redirect("/Index");
                return;
            }
            var client2 = new MongoClient("mongodb://test:test@localhost:27017/?authSource=admin");
            var db2 = client2.GetDatabase("scrummaster");
            var collection2 = db2.GetCollection<TeamUser>("userStore");
            if (!string.IsNullOrEmpty(search))
            {
                Users = collection2.Find(u => u.Username != null && u.Username.ToLower().Contains(search.ToLower())).ToList();
            }
            else
            {
                Users = collection2.Find(_ => true).ToList();
            }
            Search = search;
        }

        public IActionResult OnPost()
        {
            // Only allow admin, SuperAdmin, or ScrumMaster
            var username = HttpContext.Session.GetString("Username");
            if (username != "admin")
            {
                if (!string.IsNullOrEmpty(username))
                {
                    var props = System.IO.File.ReadAllLines("db.properties")
                        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
                        .Select(line => line.Split('=', 2))
                        .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
                    var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
                    var client = new MongoClient(mongoUri);
                    var db = client.GetDatabase("scrummaster");
                    var collection = db.GetCollection<TeamUser>("userStore");
                    var user = collection.Find(u => u.Username == username).FirstOrDefault();
                    if (user == null || (user.Role != "SuperAdmin" && user.Role != "ScrumMaster"))
                    {
                        return RedirectToPage("/Index");
                    }
                }
                else
                {
                    return RedirectToPage("/Index");
                }
            }
            // Server-side validation
            if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewRole) || string.IsNullOrWhiteSpace(NewStatus)
                || string.IsNullOrWhiteSpace(NewFirstName) || string.IsNullOrWhiteSpace(NewLastName) || string.IsNullOrWhiteSpace(NewPassword)
                || string.IsNullOrWhiteSpace(NewEmail) || string.IsNullOrWhiteSpace(NewTeamName))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return Page();
            }
            // Username validation: only alphanumeric
            if (!Regex.IsMatch(NewUsername, "^[a-zA-Z0-9]+$"))
            {
                ModelState.AddModelError("NewUsername", "Username can only contain letters and numbers.");
                return Page();
            }
            if (!IsPasswordComplex(NewPassword))
            {
                ModelState.AddModelError("NewPassword", "Password must be at least 12 characters, contain 1 uppercase, 1 number, and 1 special character.");
                return Page();
            }
            var props2 = System.IO.File.ReadAllLines("db.properties")
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
                .Select(line => line.Split('=', 2))
                .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
            var mongoUri2 = props2.ContainsKey("uri") ? props2["uri"] : "mongodb://localhost:27017";
            var client2 = new MongoClient(mongoUri2);
            var db2 = client2.GetDatabase("scrummaster");
            var collection2 = db2.GetCollection<TeamUser>("userStore");
            // Check for username uniqueness
            var existing = collection2.Find(u => u.Username == NewUsername).FirstOrDefault();
            if (existing != null)
            {
                ModelState.AddModelError("NewUsername", "Username already exists.");
                return Page();
            }
            // Encrypt password using SHA256
            string hashedPassword = ComputeSha256Hash(NewPassword);
            var user2 = new TeamUser {
                Username = NewUsername,
                FirstName = NewFirstName,
                LastName = NewLastName,
                Password = hashedPassword,
                Email = NewEmail,
                TeamName = NewTeamName,
                Role = NewRole,
                Status = NewStatus,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };
            collection2.InsertOne(user2);
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(string id)
        {
            var client = new MongoClient("mongodb://test:test@localhost:27017/?authSource=admin");
            var db = client.GetDatabase("scrummaster");
            var collection = db.GetCollection<TeamUser>("userStore");
            collection.DeleteOne(u => u.Id == ObjectId.Parse(id));
            return RedirectToPage();
        }

        public IActionResult OnPostEdit()
        {
            if (string.IsNullOrWhiteSpace(EditId) || string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewRole) || string.IsNullOrWhiteSpace(NewStatus)
                || string.IsNullOrWhiteSpace(NewFirstName) || string.IsNullOrWhiteSpace(NewLastName) || string.IsNullOrWhiteSpace(NewEmail) || string.IsNullOrWhiteSpace(NewTeamName))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return Page();
            }
            if (!Regex.IsMatch(NewUsername, "^[a-zA-Z0-9]+$"))
            {
                ModelState.AddModelError("NewUsername", "Username can only contain letters and numbers.");
                return Page();
            }
            var client = new MongoClient("mongodb://test:test@localhost:27017/?authSource=admin");
            var db = client.GetDatabase("scrummaster");
            var collection = db.GetCollection<TeamUser>("userStore");
            var update = Builders<TeamUser>.Update
                .Set(u => u.Username, NewUsername)
                .Set(u => u.FirstName, NewFirstName)
                .Set(u => u.LastName, NewLastName)
                .Set(u => u.Email, NewEmail)
                .Set(u => u.TeamName, NewTeamName)
                .Set(u => u.Role, NewRole)
                .Set(u => u.Status, NewStatus)
                .Set(u => u.Modified, DateTime.UtcNow);
            collection.UpdateOne(u => u.Id == ObjectId.Parse(EditId), update);
            return RedirectToPage();
        }

        public JsonResult OnGetCheckUsername(string username)
        {
            var client = new MongoClient("mongodb://test:test@localhost:27017/?authSource=admin");
            var db = client.GetDatabase("scrummaster");
            var collection = db.GetCollection<TeamUser>("userStore");
            var exists = collection.Find(u => u.Username == username).Any();
            return new JsonResult(new { exists });
        }

        private bool IsPasswordComplex(string password)
        {
            if (password.Length < 12) return false;
            if (!Regex.IsMatch(password, @"[A-Z]")) return false;
            if (!Regex.IsMatch(password, @"[0-9]")) return false;
            if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]")) return false;
            return true;
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(rawData);
                var hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
            }
        }

    }
}

