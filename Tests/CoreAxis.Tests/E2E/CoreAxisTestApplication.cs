using CoreAxis.Modules.ApiManager.Infrastructure;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.Modules.MappingModule.Infrastructure.Data;
using CoreAxis.Modules.ProductBuilderModule.Infrastructure.Data;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.Modules.SecretsModule.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CoreAxis.Tests.E2E;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) 
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] 
        { 
            new Claim(ClaimTypes.Name, "TestUser"), 
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("tenant_id", "default")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class CoreAxisTestApplication : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    public CoreAxisTestApplication()
    {
        // Set environment variables required for Program.cs startup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "ThisIsASuperSecretKeyForTestingOnly12345!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "CoreAxisTest");
        Environment.SetEnvironmentVariable("Jwt__Audience", "CoreAxisTest");
        Environment.SetEnvironmentVariable("Secrets__EncryptionKey", "TestEncryptionKey1234567890123456");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "DataSource=:memory:");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ThisIsASuperSecretKeyForTestingOnly12345!",
                ["Jwt:Issuer"] = "CoreAxisTest",
                ["Jwt:Audience"] = "CoreAxisTest",
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["Secrets:EncryptionKey"] = "TestEncryptionKey1234567890123456"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            RemoveDbContext<AuthDbContext>(services);
            RemoveDbContext<ProductBuilderDbContext>(services);
            RemoveDbContext<WorkflowDbContext>(services);
            RemoveDbContext<DynamicFormDbContext>(services);
            RemoveDbContext<ApiManagerDbContext>(services);
            RemoveDbContext<MappingDbContext>(services);
            RemoveDbContext<SecretsDbContext>(services);

            // Add In-Memory DbContexts
            // var dbName = Guid.NewGuid().ToString(); // Removed local variable
            
            services.AddDbContext<AuthDbContext>(options => 
                options.UseInMemoryDatabase($"Auth_{_dbName}"));
                
            services.AddDbContext<ProductBuilderDbContext>(options => 
                options.UseInMemoryDatabase($"Product_{_dbName}"));
                
            services.AddDbContext<WorkflowDbContext>(options => 
                options.UseInMemoryDatabase($"Workflow_{_dbName}"));
                
            services.AddDbContext<DynamicFormDbContext>(options => 
                options.UseInMemoryDatabase($"Form_{_dbName}"));
                
            services.AddDbContext<ApiManagerDbContext>(options => 
                options.UseInMemoryDatabase($"ApiManager_{_dbName}"));
                
            services.AddDbContext<MappingDbContext>(options => 
                options.UseInMemoryDatabase($"Mapping_{_dbName}"));

            services.AddDbContext<SecretsDbContext>(options => 
                options.UseInMemoryDatabase($"Secrets_{_dbName}"));

            // Register Test Authentication Handler
            services.AddAuthentication(options => 
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });


        builder.ConfigureLogging(logging => 
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
    }

    private void RemoveDbContext<T>(IServiceCollection services) where T : DbContext
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<T>));
        if (descriptor != null) services.Remove(descriptor);
        
        var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (contextDescriptor != null) services.Remove(contextDescriptor);
    }
}
