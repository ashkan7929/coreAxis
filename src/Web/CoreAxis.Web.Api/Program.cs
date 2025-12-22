using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel.Observability;
using CoreAxis.Modules.WalletModule.Api;
using CoreAxis.Modules.ProductOrderModule.Api;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

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

// Add Entity Framework
builder.Services.AddDbContext<DynamicFormDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Observability: register ProblemDetails services (extensible/no-op for now)
builder.Services.AddCoreAxisProblemDetails();

// Add WalletModule
builder.Services.AddWalletModuleApi(builder.Configuration);

// Add ProductOrderModule (Product API)
builder.Services.AddProductOrderModuleApi(builder.Configuration);
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