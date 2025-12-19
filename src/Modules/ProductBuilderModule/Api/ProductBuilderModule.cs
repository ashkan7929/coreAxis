using CoreAxis.BuildingBlocks;
using CoreAxis.Modules.ProductBuilderModule.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.ProductBuilderModule.Api;

public class ProductBuilderModule : IModule
{
    public string Name => "ProductBuilderModule";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        services.AddProductBuilderModuleInfrastructure(configuration);
        
        services.AddControllers()
            .AddApplicationPart(typeof(ProductBuilderModule).Assembly);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CoreAxis.Modules.ProductBuilderModule.Application.Commands.CreateProductCommand).Assembly));
    }

    public void ConfigureApplication(IApplicationBuilder app)
    {
        // Configure middleware/endpoints here
    }
}
