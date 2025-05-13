using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text;
using CommonLib.Services;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IdentityModel.Tokens.Jwt;
using NotificationService.Services;
using CommonLib.Models.Notification;
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

// Register Notification-specific services
builder.Services.AddSingleton<WebSocketService>();
builder.Services.AddScoped<INotificationService, NotificationService.Services.NotificationService>();

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

    // For WebSocket authentication, token might be passed in query string.
    // This can be handled in options.Events.OnMessageReceived or by manually validating the token in WebSocket middleware.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws")) // Or your specific WebSocket path
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification Service API", Version = "v1" });

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
logger.LogInformation("NotificationService starting up...");

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
// MIDDLEWARE CONFIGURATION
// ======================================================
// Configure Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1");
    c.RoutePrefix = string.Empty;
    c.DocumentTitle = "Notification Service Documentation";
    c.EnableDeepLinking();
    c.DisplayRequestDuration();
});

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.UseHttpsRedirection();
app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Configure endpoints
app.MapControllers();
app.MapHealthChecks("/health");

// Start the application
logger.LogInformation("NotificationService started successfully");
app.Run();
