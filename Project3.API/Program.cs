using Project3.Shared.Utilities;
using Project3.Shared.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Load email settings from appsettings.json if needed
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Register SmtpSettings as a concrete instance (not IOptions)
builder.Services.AddSingleton(sp => {
    var settings = new SmtpSettings();
    builder.Configuration.GetSection("SmtpSettings").Bind(settings);
    return settings;
});

// Register custom services from Shared project
builder.Services.AddTransient<Project3.Shared.Utilities.Email>();
builder.Services.AddScoped<Project3.Shared.Utilities.Connection>(); 

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7130", // Local WebApp development with HTTPS
                "http://localhost:5133"    // Local WebApp development
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS middleware
app.UseCors("AllowWebApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
