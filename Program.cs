using Project3.Models; // Needed for SmtpSettings
using Project3.Utilities; // Needed for Email service
using Microsoft.AspNetCore.Authentication.Cookies; // Needed for AddAuthentication/AddCookie

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// ** 1. Configure SmtpSettings from appsettings.json **
// Make sure this line exists to read your SmtpSettings section
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// ** 2. Register your Email service **
// Add this line to tell the DI container how to create the Email service.
// AddTransient means a new instance is created every time it's requested.
builder.Services.AddTransient<Project3.Utilities.Email>();
// Other options: .AddScoped<>() or .AddSingleton<>() depending on desired lifetime and dependencies.
// Transient is usually fine for stateless utility services like this.

// ** 3. Register other services **
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache(); // If using caching
builder.Services.AddSession(); // If using session state

// ** 4. Configure Authentication (Ensure this matches your setup) **
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) // Use default scheme
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // Path to redirect for login
        options.LogoutPath = "/Account/Logout";      // Path for logout
        options.AccessDeniedPath = "/Account/AccessDenied"; // Path if access denied
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Example cookie expiry
        options.SlidingExpiration = true;
    });


// Add Logging (already likely present)
builder.Services.AddLogging();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Use detailed error page in development
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Make sure session middleware is added if you use HttpContext.Session

// Add Authentication and Authorization middleware IN THIS ORDER
app.UseAuthentication(); // Who the user is
app.UseAuthorization();  // What the user can do

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
