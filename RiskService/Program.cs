using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text;
using CommonLib.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using CommonLib.Models.Risk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RiskService.Services;
using System.IdentityModel.Tokens.Jwt;

// Create and configure the WebApplication builder
var builder = WebApplication.CreateBuilder(args);

// ======================================================
// LOGGING CONFIGURATION
// ======================================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ======================================================
// SERVICES REGISTRATION
// ======================================================
// Configure controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Register custom JSON converter for ObjectId
        options.JsonSerializerOptions.Converters.Add(new ObjectIdJsonConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Register CommonLib services
builder.Services.AddScoped<ILoggerService, LoggerService>();

// Register MongoDB services
builder.Services.AddSingleton<MongoDbConnectionFactory>();

// Add database initialization service
builder.Services.AddSingleton<DatabaseInitializationService>();

// Register Risk-specific services
builder.Services.AddScoped<IRiskService, RiskService.Services.RiskService>();

// Register HttpClientService for inter-service communication
builder.Services.AddHttpClient();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// ======================================================
// JWT AUTHENTICATION CONFIGURATION
// ======================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Name,
        RoleClaimType = ClaimTypes.Role
    };
});

// JWT service registration
builder.Services.AddSingleton<JwtService>(sp =>
{
    return new JwtService(
        builder.Configuration,
        sp.GetRequiredService<ILoggerService>());
});

// ======================================================
// SWAGGER CONFIGURATION
// ======================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Risk Service API", Version = "v1" });

    // Configure XML comments file
    try
    {
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    }
    catch (Exception ex)
    {
        // Log but don't fail if XML comments aren't found
        Console.WriteLine($"XML comments file not found: {ex.Message}");
    }

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ======================================================
// CORS CONFIGURATION
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// ======================================================
// HEALTH CHECKS CONFIGURATION
// ======================================================
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());

// Add MongoDB health check with a custom check
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"];
if (!string.IsNullOrEmpty(mongoConnectionString))
{
    builder.Services.AddHealthChecks()
        .AddCheck("mongodb", () =>
        {
            try
            {
                var client = new MongoClient(mongoConnectionString);
                client.ListDatabases();
                return HealthCheckResult.Healthy("MongoDB connection is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("MongoDB connection failed", ex);
            }
        },
        new[] { "db", "mongodb" });
}

// ======================================================
// APPLICATION CONFIGURATION
// ======================================================
var app = builder.Build();

// Get instance of our logger service for startup logs
var logger = app.Services.GetRequiredService<ILoggerService>();
logger.LogInformation("RiskService starting up...");

// ======================================================
// DATABASE INITIALIZATION
// ======================================================
try
{
    logger.LogInformation("Initializing database connection...");
    var dbFactory = app.Services.GetRequiredService<MongoDbConnectionFactory>();
    if (!dbFactory.IsConnectionValid())
    {
        logger.LogCritical("MongoDB connection failed. Application cannot start.");
        Environment.ExitCode = 1;
        return; // Exit the application
    }

    var dbInitService = app.Services.GetRequiredService<DatabaseInitializationService>();
    await dbInitService.InitializeAllDatabasesAsync();
    logger.LogInformation("Database initialization completed successfully");
}
catch (Exception ex)
{
    logger.LogCritical($"Error initializing databases: {ex.Message}");
    // Terminate application immediately if database initialization fails
    logger.LogCritical("Application is shutting down due to database initialization failure");
    Environment.ExitCode = 1;
    return; // Exit the application
}

// ======================================================
// MIDDLEWARE CONFIGURATION
// ======================================================
// Configure Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Risk Service API v1");
    c.RoutePrefix = string.Empty;
    c.DocumentTitle = "Risk Service Documentation";
    c.EnableDeepLinking();
    c.DisplayRequestDuration();
});

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapControllers();
app.MapHealthChecks("/health");

logger.LogInformation("RiskService started successfully");
app.Run();
