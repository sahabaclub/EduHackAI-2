using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using WebAppDemo.Models;

namespace WebAppDemo.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string? Username { get; set; }
        [BindProperty]
        public string? Password { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Clear session on GET
            HttpContext.Session.Clear();
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Username and password are required.";
                return Page();
            }
            var props = System.IO.File.ReadAllLines("db.properties")
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
                .Select(line => line.Split('=', 2))
                .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
            var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
            var client = new MongoClient(mongoUri);
            var db = client.GetDatabase("scrummaster");
            var collection = db.GetCollection<TeamUser>("userStore");
            string hashedPassword = ComputeSha256Hash(Password);
            Console.WriteLine($"Login attempt: Username={Username}, PasswordHash={hashedPassword}");
            Console.WriteLine($"collection:{collection}");
            //Console.WriteLine($"Login attempt: Username={Username}, PasswordHash={hashedPassword}");
            var user = collection.Find(u => u.Username == Username && u.Password == hashedPassword).FirstOrDefault();
            if (user == null)
            {
                // Check users.properties for admin login
                var propsPath = Path.Combine(Directory.GetCurrentDirectory(), "users.properties");
                //Console.WriteLine($"Checking users.properties at: {propsPath}");
                if (System.IO.File.Exists(propsPath))
                {
                    var lines = System.IO.File.ReadAllLines(propsPath);
                    foreach (var line in lines)
                    {
                        //Console.WriteLine($"Inside for loop Line: {line}");
                        if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split('=');
                        //Console.WriteLine($"Parts: {string.Join(",", parts)}");
                        //Console.WriteLine($"userName: {Username} and hashedPassword: {hashedPassword}");
                        if (parts.Length == 2 && parts[0].Trim() == Username && parts[1].Trim() == hashedPassword)
                        {
                            //Console.WriteLine($"Admin login success for {Username}");
                            // Set session for admin
                            HttpContext.Session.SetString("IsAuthenticated", "true");
                            HttpContext.Session.SetString("Username", Username ?? string.Empty);
                            return RedirectToPage("/Index");
                        }
                    }
                }
                //Console.WriteLine($"Admin login failed for {Username}");
                ErrorMessage = "Invalid username or password.";
                return Page();
            }
            // Set session
            HttpContext.Session.SetString("IsAuthenticated", "true");
            HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
            return RedirectToPage("/Index");
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(rawData);
                var hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
            }
        }
    }

}
