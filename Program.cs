// Need these using statements for stuff below
using Project3.Utilities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Project3.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.Extensions.Configuration;
using Project3.Services;
using Microsoft.EntityFrameworkCore;
using Project3.Database;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

// Load email settings from appsettings.json
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register my custom services
builder.Services.AddTransient<Project3.Utilities.Email>();
builder.Services.AddScoped<Project3.Utilities.DBConnect>(); // Scoped for DB stuff seems right
builder.Services.AddScoped<IUserService, UserService>();

// Need this for making HTTP calls (IHttpClientFactory)
builder.Services.AddHttpClient();
// Setting up a specific HttpClient for calling my own API?
builder.Services.AddHttpClient("Project3Api", client =>
{
    // Make sure this base address matches where the API is running locally! Ends with '/'
    client.BaseAddress = new Uri("https://localhost:7256/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Adds services for MVC Controllers and Views
builder.Services.AddControllersWithViews();

// Swagger stuff for API testing page
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Caching and Session state
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    // options.IdleTimeout = TimeSpan.FromMinutes(30); // Can set timeout later if needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Helps session work even with cookie consent popups
});

// Authentication - using Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // Redirect here if not logged in
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied"; // Redirect here if logged in but not allowed
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// Authorization services (needed if using [Authorize])
builder.Services.AddAuthorization();

builder.Services.AddLogging();

// ==================================================
var app = builder.Build();
// ==================================================

// --- Middleware Pipeline (Order Matters!) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Dev mode: show detailed errors and the Swagger UI
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(); // Makes the /swagger page work
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // For wwwroot files (CSS, JS)
app.UseRouting(); // Decides which endpoint to use

// Session needs to be configured before Auth and endpoint mapping
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// --- Map Endpoints ---

// Maps API controllers (using routes defined in the controller files)
app.MapControllers();

// Maps the default route for MVC pages (controller/action/optional-id)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ==================================================
app.Run(); // Start the app!
// ==================================================
