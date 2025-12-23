using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Play.Identity.Contracts;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Settings;
using System.Threading;

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
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await CreateAdminUserAndRoles(roleManager, userManager, publishEndpoint, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task CreateAdminUserAndRoles(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
        {
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
                    EmailConfirmed = true,
                    Gil = 100
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

            // Publish UserCreated so Trading can sync the admin user
            // We publish even if the user already existed — Trading handles idempotency
            await publishEndpoint.Publish(
                new UserUpdated(adminUser.Id, adminUser.Email!, adminUser.Gil),
                cancellationToken
            );

            Console.WriteLine($"Published UserCreated for admin: {adminUser.Email}");

            // Debug log: print roles for admin user
            var roles = await userManager.GetRolesAsync(adminUser);
            Console.WriteLine($"Roles for {_settings.AdminUserEmail}: {string.Join(",", roles)}");
        }

        public Task CreateServicesIdentities(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
        {
            // create identities for other services if needed
            throw new NotImplementedException();
        }

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