using System;
using System.IO;
using MarketDataService.Repositories;
using MarketDataService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using CommonLib.Services;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Security.Claims;
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

// Register Market-specific repositories
builder.Services.AddScoped<IOrderBookRepository, OrderBookRepository>();
builder.Services.AddScoped<ISymbolRepository, SymbolRepository>();
builder.Services.AddScoped<IKlineRepository, KlineRepository>();
builder.Services.AddScoped<ITradeRepository, TradeRepository>();
builder.Services.AddScoped<IMarketDataRepository, MarketDataRepository>();

// Register Market-specific services
builder.Services.AddScoped<IMarketService, MarketService>();
builder.Services.AddScoped<IKlineService, KlineService>();

// Register HttpClientService for inter-service communication
builder.Services.AddHttpClient();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// Register CommonLib/Api services for inter-service communication
builder.Services.AddTradingSystemServices();

// Add WebSocketService
builder.Services.AddSingleton<IWebSocketService, WebSocketService>();

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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Market Data Service API", Version = "v1" });

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
logger.LogInformation("MarketDataService starting up...");

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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Market Data Service API v1");
    c.RoutePrefix = string.Empty;
    c.DocumentTitle = "Market Data Service Documentation";
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

// ======================================================
// CREATE DEFAULT SYMBOLS
// ======================================================
try
{
    logger.LogInformation("Checking for default symbols...");

    // Get symbol repository from DI
    using var scope = app.Services.CreateScope();
    var symbolRepository = scope.ServiceProvider.GetRequiredService<ISymbolRepository>();

    // Define default symbols
    var defaultSymbols = new List<(string Name, string BaseAsset, string QuoteAsset)>
    {
        ("BTC-USDT", "BTC", "USDT"),
        ("ETH-USDT", "ETH", "USDT"),
        ("ETH-BTC", "ETH", "BTC")
    };

    foreach (var symbolInfo in defaultSymbols)
    {
        // Check if symbol already exists
        var existingSymbol = await symbolRepository.GetSymbolByNameAsync(symbolInfo.Name);
        if (existingSymbol == null)
        {
            // Create new symbol
            var symbol = new CommonLib.Models.Market.Symbol
            {
                Name = symbolInfo.Name,
                BaseAsset = symbolInfo.BaseAsset,
                QuoteAsset = symbolInfo.QuoteAsset,
                BaseAssetPrecision = 8,
                QuotePrecision = 8,
                MinPrice = 0.00000001m,
                MaxPrice = 1000000m,
                TickSize = 0.00000001m,
                MinQty = 0.00000001m,
                MaxQty = 1000000m,
                StepSize = 0.00000001m,
                IsActive = true,
                MinOrderSize = 0.0001m,
                MaxOrderSize = 100000m,
                TakerFee = 0.001m,
                MakerFee = 0.0005m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdSymbol = await symbolRepository.CreateSymbolAsync(symbol);
            logger.LogInformation($"Created default symbol: {createdSymbol.Name}");
        }
        else
        {
            logger.LogInformation($"Default symbol already exists: {existingSymbol.Name}");
        }
    }

    logger.LogInformation("Default symbols check completed");
}
catch (Exception ex)
{
    logger.LogError($"Error creating default symbols: {ex.Message}");
}

// Start the application
logger.LogInformation("MarketDataService started successfully");
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
