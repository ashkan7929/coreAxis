using CoreAxis.BuildingBlocks;
using CoreAxis.Modules.DynamicForm;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.DynamicForm.Api;

public class DynamicFormModule : IModule
{
    public string Name => "DynamicForm";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services)
    {
        // We need configuration to register DynamicForm services
        // But IModule.RegisterServices only provides IServiceCollection.
        // We can build a temporary service provider to get configuration, 
        // or assume IConfiguration is available in services (it usually is).
        
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();
        
        if (configuration != null)
        {
            services.AddDynamicFormModule(configuration);
        }
    }

    public void ConfigureApplication(IApplicationBuilder app)
    {
        // Configure application pipeline if needed
    }
}
