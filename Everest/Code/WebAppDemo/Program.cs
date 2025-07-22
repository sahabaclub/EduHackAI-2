using System.IO;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddDataProtection();
builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

// Global authentication middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();
    if (!string.IsNullOrEmpty(path) &&
        !path.StartsWith("/login") && !path.StartsWith("/css") && !path.StartsWith("/js") && !path.StartsWith("/lib") && !path.StartsWith("/favicon") && !path.StartsWith("/api/ganttdata") && !path.StartsWith("/api/updatetasks"))
    {
        if (context.Session.GetString("IsAuthenticated") != "true")
        {
            context.Response.Redirect("/Login");
            return;
        }
    }
    await next();
});






// Minimal API for updating tasks
app.MapPost("/api/updatetasks", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    List<TaskUpdate>? updates = null;
    try {
        updates = System.Text.Json.JsonSerializer.Deserialize<List<TaskUpdate>>(body);
    } catch (Exception ex) {
        return Results.Json(new { success = false, error = "Deserialization failed", body, exception = ex.Message });
    }
    if (updates == null || updates.Count == 0)
        return Results.Json(new { success = false, error = "No updates received", body });
    var client = new MongoDB.Driver.MongoClient("mongodb://test:test@localhost:27017/?authSource=admin");
    var db = client.GetDatabase("scrummaster");
    var collection = db.GetCollection<TaskList>("taskList");
    int updatedCount = 0;
    foreach (var upd in updates)
    {
        if (string.IsNullOrWhiteSpace(upd.id)) continue;
        string startFormatted = upd.start;
        string endFormatted = upd.end;
        DateTime? startDt = null;
        DateTime? endDt = null;
        if (!string.IsNullOrWhiteSpace(upd.start))
        {
            DateTime temp;
            if (DateTime.TryParseExact(upd.start, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out temp))
                startDt = temp;
        }
        if (!string.IsNullOrWhiteSpace(upd.end))
        {
            DateTime temp;
            if (DateTime.TryParseExact(upd.end, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out temp))
                endDt = temp;
        }
        if (startDt.HasValue)
            startFormatted = startDt.Value.ToString("dd-MM-yyyy");
        if (endDt.HasValue)
            endFormatted = endDt.Value.ToString("dd-MM-yyyy");
        var filter = MongoDB.Driver.Builders<TaskList>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(upd.id));
        var updateDef = MongoDB.Driver.Builders<TaskList>.Update
            .Set("startDate", startFormatted)
            .Set("endDate", endFormatted)
            .Set("progress", upd.progress);
        var result = await collection.UpdateOneAsync(filter, updateDef);
        if (result.ModifiedCount > 0) updatedCount++;
    }
    return Results.Json(new { success = true, updatedCount });
});
    

// Minimal API for saving a new project
app.MapPost("/api/addproject", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    Project? project = null;
    try {
        project = System.Text.Json.JsonSerializer.Deserialize<Project>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    } catch (Exception ex) {
        return Results.Json(new { success = false, error = "Deserialization failed", body, exception = ex.Message });
    }
    if (project == null || string.IsNullOrWhiteSpace(project.Name))
        return Results.Json(new { success = false, error = "Invalid project data", body });
    var props = File.ReadAllLines("db.properties")
        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
        .Select(line => line.Split('=', 2))
        .ToDictionary(parts => parts[0].Trim(), parts => parts.Length > 1 ? parts[1].Trim() : "");
    var mongoUri = props.ContainsKey("uri") ? props["uri"] : "mongodb://localhost:27017";
    var client = new MongoDB.Driver.MongoClient(mongoUri);
    var db = client.GetDatabase("scrummaster");
    var collection = db.GetCollection<Project>("projectStore");
    await collection.InsertOneAsync(new Project { Name = project.Name, Description = project.Description });
    return Results.Json(new { success = true });
});

// Minimal API to fetch tasks by projectId
app.MapGet("/api/gettasksbyproject", async (HttpContext context) =>
{
    var projectId = context.Request.Query["projectId"].ToString();
    if (string.IsNullOrWhiteSpace(projectId))
        return Results.Json(new List<TaskList>());
    var client = new MongoDB.Driver.MongoClient("mongodb://test:test@localhost:27017/?authSource=admin");
    var db = client.GetDatabase("scrummaster");
    var projectCollection = db.GetCollection<Project>("projectStore");
    var projectObjId = MongoDB.Bson.ObjectId.TryParse(projectId, out var objId) ? objId : (ObjectId?)null;
    if (projectObjId == null)
        return Results.Json(new List<TaskList>());
    var project = await projectCollection.Find(p => p.Id == projectObjId).FirstOrDefaultAsync();
    if (project == null || string.IsNullOrWhiteSpace(project.Name))
        return Results.Json(new List<TaskList>());
    var taskCollection = db.GetCollection<TaskList>("taskList");
    var tasks = await taskCollection.Find(t => t.projectName == project.Name).ToListAsync();
    return Results.Json(tasks);
});

// Minimal API for updating user password
app.MapPost("/api/updatepassword", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    try {
        var req = System.Text.Json.JsonSerializer.Deserialize<PasswordUpdateRequest>(body);
        if (req == null || string.IsNullOrWhiteSpace(req.id) || string.IsNullOrWhiteSpace(req.password))
            return Results.Json(new { success = false, error = "Invalid request." });
        var client = new MongoDB.Driver.MongoClient("mongodb://test:test@localhost:27017/?authSource=admin");
        var db = client.GetDatabase("scrummaster");
        var collection = db.GetCollection<User>("userStore");
        var filter = MongoDB.Driver.Builders<User>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(req.id));
        // Hash password before saving (simple SHA256 for demo, use bcrypt/argon2 in production)
        string hashed = string.Empty;
        // Console.WriteLine($"Hashing password for user {req.id} with provided password. {req.password}");

        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(req.password);
            var hash = sha.ComputeHash(bytes);
            hashed = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            // Console.WriteLine($"Hashed password: {hashed}");
        }
        var updateDef = MongoDB.Driver.Builders<User>.Update.Set("password", hashed);
        var result = await collection.UpdateOneAsync(filter, updateDef);
        if (result.ModifiedCount > 0)
            return Results.Json(new { success = true });
        else
            return Results.Json(new { success = false, error = "User not found or password not updated." });
    } catch (Exception ex) {
        return Results.Json(new { success = false, error = ex.Message });
    }
});

app.MapRazorPages();
app.UseAuthorization();
app.Run();

// DTO for password update
public class PasswordUpdateRequest
{
    public string? id { get; set; }
    public string? password { get; set; }
}



// DTOs must be declared before any top-level statements
public class TaskUpdate
{
    public string? id { get; set; }
    public string? start { get; set; }
    public string? end { get; set; }
    public int progress { get; set; } = 0; // Default progress to 0
}
public class TaskList
{
    public MongoDB.Bson.ObjectId Id { get; set; }
    public string? taskName { get; set; }
    public int? duration { get; set; }
    public string? assignedTo { get; set; }
    public int sequenceNumber { get; set; }
    public string? startDate { get; set; }
    public string? endDate { get; set; }
    public string? projectName { get; set; } // Added projectName field
    public int progress { get; set; } = 0; // Default progress to 0

}



public class Project
{
    public MongoDB.Bson.ObjectId Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class User
{
    public MongoDB.Bson.ObjectId Id { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? TeamName { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
}