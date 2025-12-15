using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Settings;

namespace Play.Identity.Service.HostedServices
{
    public class IdentitySeedHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IdentitySettings _settings;

        public IdentitySeedHostedService(
            IServiceScopeFactory serviceScopeFactory,
            IOptions<IdentitySettings> settings)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _settings = settings.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure roles exist
            await CreateRoleIfNotExistsAsync(Roles.Admin, roleManager);
            await CreateRoleIfNotExistsAsync(Roles.Player, roleManager);

            // Ensure admin user exists
            var adminUser = await userManager.FindByEmailAsync(_settings.AdminUserEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = _settings.AdminUserEmail,
                    Email = _settings.AdminUserEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, _settings.AdminUserPassword);
                if (!createResult.Succeeded)
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", createResult.Errors)}");
                }
            }

            // Ensure admin user is in Admin role
            if (!await userManager.IsInRoleAsync(adminUser, Roles.Admin))
            {
                var addRoleResult = await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                if (!addRoleResult.Succeeded)
                {
                    throw new Exception($"Failed to add admin user to role: {string.Join(", ", addRoleResult.Errors)}");
                }
            }

            // Debug log: print roles for admin user
            var roles = await userManager.GetRolesAsync(adminUser);
            Console.WriteLine($"Roles for {_settings.AdminUserEmail}: {string.Join(",", roles)}");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task CreateRoleIfNotExistsAsync(
            string role,
            RoleManager<ApplicationRole> roleManager)
        {
            var roleExists = await roleManager.RoleExistsAsync(role);
            if (!roleExists)
            {
                var result = await roleManager.CreateAsync(new ApplicationRole { Name = role });
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role {role}: {string.Join(", ", result.Errors)}");
                }
            }
        }
    }
}