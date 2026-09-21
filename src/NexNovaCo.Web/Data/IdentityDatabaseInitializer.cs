using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

public static class IdentityDatabaseInitializer
{
    public const string AdminRole = "Admin";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration,
        IWebHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        // Development is automatic. Other environments require an explicit, controlled operator opt-in.
        if (!environment.IsDevelopment() && !configuration.GetValue<bool>("Identity:InitializeDatabase")) return;

        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await database.Database.MigrateAsync(cancellationToken);
        await HomeHeroInitializer.InitializeAsync(database, cancellationToken);
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(AdminRole))
            RequireSuccess(await roles.CreateAsync(new IdentityRole(AdminRole)));

        var email = configuration["AdminUser:Email"]?.Trim();
        var password = configuration["AdminUser:Password"];
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (environment.IsDevelopment())
                logger.LogWarning("Admin bootstrap skipped: configure AdminUser:Email and AdminUser:Password using User Secrets or environment variables. No default account was created.");
            return;
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email };
            RequireSuccess(await users.CreateAsync(user, password));
        }
        else if (!await users.IsInRoleAsync(user, AdminRole) && !await users.CheckPasswordAsync(user, password))
        {
            // Never silently promote an unrelated existing account selected by a misconfigured email.
            throw new InvalidOperationException("Admin bootstrap cannot promote the existing account without valid credentials.");
        }

        if (!await users.IsInRoleAsync(user, AdminRole))
            RequireSuccess(await users.AddToRoleAsync(user, AdminRole));
        // Existing Admin passwords are intentionally never changed by bootstrap configuration.
        logger.LogInformation("Admin bootstrap completed. Existing account passwords are unchanged.");
    }

    private static void RequireSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Identity bootstrap failed: " + string.Join(", ", result.Errors.Select(error => error.Code)));
    }
}
