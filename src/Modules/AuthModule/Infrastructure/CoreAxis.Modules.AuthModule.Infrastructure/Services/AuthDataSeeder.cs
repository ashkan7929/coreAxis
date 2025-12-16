using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Services;

public class AuthDataSeeder : IAuthDataSeeder
{
    private readonly AuthDbContext _context;
    private readonly ILogger<AuthDataSeeder> _logger;

    public AuthDataSeeder(AuthDbContext context, ILogger<AuthDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting Auth data seeding...");

            // Seed Roles
            await SeedRolesAsync();

            // Seed Actions
            await SeedActionsAsync();

            // Seed Pages
            await SeedPagesAsync();

            // Seed Permissions
            await SeedPermissionsAsync();

            // Assign permissions to roles
            await AssignPermissionsToRolesAsync();

            await _context.SaveChangesAsync();
            _logger.LogInformation("Auth data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Auth data seeding");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new { Name = "Admin", Description = "System Administrator", IsSystemRole = true },
            new { Name = "User", Description = "Regular User", IsSystemRole = true },
            new { Name = "ApiManager", Description = "API Manager Role", IsSystemRole = false }
        };

        foreach (var roleData in roles)
        {
            if (!await _context.Roles.AnyAsync(r => r.Name == roleData.Name))
            {
                var role = new Role(roleData.Name, roleData.Description, roleData.IsSystemRole);
                _context.Roles.Add(role);
                _logger.LogInformation($"Added role: {roleData.Name}");
            }
        }
    }

    private async Task SeedActionsAsync()
    {
        var actions = new[]
        {
            new { Code = "Read", Name = "Read", Description = "Read access" },
            new { Code = "Create", Name = "Create", Description = "Create access" },
            new { Code = "Update", Name = "Update", Description = "Update access" },
            new { Code = "Delete", Name = "Delete", Description = "Delete access" },
            new { Code = "Manage", Name = "Manage", Description = "Full management access" },
            new { Code = "MarkPaid", Name = "Mark Paid", Description = "Mark commission as paid" }
        };

        foreach (var actionData in actions)
        {
            if (!await _context.Actions.AnyAsync(a => a.Code == actionData.Code))
            {
                var action = new Domain.Entities.Action(actionData.Code, actionData.Name, actionData.Description);
                _context.Actions.Add(action);
                _logger.LogInformation($"Added action: {actionData.Code}");
            }
        }
    }

    private async Task SeedPagesAsync()
    {
        var pages = new[]
        {
            new { Code = "Auth", Name = "Authentication", ModuleName = "AuthModule", Description = "Authentication and authorization management" },
            new { Code = "Users", Name = "Users", ModuleName = "AuthModule", Description = "User management" },
            new { Code = "Roles", Name = "Roles", ModuleName = "AuthModule", Description = "Role management" },
            new { Code = "Permissions", Name = "Permissions", ModuleName = "AuthModule", Description = "Permission management" },
            new { Code = "ApiManager", Name = "API Manager", ModuleName = "ApiManager", Description = "API management and configuration" },
            new { Code = "WebServices", Name = "Web Services", ModuleName = "ApiManager", Description = "Web service management" },
            new { Code = "SecurityProfiles", Name = "Security Profiles", ModuleName = "ApiManager", Description = "Security profile management" },
            // Wallet Module
            new { Code = "WALLET", Name = "Wallet", ModuleName = "WalletModule", Description = "Wallet management" },
            // MLM Module
            new { Code = "Commissions", Name = "Commissions", ModuleName = "MLMModule", Description = "Commission management" },
            new { Code = "CommissionRules", Name = "Commission Rules", ModuleName = "MLMModule", Description = "Commission rules management" },
            new { Code = "UserReferrals", Name = "User Referrals", ModuleName = "MLMModule", Description = "User referrals management" }
        };

        foreach (var pageData in pages)
        {
            if (!await _context.Pages.AnyAsync(p => p.Code == pageData.Code))
            {
                var page = new Page(pageData.Code, pageData.Name, pageData.ModuleName, pageData.Description);
                _context.Pages.Add(page);
                _logger.LogInformation($"Added page: {pageData.Code}");
            }
        }
    }

    private async Task SeedPermissionsAsync()
    {
        // Get all pages and actions
        var pages = await _context.Pages.ToListAsync();
        var actions = await _context.Actions.ToListAsync();

        // Create permissions for specific page-action combinations
        var permissionCombinations = new[]
        {
            // Auth module permissions
            ("Auth", "Read"),
            ("Auth", "Manage"),
            ("Users", "Read"),
            ("Users", "Create"),
            ("Users", "Update"),
            ("Users", "Delete"),
            ("Roles", "Read"),
            ("Roles", "Create"),
            ("Roles", "Update"),
            ("Roles", "Delete"),
            ("Permissions", "Read"),
            ("Permissions", "Create"),
            ("Permissions", "Update"),
            ("Permissions", "Delete"),
            
            // ApiManager permissions
            ("ApiManager", "Read"),
            ("ApiManager", "Create"),
            ("ApiManager", "Update"),
            ("ApiManager", "Delete"),
            ("ApiManager", "Manage"),
            ("WebServices", "Read"),
            ("WebServices", "Create"),
            ("WebServices", "Update"),
            ("WebServices", "Delete"),
            ("SecurityProfiles", "Read"),
            ("SecurityProfiles", "Create"),
            ("SecurityProfiles", "Update"),
            ("SecurityProfiles", "Delete"),
            
            // Wallet permissions
            ("WALLET", "Read"),
            ("WALLET", "Manage"),
            
            // MLM permissions
            ("Commissions", "Read"),
            ("Commissions", "MarkPaid"),
            ("CommissionRules", "Create"),
            ("UserReferrals", "Create")
        };

        foreach (var (pageCode, actionCode) in permissionCombinations)
        {
            var page = pages.FirstOrDefault(p => p.Code == pageCode);
            var action = actions.FirstOrDefault(a => a.Code == actionCode);

            if (page != null && action != null)
            {
                if (!await _context.Permissions.AnyAsync(p => p.PageId == page.Id && p.ActionId == action.Id))
                {
                    var permission = new Permission(page.Id, action.Id, $"{pageCode} {actionCode} permission");
                    permission.SetName($"{pageCode}:{actionCode}");
                    _context.Permissions.Add(permission);
                    _logger.LogInformation($"Added permission: {pageCode}:{actionCode}");
                }
            }
        }
    }

    private async Task AssignPermissionsToRolesAsync()
    {
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        var apiManagerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "ApiManager");
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");

        if (adminRole != null)
        {
            // Admin gets all permissions
            var allPermissions = await _context.Permissions.ToListAsync();
            foreach (var permission in allPermissions)
            {
                if (!await _context.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission(adminRole.Id, permission.Id, Guid.Empty));
                }
            }
        }

        if (apiManagerRole != null)
        {
            // ApiManager gets ApiManager related permissions
            var apiManagerPermissions = await _context.Permissions
                .Include(p => p.Page)
                .Where(p => p.Page.Code == "ApiManager" || p.Page.Code == "WebServices" || p.Page.Code == "SecurityProfiles")
                .ToListAsync();

            foreach (var permission in apiManagerPermissions)
            {
                if (!await _context.RolePermissions.AnyAsync(rp => rp.RoleId == apiManagerRole.Id && rp.PermissionId == permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission(apiManagerRole.Id, permission.Id, Guid.Empty));
                }
            }
        }

        if (userRole != null)
        {
            // User gets basic read permissions
            var readPermissions = await _context.Permissions
                .Include(p => p.Action)
                .Where(p => p.Action.Code == "Read")
                .ToListAsync();

            foreach (var permission in readPermissions)
            {
                if (!await _context.RolePermissions.AnyAsync(rp => rp.RoleId == userRole.Id && rp.PermissionId == permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission(userRole.Id, permission.Id, Guid.Empty));
                }
            }
        }
    }
}