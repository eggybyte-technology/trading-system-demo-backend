using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;
using CommonLib.Services;
using AccountService.Services;
using CommonLib.Models.Account;
using System.IdentityModel.Tokens.Jwt;
using CommonLib.Api;

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
        options.JsonSerializerOptions.Converters.Add(new ObjectIdJsonConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Register CommonLib services
builder.Services.AddScoped<ILoggerService, LoggerService>();
builder.Services.AddScoped<IApiLoggingService, ApiLoggingService>();

// Register MongoDB services
builder.Services.AddSingleton<MongoDbConnectionFactory>();

// Add database initialization service
builder.Services.AddSingleton<DatabaseInitializationService>();

// Register Account-specific services
builder.Services.AddScoped<IAccountService, AccountService.Services.AccountService>();

// Register HttpClientService for inter-service communication
builder.Services.AddHttpClient();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// Register CommonLib/Api services for inter-service communication
builder.Services.AddTradingSystemServices();

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
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "TradingSystem_DefaultKey")),
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Account Service API", Version = "v1" });

    // Add XML comments for API documentation
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
        Console.WriteLine($"XML comments file not found: {ex.Message}");
    }

    // Add JWT authentication to Swagger UI
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

// Add MongoDB health check
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

// Get logger service for startup logs
var logger = app.Services.GetRequiredService<ILoggerService>();
logger.LogInformation("AccountService starting up...");

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
        return;
    }

    var dbInitService = app.Services.GetRequiredService<DatabaseInitializationService>();
    await dbInitService.InitializeAllDatabasesAsync();
    logger.LogInformation("Database initialization completed successfully");
}
catch (Exception ex)
{
    logger.LogCritical($"Error initializing databases: {ex.Message}");
    logger.LogCritical("Application is shutting down due to database initialization failure");
    Environment.ExitCode = 1;
    return;
}

// ======================================================
// EXCEPTION HANDLING
// ======================================================
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionHandlerFeature != null)
        {
            var exception = exceptionHandlerFeature.Error;
            logger.LogError($"Global error handler caught exception: {exception.Message}");

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                statusCode = context.Response.StatusCode,
                message = "An error occurred processing your request."
            }));
        }
    });
});

// ======================================================
// MIDDLEWARE CONFIGURATION
// ======================================================
// Configure Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Service API v1");
    c.RoutePrefix = string.Empty;
    c.DocumentTitle = "Account Service Documentation";
    c.EnableDeepLinking();
    c.DisplayRequestDuration();
});

// Configure HTTP pipeline

app.UseHttpsRedirection();
app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Configure endpoints
app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            HealthChecks = report.Entries.Select(e => new
            {
                Component = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// Start the application
logger.LogInformation("AccountService started successfully");
app.Run();
