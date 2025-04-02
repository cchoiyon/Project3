var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<Project3.Models.SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Make session cookie essential
});


builder.Services.AddAuthentication(options =>
{
    // Scheme used after login to check cookie on each request
    options.DefaultAuthenticateScheme = "MyCookieAuth";
    // Scheme used by HttpContext.SignInAsync
    options.DefaultSignInScheme = "MyCookieAuth";
    // *** Scheme to use when [Authorize] fails and needs to redirect to LoginPath ***
    options.DefaultChallengeScheme = "MyCookieAuth";
})
.AddCookie("MyCookieAuth", options => // Use the SAME scheme name here
{
    options.Cookie.Name = "Project3.AuthCookie"; // Your cookie name
    options.LoginPath = "/Account/Login";        // Path to redirect for login
    options.AccessDeniedPath = "/Account/AccessDenied"; // Optional
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Session duration
    options.SlidingExpiration = true;
});

var app = builder.Build();
app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // <<< Add this BEFORE Authorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
