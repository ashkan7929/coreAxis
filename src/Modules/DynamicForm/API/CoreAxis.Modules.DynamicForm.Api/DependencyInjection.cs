using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Application.Services.Handlers; // Added
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.Modules.DynamicForm.Infrastructure.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Services;
using CoreAxis.SharedKernel.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace CoreAxis.Modules.DynamicForm;

public static class DependencyInjection
{
    public static IServiceCollection AddDynamicFormModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<DynamicFormDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                sql.CommandTimeout(60);
            }));

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Add Repositories
        services.AddScoped<IFormRepository, FormRepository>();
        services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
        services.AddScoped<IFormStepRepository, FormStepRepository>();
        services.AddScoped<IFormStepSubmissionRepository, FormStepSubmissionRepository>();
        // services.AddScoped<IFormVersionRepository, FormVersionRepository>();
        // services.AddScoped<IFormAccessPolicyRepository, FormAccessPolicyRepository>(); // Missing?
        // services.AddScoped<IFormAuditLogRepository, FormAuditLogRepository>(); // Missing?
        services.AddScoped<IFormulaDefinitionRepository, FormulaDefinitionRepository>();
        services.AddScoped<IFormulaVersionRepository, FormulaVersionRepository>();
        services.AddScoped<IFormulaEvaluationLogRepository, FormulaEvaluationLogRepository>();

        // Add Domain Services
        services.AddScoped<IFormSchemaValidator, FormSchemaValidator>();
        services.AddScoped<IExpressionEngine, ExpressionEngine>();
        services.AddScoped<IDependencyGraph, DependencyGraph>();
        services.AddScoped<IValidationEngine, ValidationEngine>();
        services.AddScoped<IDynamicOptionsManager, DynamicOptionsManager>();

        // Add Application Services
        // services.AddScoped<IFormService, FormService>();
        // services.AddScoped<ISubmissionService, SubmissionService>();
        // services.AddScoped<IFormVersionService, FormVersionService>();
        // services.AddScoped<IFormAccessService, FormAccessService>();
        // services.AddScoped<IFormAuditService, FormAuditService>();
        // services.AddScoped<IFormAnalyticsService, FormAnalyticsService>();
        // services.AddScoped<IFormExportService, FormExportService>();
        // services.AddScoped<IFormImportService, FormImportService>();
        // services.AddScoped<IFormTemplateService, FormTemplateService>();
        // services.AddScoped<IFormWorkflowService, FormWorkflowService>();
        // services.AddScoped<IFormNotificationService, FormNotificationService>();
        // services.AddScoped<IFormCacheService, FormCacheService>();
        // services.AddScoped<IFormSecurityService, FormSecurityService>();
        // services.AddScoped<IFormIntegrationService, FormIntegrationService>();
        services.AddScoped<IFormulaService, FormulaService>();
        services.AddScoped<IDataOrchestrator, DataOrchestrator>();
        services.AddScoped<IRoundingPolicy, RoundingPolicy>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddSingleton<IFormEventManager, FormEventManager>();
        services.AddScoped<IFormEventHandler, DefaultFormEventHandler>();
        services.AddScoped<IIncrementalRecalculationEngine, IncrementalRecalculationEngine>();

        // Add Infrastructure Services
        // services.AddScoped<IApiManager, ApiManager>();
        // services.AddScoped<IFormFileService, FormFileService>();
        // services.AddScoped<IFormEmailService, FormEmailService>();
        // services.AddScoped<IFormSmsService, FormSmsService>();
        // services.AddScoped<IFormPdfService, FormPdfService>();
        // services.AddScoped<IFormExcelService, FormExcelService>();
        // services.AddScoped<IFormBackupService, FormBackupService>();
        // services.AddScoped<IFormMigrationService, FormMigrationService>();
        // services.AddScoped<IFormPerformanceService, FormPerformanceService>();
        // services.AddScoped<IFormMonitoringService, FormMonitoringService>();
        
        // Add SharedKernel Clients
        services.AddScoped<IFormClient, FormClient>();
        services.AddScoped<IFormulaClient, FormulaClient>();

        // Add Caching
        services.AddMemoryCache();
        /*
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        */

        // Add Background Services
        /*
        services.AddHostedService<FormCleanupService>();
        services.AddHostedService<FormAnalyticsProcessorService>();
        services.AddHostedService<FormNotificationProcessorService>();
        services.AddHostedService<FormBackupSchedulerService>();
        */

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
