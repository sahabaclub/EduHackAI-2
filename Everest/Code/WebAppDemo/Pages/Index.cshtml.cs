using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using WebAppDemo.Models;

namespace WebAppDemo.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public bool ShowTeamManagement { get; set; }

    public void OnGet()
    {
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
                ShowTeamManagement = true;
                return;
            }
            var props = System.IO.File.ReadAllLines("db.properties")
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
                .Select(line => line.Split('=', 2))
                .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
            var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
            var client = new MongoDB.Driver.MongoClient(mongoUri);
            var db = client.GetDatabase("scrummaster");
            var collection = db.GetCollection<WebAppDemo.Models.TeamUser>("userStore");
            var user = collection.Find(u => u.Username == username).FirstOrDefault();
            if (user != null && !string.IsNullOrEmpty(user.Role) && allowedRoles.Contains(user.Role))
            {
                ShowTeamManagement = true;
                return;
            }
        }
        ShowTeamManagement = false;
    }
}
