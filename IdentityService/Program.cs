using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using CommonLib.Services;
using IdentityService.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IdentityModel.Tokens.Jwt;
using CommonLib.Models.Identity;

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
builder.Services.AddSingleton<ILoggerService, LoggerService>();
builder.Services.AddScoped<IApiLoggingService, ApiLoggingService>();

// Register MongoDB connection factory
builder.Services.AddSingleton<MongoDbConnectionFactory>();

// Add database initialization service
builder.Services.AddSingleton<DatabaseInitializationService>();

// Register Identity-specific services and repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISecurityTokenRepository, SecurityTokenRepository>();

// Register HttpClientService for inter-service communication
builder.Services.AddHttpClient();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// ======================================================
// JWT AUTHENTICATION CONFIGURATION
// ======================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "TradingSystem_DefaultKey";
var issuer = jwtSettings["Issuer"] ?? "TradingSystem";
var audience = jwtSettings["Audience"] ?? "TradingSystemClients";

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

// Configure JWT authentication explicitly for easier debugging
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configure token validation parameters
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Sub, // Use 'sub' for the user identifier
        RoleClaimType = ClaimTypes.Role
    };

    // Save for diagnostic purposes
    options.SaveToken = true;

    // Make sure token is received properly
    options.RequireHttpsMetadata = false;

    // Add debugging event handlers to diagnose JWT processing
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerService>();
            logger.LogDebug("JWT OnMessageReceived event fired");

            var token = context.Token;
            if (string.IsNullOrEmpty(token))
            {
                logger.LogDebug("No token found in the request");
                var authHeader = context.Request.Headers["Authorization"].ToString();
                logger.LogDebug($"Authorization header: {(string.IsNullOrEmpty(authHeader) ? "Not present" : authHeader)}");

                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                    context.Token = token;
                    logger.LogDebug($"Extracted token from Authorization header: {token.Substring(0, Math.Min(20, token.Length))}...");
                }
            }
            else
            {
                logger.LogDebug($"Token found in context: {token.Substring(0, Math.Min(20, token.Length))}...");
            }

            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerService>();
            logger.LogDebug("JWT OnTokenValidated event fired - Token is valid");

            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                logger.LogDebug($"JWT Token validated for user: {identity.Name}");

                // Ensure the principal has a 'sub' claim that can be used for UserId
                var sub = identity.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                logger.LogDebug($"Subject claim: {sub ?? "not found"}");

                foreach (var claim in identity.Claims)
                {
                    logger.LogDebug($"Claim: {claim.Type}={claim.Value}");
                }
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerService>();
            logger.LogWarning($"JWT Authentication failed: {context.Exception.Message}");
            logger.LogDebug($"Exception details: {context.Exception}");
            return Task.CompletedTask;
        },

        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerService>();
            logger.LogDebug("JWT OnChallenge event fired - Authentication challenge issued");
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity Service API", Version = "v1" });

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
logger.LogInformation("IdentityService starting up...");

// ======================================================
// DATABASE INITIALIZATION
// ======================================================
try
{
    logger.LogInformation("Initializing database connection...");
    var dbFactory = app.Services.GetRequiredService<MongoDbConnectionFactory>();
    var testCollection = dbFactory.GetCollection<User>();
    // Just test that we can access a collection
    var count = await testCollection.CountDocumentsAsync(Builders<User>.Filter.Empty);
    logger.LogInformation("Connected to MongoDB successfully.");
}
catch (Exception ex)
{
    logger.LogCritical($"Error connecting to MongoDB: {ex.Message}");
    logger.LogCritical("Application is shutting down due to database connection failure");
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service API v1");
    c.RoutePrefix = string.Empty;
    c.DocumentTitle = "Identity Service Documentation";
    c.EnableDeepLinking();
    c.DisplayRequestDuration();
});

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Configure endpoints
app.MapControllers();
app.MapHealthChecks("/health");

// Start the application
logger.LogInformation("IdentityService started successfully");
app.Run();
