using Logiwall.Domain.Entities.Identity;
using Logiwall.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logiwall.Infrastructure.Services.Identity;

/// <summary>
/// Makes the user in <see cref="AdminSeedOptions"/> an admin at start-up (ADR-0005).
/// Does nothing when the phone number or the password is not configured.
/// </summary>
public class AdminSeeder(IServiceScopeFactory scopeFactory,
    IOptions<AdminSeedOptions> options,
    ILogger<AdminSeeder> logger) : IHostedService
{
    private const string AdminRole = "Admin";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.PhoneNumber) || string.IsNullOrWhiteSpace(seed.Password))
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            var user = await userManager.FindByNameAsync(seed.PhoneNumber);
            IdentityResult result;
            if (user is null)
            {
                user = new User { UserName = seed.PhoneNumber, PhoneNumber = seed.PhoneNumber, Active = true };
                result = await userManager.CreateAsync(user, seed.Password);
            }
            else
            {
                // Never replace a password the user already has; the seed only fills a missing one.
                result = await userManager.HasPasswordAsync(user)
                    ? IdentityResult.Success
                    : await userManager.AddPasswordAsync(user, seed.Password);
            }

            if (result.Succeeded && await userManager.IsInRoleAsync(user, AdminRole) is false)
                result = await userManager.AddToRoleAsync(user, AdminRole);

            if (result.Succeeded is false)
            {
                logger.LogWarning("[AdminSeed][Start][{ErrorCodes}] Admin user was not seeded.", string.Join(", ", result.Errors.Select(e => e.Code)));
                return;
            }

            logger.LogInformation("[AdminSeed][Start][{UserId}] Admin user is ready.", user.Id);
        }
        catch (Exception exc)
        {
            // A database without the SeedAdminRole migration must not stop the API from starting.
            logger.LogError(exc, "[AdminSeed][Start] Seeding the admin user failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
