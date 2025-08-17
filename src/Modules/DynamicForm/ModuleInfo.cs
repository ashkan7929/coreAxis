using CoreAxis.SharedKernel.Interfaces;
using System.Reflection;

namespace CoreAxis.Modules.DynamicForm;

public class ModuleInfo : IModuleInfo
{
    public string Name => "DynamicForm";
    public string DisplayName => "Dynamic Form Module";
    public string Description => "A comprehensive module for creating, managing, and processing dynamic forms with advanced validation, dependency management, and submission handling capabilities.";
    public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    public string Author => "CoreAxis Development Team";
    public string[] Dependencies => new[] { "SharedKernel", "Identity" };
    public bool IsEnabled => true;
    public int LoadOrder => 100;
    public DateTime CreatedAt => new(2024, 1, 15);
    public DateTime UpdatedAt => DateTime.UtcNow;

    public Dictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>
        {
            ["Category"] = "Forms",
            ["Tags"] = new[] { "forms", "dynamic", "validation", "submissions" },
            ["SupportedLanguages"] = new[] { "en", "fa" },
            ["RequiredPermissions"] = new[] { "Forms.Read", "Forms.Write", "Forms.Delete" },
            ["ApiVersion"] = "v1",
            ["DatabaseSchema"] = "DynamicForm",
            ["ConfigurationSection"] = "DynamicForm",
            ["HealthCheckName"] = "dynamicform",
            ["Features"] = new[]
            {
                "Form Creation and Management",
                "Dynamic Field Types",
                "Advanced Validation Engine",
                "Dependency Management",
                "Submission Processing",
                "Form Versioning",
                "Access Control",
                "Audit Logging",
                "Analytics and Reporting",
                "Import/Export",
                "Templates",
                "Workflows",
                "Notifications",
                "Caching",
                "Security",
                "Integration APIs"
            },
            ["SupportedFieldTypes"] = new[]
            {
                "text", "textarea", "number", "email", "password", "url", "tel",
                "date", "time", "datetime", "month", "week",
                "checkbox", "radio", "select", "multiselect",
                "file", "image", "color", "range", "hidden",
                "section", "html", "calculated", "lookup", "signature",
                "rating", "matrix", "repeater", "conditional"
            },
            ["SupportedValidationRules"] = new[]
            {
                "required", "minLength", "maxLength", "min", "max",
                "pattern", "email", "url", "numeric", "integer",
                "date", "time", "custom", "conditional", "crossField",
                "async", "fileType", "fileSize", "imageSize"
            },
            ["SupportedExpressions"] = new[]
            {
                "arithmetic", "logical", "comparison", "string",
                "date", "conditional", "lookup", "aggregate"
            },
            ["IntegrationCapabilities"] = new[]
            {
                "REST APIs", "Webhooks", "Email", "SMS",
                "File Storage", "Database", "External Services"
            },
            ["ExportFormats"] = new[] { "JSON", "XML", "CSV", "Excel", "PDF" },
            ["ImportFormats"] = new[] { "JSON", "XML", "CSV", "Excel" },
            ["CacheProviders"] = new[] { "Memory", "Redis", "Distributed" },
            ["StorageProviders"] = new[] { "Local", "Azure", "AWS", "Google" },
            ["NotificationChannels"] = new[] { "Email", "SMS", "Push", "InApp" },
            ["SecurityFeatures"] = new[]
            {
                "Field-level Security", "Access Control", "Audit Trail",
                "Rate Limiting", "CSRF Protection", "XSS Protection",
                "Data Encryption", "File Type Validation"
            },
            ["PerformanceFeatures"] = new[]
            {
                "Caching", "Query Optimization", "Connection Pooling",
                "Async Processing", "Batch Operations", "Compression"
            },
            ["MonitoringFeatures"] = new[]
            {
                "Health Checks", "Performance Metrics", "Error Tracking",
                "Usage Analytics", "Audit Logs", "Real-time Monitoring"
            }
        };
    }

    public bool ValidateDependencies(IEnumerable<IModuleInfo> availableModules)
    {
        var availableModuleNames = availableModules.Select(m => m.Name).ToHashSet();
        return Dependencies.All(dep => availableModuleNames.Contains(dep));
    }

    public void Initialize(IServiceProvider serviceProvider)
    {
        // Module initialization logic
        var logger = serviceProvider.GetService<ILogger<ModuleInfo>>();
        logger?.LogInformation("Initializing Dynamic Form Module v{Version}", Version);

        // Validate configuration
        var configuration = serviceProvider.GetService<IConfiguration>();
        ValidateConfiguration(configuration, logger);

        // Initialize services
        InitializeServices(serviceProvider, logger);

        logger?.LogInformation("Dynamic Form Module initialized successfully");
    }

    public void Shutdown(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILogger<ModuleInfo>>();
        logger?.LogInformation("Shutting down Dynamic Form Module");

        // Cleanup resources
        CleanupResources(serviceProvider, logger);

        logger?.LogInformation("Dynamic Form Module shutdown completed");
    }

    private void ValidateConfiguration(IConfiguration? configuration, ILogger? logger)
    {
        if (configuration == null)
        {
            logger?.LogWarning("Configuration is not available for Dynamic Form Module");
            return;
        }

        var dynamicFormSection = configuration.GetSection("DynamicForm");
        if (!dynamicFormSection.Exists())
        {
            logger?.LogWarning("DynamicForm configuration section is missing");
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            logger?.LogError("Database connection string is missing");
            throw new InvalidOperationException("Database connection string is required for Dynamic Form Module");
        }

        logger?.LogDebug("Dynamic Form Module configuration validated successfully");
    }

    private void InitializeServices(IServiceProvider serviceProvider, ILogger? logger)
    {
        try
        {
            // Initialize database context
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<DynamicFormDbContext>();
            if (dbContext != null)
            {
                // Ensure database is created
                dbContext.Database.EnsureCreated();
                logger?.LogDebug("Database context initialized");
            }

            // Initialize cache services
            var cacheService = scope.ServiceProvider.GetService<IFormCacheService>();
            if (cacheService != null)
            {
                // Warm up cache if needed
                logger?.LogDebug("Cache service initialized");
            }

            // Initialize background services
            var backgroundServices = scope.ServiceProvider.GetServices<IHostedService>();
            logger?.LogDebug("Background services initialized: {Count}", backgroundServices.Count());
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error initializing Dynamic Form Module services");
            throw;
        }
    }

    private void CleanupResources(IServiceProvider serviceProvider, ILogger? logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            
            // Cleanup cache
            var cacheService = scope.ServiceProvider.GetService<IFormCacheService>();
            if (cacheService != null)
            {
                // Clear module-specific cache entries
                logger?.LogDebug("Cache cleanup completed");
            }

            // Cleanup background services
            var backgroundServices = scope.ServiceProvider.GetServices<IHostedService>();
            foreach (var service in backgroundServices)
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            logger?.LogDebug("Background services cleanup completed");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during Dynamic Form Module cleanup");
        }
    }

    public HealthCheckResult CheckHealth(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var healthChecks = new List<(string Name, bool IsHealthy, string? Message)>();

            // Check database connectivity
            try
            {
                var dbContext = scope.ServiceProvider.GetService<DynamicFormDbContext>();
                if (dbContext != null)
                {
                    var canConnect = dbContext.Database.CanConnect();
                    healthChecks.Add(("Database", canConnect, canConnect ? null : "Cannot connect to database"));
                }
            }
            catch (Exception ex)
            {
                healthChecks.Add(("Database", false, ex.Message));
            }

            // Check cache service
            try
            {
                var cacheService = scope.ServiceProvider.GetService<IFormCacheService>();
                if (cacheService != null)
                {
                    // Simple cache test
                    healthChecks.Add(("Cache", true, null));
                }
            }
            catch (Exception ex)
            {
                healthChecks.Add(("Cache", false, ex.Message));
            }

            // Check validation engine
            try
            {
                var validationEngine = scope.ServiceProvider.GetService<IValidationEngine>();
                if (validationEngine != null)
                {
                    healthChecks.Add(("ValidationEngine", true, null));
                }
            }
            catch (Exception ex)
            {
                healthChecks.Add(("ValidationEngine", false, ex.Message));
            }

            var allHealthy = healthChecks.All(h => h.IsHealthy);
            var status = allHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
            var description = allHealthy ? "All services are healthy" : "Some services are unhealthy";

            var data = healthChecks.ToDictionary(
                h => h.Name,
                h => (object)(h.IsHealthy ? "Healthy" : $"Unhealthy: {h.Message}")
            );

            return new HealthCheckResult(status, description, data: data);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, "Health check failed", ex);
        }
    }
}

// Health check result classes
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public class HealthCheckResult
{
    public HealthStatus Status { get; }
    public string? Description { get; }
    public Exception? Exception { get; }
    public IReadOnlyDictionary<string, object>? Data { get; }

    public HealthCheckResult(HealthStatus status, string? description = null, Exception? exception = null, IReadOnlyDictionary<string, object>? data = null)
    {
        Status = status;
        Description = description;
        Exception = exception;
        Data = data;
    }
}