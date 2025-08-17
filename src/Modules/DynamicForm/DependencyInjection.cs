using CoreAxis.Modules.DynamicForm.Application.Interfaces;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.Modules.DynamicForm.Infrastructure.Persistence;
using CoreAxis.Modules.DynamicForm.Infrastructure.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CoreAxis.Modules.DynamicForm;

public static class DependencyInjection
{
    public static IServiceCollection AddDynamicFormModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<DynamicFormDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Add Repositories
        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
        services.AddScoped<IFormStepRepository, FormStepRepository>();
        services.AddScoped<IFormStepSubmissionRepository, FormStepSubmissionRepository>();
        services.AddScoped<IFormVersionRepository, FormVersionRepository>();
        services.AddScoped<IFormAccessPolicyRepository, FormAccessPolicyRepository>();
        services.AddScoped<IFormAuditLogRepository, FormAuditLogRepository>();
        services.AddScoped<IFormulaDefinitionRepository, FormulaDefinitionRepository>();
        services.AddScoped<IFormulaVersionRepository, FormulaVersionRepository>();
        services.AddScoped<IFormulaEvaluationLogRepository, FormulaEvaluationLogRepository>();

        // Add Domain Services
        services.AddScoped<IFormSchemaValidator, FormSchemaValidator>();
        services.AddScoped<IExpressionEngine, ExpressionEngine>();
        services.AddScoped<IDependencyGraphService, DependencyGraphService>();
        services.AddScoped<IValidationEngine, ValidationEngine>();
        services.AddScoped<IDynamicOptionsManager, DynamicOptionsManager>();

        // Add Application Services
        services.AddScoped<IFormService, FormService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IFormVersionService, FormVersionService>();
        services.AddScoped<IFormAccessService, FormAccessService>();
        services.AddScoped<IFormAuditService, FormAuditService>();
        services.AddScoped<IFormAnalyticsService, FormAnalyticsService>();
        services.AddScoped<IFormExportService, FormExportService>();
        services.AddScoped<IFormImportService, FormImportService>();
        services.AddScoped<IFormTemplateService, FormTemplateService>();
        services.AddScoped<IFormWorkflowService, FormWorkflowService>();
        services.AddScoped<IFormNotificationService, FormNotificationService>();
        services.AddScoped<IFormCacheService, FormCacheService>();
        services.AddScoped<IFormSecurityService, FormSecurityService>();
        services.AddScoped<IFormIntegrationService, FormIntegrationService>();
        services.AddScoped<IFormulaService, FormulaService>();

        // Add Infrastructure Services
        services.AddScoped<IApiManager, ApiManager>();
        services.AddScoped<IFormFileService, FormFileService>();
        services.AddScoped<IFormEmailService, FormEmailService>();
        services.AddScoped<IFormSmsService, FormSmsService>();
        services.AddScoped<IFormPdfService, FormPdfService>();
        services.AddScoped<IFormExcelService, FormExcelService>();
        services.AddScoped<IFormBackupService, FormBackupService>();
        services.AddScoped<IFormMigrationService, FormMigrationService>();
        services.AddScoped<IFormPerformanceService, FormPerformanceService>();
        services.AddScoped<IFormMonitoringService, FormMonitoringService>();

        // Add Caching
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // Add Background Services
        services.AddHostedService<FormCleanupService>();
        services.AddHostedService<FormAnalyticsProcessorService>();
        services.AddHostedService<FormNotificationProcessorService>();
        services.AddHostedService<FormBackupSchedulerService>();

        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add Configuration Options
        services.Configure<DynamicFormOptions>(configuration.GetSection("DynamicForm"));
        services.Configure<FormValidationOptions>(configuration.GetSection("DynamicForm:Validation"));
        services.Configure<FormCacheOptions>(configuration.GetSection("DynamicForm:Cache"));
        services.Configure<FormSecurityOptions>(configuration.GetSection("DynamicForm:Security"));
        services.Configure<FormIntegrationOptions>(configuration.GetSection("DynamicForm:Integration"));
        services.Configure<FormNotificationOptions>(configuration.GetSection("DynamicForm:Notification"));
        services.Configure<FormBackupOptions>(configuration.GetSection("DynamicForm:Backup"));
        services.Configure<FormPerformanceOptions>(configuration.GetSection("DynamicForm:Performance"));

        // Add Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<DynamicFormDbContext>("dynamicform-db")
            .AddCheck<FormServiceHealthCheck>("dynamicform-service")
            .AddCheck<FormCacheHealthCheck>("dynamicform-cache")
            .AddCheck<FormIntegrationHealthCheck>("dynamicform-integration");

        // Add Swagger Documentation
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CoreAxis Dynamic Form API",
                Version = "v1",
                Description = "API for managing dynamic forms and submissions",
                Contact = new OpenApiContact
                {
                    Name = "CoreAxis Team",
                    Email = "support@coreaxis.com"
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Add security definitions
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

        return services;
    }

    public static IApplicationBuilder UseDynamicFormModule(this IApplicationBuilder app)
    {
        // Ensure database is created and migrated
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DynamicFormDbContext>();
            context.Database.EnsureCreated();
        }

        return app;
    }
}

// Configuration Options Classes
public class DynamicFormOptions
{
    public int MaxFormSize { get; set; } = 1000; // KB
    public int MaxSubmissionSize { get; set; } = 5000; // KB
    public int MaxFieldsPerForm { get; set; } = 100;
    public int MaxSubmissionsPerForm { get; set; } = 10000;
    public bool EnableAuditLog { get; set; } = true;
    public bool EnableVersioning { get; set; } = true;
    public bool EnableCaching { get; set; } = true;
    public bool EnableCompression { get; set; } = true;
    public bool EnableEncryption { get; set; } = false;
    public string DefaultLanguage { get; set; } = "en";
    public string[] SupportedLanguages { get; set; } = { "en", "fa" };
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public string CurrencyFormat { get; set; } = "C";
    public string NumberFormat { get; set; } = "N2";
}

public class FormValidationOptions
{
    public bool EnableStrictValidation { get; set; } = true;
    public bool EnableAsyncValidation { get; set; } = true;
    public bool EnableCrossFieldValidation { get; set; } = true;
    public bool EnableCustomValidators { get; set; } = true;
    public int MaxValidationErrors { get; set; } = 50;
    public int ValidationTimeout { get; set; } = 30; // seconds
    public bool CacheValidationResults { get; set; } = true;
    public int ValidationCacheExpiry { get; set; } = 300; // seconds
}

public class FormCacheOptions
{
    public bool EnableDistributedCache { get; set; } = true;
    public bool EnableMemoryCache { get; set; } = true;
    public int DefaultCacheExpiry { get; set; } = 3600; // seconds
    public int FormCacheExpiry { get; set; } = 1800; // seconds
    public int SchemaCacheExpiry { get; set; } = 3600; // seconds
    public int SubmissionCacheExpiry { get; set; } = 300; // seconds
    public string CacheKeyPrefix { get; set; } = "DF:";
    public bool EnableCacheCompression { get; set; } = true;
}

public class FormSecurityOptions
{
    public bool EnableEncryption { get; set; } = false;
    public bool EnableFieldLevelSecurity { get; set; } = true;
    public bool EnableAccessControl { get; set; } = true;
    public bool EnableAuditTrail { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public int MaxRequestsPerMinute { get; set; } = 100;
    public bool EnableCsrfProtection { get; set; } = true;
    public bool EnableXssProtection { get; set; } = true;
    public string[] AllowedFileTypes { get; set; } = { ".pdf", ".doc", ".docx", ".jpg", ".png", ".gif" };
    public int MaxFileSize { get; set; } = 10; // MB
}

public class FormIntegrationOptions
{
    public bool EnableWebhooks { get; set; } = true;
    public bool EnableApiIntegration { get; set; } = true;
    public bool EnableEmailIntegration { get; set; } = true;
    public bool EnableSmsIntegration { get; set; } = false;
    public int WebhookTimeout { get; set; } = 30; // seconds
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public string[] TrustedDomains { get; set; } = Array.Empty<string>();
}

public class FormNotificationOptions
{
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnableSmsNotifications { get; set; } = false;
    public bool EnablePushNotifications { get; set; } = false;
    public bool EnableInAppNotifications { get; set; } = true;
    public string DefaultFromEmail { get; set; } = "noreply@coreaxis.com";
    public string DefaultFromName { get; set; } = "CoreAxis";
    public int NotificationBatchSize { get; set; } = 100;
    public int NotificationRetryAttempts { get; set; } = 3;
}

public class FormBackupOptions
{
    public bool EnableAutoBackup { get; set; } = true;
    public string BackupSchedule { get; set; } = "0 2 * * *"; // Daily at 2 AM
    public int BackupRetentionDays { get; set; } = 30;
    public string BackupStoragePath { get; set; } = "./backups/forms";
    public bool EnableCloudBackup { get; set; } = false;
    public string CloudBackupProvider { get; set; } = "Azure";
    public bool CompressBackups { get; set; } = true;
    public bool EncryptBackups { get; set; } = true;
}

public class FormPerformanceOptions
{
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public bool EnableQueryOptimization { get; set; } = true;
    public bool EnableConnectionPooling { get; set; } = true;
    public int QueryTimeout { get; set; } = 30; // seconds
    public int MaxConcurrentRequests { get; set; } = 100;
    public int MaxDegreeOfParallelism { get; set; } = 4;
    public bool EnableAsyncProcessing { get; set; } = true;
    public int BatchSize { get; set; } = 1000;
}