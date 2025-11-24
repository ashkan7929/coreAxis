using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.Modules.Workflow.Infrastructure.Data;

namespace CoreAxis.ApiGateway.HealthChecks
{
    public class DbConnectivityHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DbConnectivityHealthCheck(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            var authOk = await TryCanConnect<AuthDbContext>(sp, cancellationToken);
            var workflowOk = await TryCanConnect<WorkflowDbContext>(sp, cancellationToken);

            if (authOk && workflowOk)
            {
                return HealthCheckResult.Healthy("Database connectivity OK");
            }

            return HealthCheckResult.Unhealthy("Database connectivity failed");
        }

        private static async Task<bool> TryCanConnect<TDb>(System.IServiceProvider sp, CancellationToken ct) where TDb : DbContext
        {
            var db = sp.GetService<TDb>();
            if (db == null) return false;
            try
            {
                return await db.Database.CanConnectAsync(ct);
            }
            catch
            {
                return false;
            }
        }
    }
}