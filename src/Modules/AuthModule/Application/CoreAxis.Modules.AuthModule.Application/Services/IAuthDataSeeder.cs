using System.Threading.Tasks;

namespace CoreAxis.Modules.AuthModule.Application.Services;

/// <summary>
/// Interface for seeding initial authentication and authorization data
/// </summary>
public interface IAuthDataSeeder
{
    /// <summary>
    /// Seeds initial roles, pages, actions, and permissions
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SeedAsync();
}