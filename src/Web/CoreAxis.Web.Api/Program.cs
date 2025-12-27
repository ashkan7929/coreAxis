using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.Modules.DynamicForm;
using CoreAxis.SharedKernel.Observability;
// using CoreAxis.SharedKernel.Eventing; 
using CoreAxis.Modules.ProductOrderModule.Api;
using CoreAxis.Modules.WalletModule.Api;
using CoreAxis.Modules.AuthModule.API;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load .env
string? loadedEnvPath = null;
var currentDir = Directory.GetCurrentDirectory();
var candidateEnvPaths = new List<string>
{
    Path.Combine(currentDir, ".env"),
    Path.Combine(AppContext.BaseDirectory, ".env")
};
var parent = Directory.GetParent(currentDir);
for (int i = 0; i < 5; i++)
{
    if (parent == null) break;
    candidateEnvPaths.Add(Path.Combine(parent.FullName, ".env"));
    parent = parent.Parent;
}
foreach (var candidate in candidateEnvPaths.Distinct())
{
    if (File.Exists(candidate))
    {
        Env.Load(candidate);
        loadedEnvPath = candidate;
        break;
    }
}
Console.WriteLine(loadedEnvPath != null ? $"[Startup] .env loaded from: {loadedEnvPath}" : "[Startup] .env not found");

builder.Configuration.AddEnvironmentVariables();
ExpandEnvironmentVariables(builder.Configuration);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("webApi-v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CoreAxis Web API",
        Version = "v1",
        Description = "Main Web API for CoreAxis application"
    });

    // Include all controllers in the main doc
    c.DocInclusionPredicate((docName, apiDesc) => true);

    // Add JWT security to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments for all loaded assemblies to enrich Swagger docs
    var basePath = AppContext.BaseDirectory;
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    foreach (var asm in assemblies)
    {
        var xml = Path.Combine(basePath, $"{asm.GetName().Name}.xml");
        if (File.Exists(xml))
        {
            c.IncludeXmlComments(xml);
        }
    }
});

// Add DynamicForm Module
builder.Services.AddDynamicFormModule(builder.Configuration);

// Observability: register ProblemDetails services (extensible/no-op for now)
builder.Services.AddCoreAxisProblemDetails();
// builder.Services.AddCoreAxisEventBus(); 
builder.Services.AddSingleton<CoreAxis.EventBus.IEventBus, CoreAxis.EventBus.InMemoryEventBus>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdAccessor, HttpContextCorrelationIdAccessor>();

// Add WalletModule
builder.Services.AddWalletModuleApi(builder.Configuration);

// Add AuthModule
CoreAxis.Modules.AuthModule.API.DependencyInjection.AddAuthModuleApi(builder.Services, builder.Configuration, builder.Environment);

// Add ProductOrderModule (Product API)
builder.Services.AddProductOrderModuleApi(builder.Configuration);

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                jwtSettings["SecretKey"] 
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                ?? "CoreAxisSecretKey1234567890123456")) // Fallback
    };
});

// Register file-based Audit Store for admin audit API
builder.Services.AddSingleton<CoreAxis.SharedKernel.Observability.Audit.IAuditStore>(sp =>
    new CoreAxis.SharedKernel.Observability.Audit.AuditFileStore(
        Path.Combine(AppContext.BaseDirectory, "App_Data", "audit"))
);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("webApi-v1/swagger.json", "CoreAxis Web API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
// Insert Correlation middleware early so headers/context propagate to downstream components
app.UseCoreAxisCorrelation();
// Map exceptions uniformly to RFC7807 Problem+JSON with correlation
app.UseCoreAxisProblemDetails();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static void ExpandEnvironmentVariables(IConfiguration configuration)
{
    var connectionStringsSection = configuration.GetSection("ConnectionStrings");
    if (connectionStringsSection.Exists())
    {
        ExpandSection(connectionStringsSection, new[] { "DefaultConnection" });
    }

    var fanavaranSection = configuration.GetSection("Fanavaran");
    if (fanavaranSection.Exists())
    {
        ExpandSection(fanavaranSection, new[] { "BaseUrl", "AppName", "Secret", "Username", "Password", "AuthorizationHeader", "Location", "CorpId", "ContractId" });
    }

    var jwtSection = configuration.GetSection("Jwt");
    if (jwtSection.Exists())
    {
        ExpandSection(jwtSection, new[] { "SecretKey", "Issuer", "Audience" });
    }
}

static void ExpandSection(IConfigurationSection section, string[] keys)
{
    foreach (var key in keys)
    {
        var value = section[key];
        if (!string.IsNullOrEmpty(value))
        {
            // First try standard expansion (works for %VAR% on Windows, $VAR on Linux/Mac sometimes)
            var expanded = Environment.ExpandEnvironmentVariables(value);
            
            // If explicit ${VAR} syntax is used (common in appsettings templates), handle it manually
            if (expanded.Contains("${"))
            {
                expanded = System.Text.RegularExpressions.Regex.Replace(expanded, @"\$\{(\w+)\}", match =>
                {
                    var envVar = Environment.GetEnvironmentVariable(match.Groups[1].Value);
                    return envVar ?? match.Value;
                });
            }
            
            section[key] = expanded;
        }
    }
}
