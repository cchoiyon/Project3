using Project3.Utilities; // Needed for Email service and DBConnect
using Microsoft.AspNetCore.Authentication.Cookies; // Needed for AddAuthentication/AddCookie
using Project3.Models.Configuration; // Assuming SmtpSettings is here

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// ** 1. Configure SmtpSettings from appsettings.json **
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// ** 2. Register Custom Services **
// Register Email service (used by AccountApiController)
builder.Services.AddTransient<Project3.Utilities.Email>();
// Register DBConnect (used by API Controllers) - Scoped is often suitable
builder.Services.AddScoped<Project3.Utilities.DBConnect>();

// ** 3. Register HttpClientFactory **
// Allows MVC controllers (and other services) to make HTTP requests to APIs
builder.Services.AddHttpClient();
// Configure the named client for your internal API
builder.Services.AddHttpClient("Project3Api", client =>
{
    // *** ADDED BaseAddress CONFIGURATION ***
    // Set the base address for API calls made using this named client.
    // Use the correct HTTPS URL and port for your local development environment.
    // This should match the URL where your application is running (e.g., from launchSettings.json).
    // Make sure the URL ends with a slash '/'.
    client.BaseAddress = new Uri("https://localhost:7256/"); // Using port 7256 as identified

    // Existing configuration:
    client.DefaultRequestHeaders.Add("Accept", "application/json");

    // Optional: Add other default headers if needed
    // client.DefaultRequestHeaders.Add("User-Agent", "Project3-MvcClient");
});


// ** 4. Register MVC & API Controllers **
// AddControllersWithViews registers services for Views, Controllers, and basic API support.
builder.Services.AddControllersWithViews();

// ** 5. Add Swagger/OpenAPI Services (for API testing/documentation) **
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer(); // Explores API endpoints
builder.Services.AddSwaggerGen(); // Generates Swagger JSON docs

// ** 6. Add Caching & Session **
builder.Services.AddMemoryCache(); // If using caching
builder.Services.AddSession(options =>
{
    // Configure session options if needed (e.g., timeout)
    // options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Make session cookie essential
});

// ** 7. Configure Authentication **
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // Where to redirect for login
        options.LogoutPath = "/Account/Logout";      // Path for logout action
        options.AccessDeniedPath = "/Account/AccessDenied"; // Where to redirect on access denied
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Cookie expiry
        options.SlidingExpiration = true; // Renew cookie on activity
    });

// ** 8. Add Logging **
builder.Services.AddLogging();


// ==================================================
// Build the App
// ==================================================
var app = builder.Build();

// ==================================================
// Configure the HTTP request pipeline (Middleware Order Matters!).
// ==================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Use custom error page in production
    app.UseHsts(); // Use HSTS for security
}
else
{
    app.UseDeveloperExceptionPage(); // Show detailed errors in development
    // ** Enable Swagger UI only in Development **
    app.UseSwagger(); // Generate swagger.json endpoint
    app.UseSwaggerUI(options =>
    {
        // Configure Swagger UI options if needed
        // options.SwaggerEndpoint("/swagger/v1/swagger.json", "Project3 API V1");
        // options.RoutePrefix = string.Empty; // Serve UI at app root if desired
    });
}

app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS

app.UseStaticFiles(); // Serve static files (CSS, JS, images) from wwwroot

app.UseRouting(); // Marks the position where routing decisions are made

app.UseSession(); // Enable session state - Place AFTER UseRouting and BEFORE UseAuthentication/UseAuthorization/MapControllers

// Authentication must come before Authorization
app.UseAuthentication(); // Identify the user (reads cookie, etc.)
app.UseAuthorization();  // Check if the identified user has permission for the requested endpoint

// --- Map Endpoints ---
// Map attribute-routed API controllers (e.g., [Route("api/[controller]")])
app.MapControllers();

// Map conventional MVC routes (for controllers returning Views)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add endpoints for Razor Pages if you were using them (Project requires Views)
// app.MapRazorPages();

// ==================================================
// Run the App
// ==================================================
app.Run();
